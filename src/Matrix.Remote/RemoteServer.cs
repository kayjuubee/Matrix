using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Matrix.Remote;

public sealed class RemoteServer : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly SemaphoreSlim commandLock = new(1, 1);
    private readonly Dictionary<string, (RemoteCommand Command, RemoteResult Result)> completed = [];
    private readonly Queue<string> requestOrder = [];
    public string AccessCode { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
    public string Address => app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    public RemoteServer(IRemoteController controller, int port = 8787, bool loopbackOnly = false)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders(); // Never log access codes, bearer tokens or event photos.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 4096;
            options.Listen(loopbackOnly ? IPAddress.Loopback : IPAddress.Any, port);
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' blob:; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            // No cookies and no CORS: commands require an explicit bearer token.
            if (context.Request.Path.StartsWithSegments("/api") &&
                !SecureEquals(context.Request.Headers.Authorization.ToString(), "Bearer " + token))
            { context.Response.StatusCode = 401; return; }
            await next(context);
        });
        app.UseRateLimiter();
        app.MapGet("/", () => Results.Text(ReadPage(), "text/html; charset=utf-8"));
        app.MapPost("/login", (LoginRequest request) => SecureEquals(request.Code?.Trim().ToUpperInvariant() ?? "", AccessCode)
            ? Results.Ok(new { token }) : Results.Unauthorized()).RequireRateLimiting("login");
        app.MapGet("/api/state", () => controller.ReadState());
        app.MapGet("/api/images/{id}", async (string id, bool? large) =>
        {
            if (id != "master" && (id.Length != 64 || id.Any(c => !Uri.IsHexDigit(c)))) return Results.NotFound();
            var image = await controller.ReadImage(id, large == true);
            return image is null ? Results.NotFound() : Results.File(image, "image/png");
        });
        app.MapPost("/api/command", async (RemoteCommand command) =>
        {
            if (string.IsNullOrWhiteSpace(command.RequestId) || command.RequestId.Length > 100 || string.IsNullOrWhiteSpace(command.Action) || command.Action.Length > 50)
                return Results.BadRequest(new RemoteResult(false, "Ungültiger Auftrag."));
            await commandLock.WaitAsync();
            try
            {
                // A phone retry after a lost response must not create a second print.
                if (completed.TryGetValue(command.RequestId, out var previous))
                    return previous.Command == command ? Results.Ok(previous.Result) : Results.Conflict();
                var result = await controller.Execute(command);
                if (completed.Count >= 1000) completed.Remove(requestOrder.Dequeue());
                completed.Add(command.RequestId, (command, result)); requestOrder.Enqueue(command.RequestId);
                return Results.Ok(result);
            }
            finally { commandLock.Release(); }
        });
    }

    public Task StartAsync() => app.StartAsync();
    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try { await app.StopAsync(timeout.Token); }
        finally { await app.DisposeAsync(); }
    }

    private static bool SecureEquals(string a, string b) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    private static string ReadPage()
    {
        using var stream = typeof(RemoteServer).Assembly.GetManifestResourceStream("Matrix.Remote.control.html")!;
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    private sealed record LoginRequest(string? Code);
}
