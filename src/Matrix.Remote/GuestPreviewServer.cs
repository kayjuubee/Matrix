using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace Matrix.Remote;

public sealed record PreviewPhoto(string Id, string Coordinate, int Column, int Row);
public sealed record PreviewState(string ProjectId, int Columns, int Rows, double TileAspect,
    int Total, int Remaining, IReadOnlyList<PreviewPhoto> Photos);

/// <summary>A separate, read-only listener suitable for sharing with event guests.</summary>
public sealed class GuestPreviewServer : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly string shareToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public string Address => app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    public GuestPreviewServer(IRemoteController controller, string projectId, int port = 8788, bool loopbackOnly = false)
    {
        if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("Ein Projekt muss ausgewählt sein.");
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1024;
            options.Listen(loopbackOnly ? IPAddress.Loopback : IPAddress.Any, port);
        });
        app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' blob:; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            if (context.Request.Path.StartsWithSegments("/view/state") || context.Request.Path.StartsWithSegments("/view/images"))
            {
                if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString()), Encoding.UTF8.GetBytes("Bearer " + shareToken)))
                { context.Response.StatusCode = 401; return; }
                if ((await controller.ReadState()).ProjectId != projectId)
                { context.Response.StatusCode = 410; return; }
            }
            await next(context);
        });
        app.MapGet("/", () => Results.Redirect("/view"));
        app.MapGet("/view", () =>
        {
            using var stream = typeof(GuestPreviewServer).Assembly.GetManifestResourceStream("Matrix.Remote.preview.html")!;
            using var reader = new StreamReader(stream);
            return Results.Text(reader.ReadToEnd(), "text/html; charset=utf-8");
        });
        app.MapGet("/view/state", async () =>
        {
            var state = await controller.ReadState();
            if (state.ProjectId != projectId) return Results.StatusCode(410);
            return Results.Ok(new PreviewState(projectId, state.Columns, state.Rows, state.TileAspect, state.Total, state.Remaining,
                state.Photos.Select(p => new PreviewPhoto(p.Id, p.Coordinate, p.Column, p.Row)).ToArray()));
        });
        app.MapGet("/view/images/{id}", async (string id) =>
        {
            if (id != "master" && (id.Length != 64 || id.Any(c => !Uri.IsHexDigit(c)))) return Results.NotFound();
            // Resolve inside the shared project, even during a project switch.
            var bytes = await controller.ReadImage(id, projectId: projectId);
            if ((await controller.ReadState()).ProjectId != projectId) return Results.StatusCode(410);
            return bytes is null ? Results.NotFound() : Results.File(bytes, "image/png");
        });
        // No login, command, printer or administrative routes on this listener.
    }

    public string CreateLink(string baseAddress)
    {
        if (!Uri.TryCreate(baseAddress.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrWhiteSpace(uri.Host) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
            throw new ArgumentException("Bitte nur die Basisadresse eingeben, z. B. http://192.168.1.50:8788 oder die öffentliche HTTPS-Adresse.");
        return new UriBuilder(uri) { Path = "/view", Fragment = shareToken }.Uri.AbsoluteUri;
    }

    public static byte[] QrPng(string link)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }

    public Task StartAsync() => app.StartAsync();
    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try { await app.StopAsync(timeout.Token); }
        finally { await app.DisposeAsync(); }
    }
}
