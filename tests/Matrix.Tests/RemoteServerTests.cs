using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Matrix.Remote;
using Xunit;

public class RemoteServerTests
{
    private sealed class Controller : IRemoteController
    {
        public int Commands;
        public Task<RemoteState> ReadState() => Task.FromResult(new RemoteState("project", 5, 5, 1.5, 25, 24,
            [new(new string('A', 64), "D03", 3, 2, "Pending")], new string('A', 64), false, false, true, false, "RX1", null));
        public Task<byte[]?> ReadImage(string id, bool large = false) => Task.FromResult<byte[]?>(id == "master" ? [1, 2, 3] : null);
        public Task<RemoteResult> Execute(RemoteCommand command)
        { Interlocked.Increment(ref Commands); return Task.FromResult(new RemoteResult(true, "Übernommen")); }
    }

    private static async Task Login(HttpClient client, string code)
    {
        using var response = await client.PostAsJsonAsync("/login", new { code });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
    }

    [Fact] public async Task PhotosAndControlAreProtectedAndPageDoesNotExposeCode()
    {
        await using var server = new RemoteServer(new Controller(), 0, loopbackOnly: true); await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.Address) };
        string page = await client.GetStringAsync("/");
        Assert.Contains("Matrix Fernsteuerung", page); Assert.DoesNotContain(server.AccessCode, page);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/state")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/images/master")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/command", new RemoteCommand("print", "project", "one"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/login", new { code = "wrong" })).StatusCode);
        await Login(client, server.AccessCode);
        using var stateResponse = await client.GetAsync("/api/state"); stateResponse.EnsureSuccessStatusCode();
        Assert.True(stateResponse.Headers.CacheControl!.NoStore);
        Assert.False(stateResponse.Headers.Contains("Access-Control-Allow-Origin"));
        var state = await stateResponse.Content.ReadFromJsonAsync<RemoteState>();
        Assert.Equal(24, state!.Remaining); Assert.Equal("D03", state.Photos[0].Coordinate);
        using var image = await client.GetAsync("/api/images/master"); image.EnsureSuccessStatusCode();
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(new byte[] { 1, 2, 3 }, await image.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/images/not-a-photo")).StatusCode);
    }

    [Fact] public async Task RepeatedOrConcurrentPhoneRequestsExecutePrintOnlyOnce()
    {
        var controller = new Controller();
        await using var server = new RemoteServer(controller, 0, loopbackOnly: true); await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.Address) }; await Login(client, server.AccessCode);
        var command = new RemoteCommand("print", "project", "request-123", new string('A', 64), true);
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync("/api/command", command)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(1, controller.Commands);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/command", command with { ImageId = new string('B', 64) })).StatusCode);
        Assert.Equal(1, controller.Commands);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/command", command with { RequestId = "" })).StatusCode);
        foreach (var response in responses) response.Dispose();
    }

    [Fact] public async Task LoginAttemptsAreLimitedAndRestartRevokesPreviousSession()
    {
        string oldCode, oldToken;
        await using (var server = new RemoteServer(new Controller(), 0, loopbackOnly: true))
        {
            await server.StartAsync(); oldCode = server.AccessCode;
            using var client = new HttpClient { BaseAddress = new Uri(server.Address) }; await Login(client, oldCode);
            oldToken = client.DefaultRequestHeaders.Authorization!.Parameter!;
            for (int i = 0; i < 4; i++) await client.PostAsJsonAsync("/login", new { code = "wrong" });
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/login", new { code = "wrong" })).StatusCode);
        }
        await using var restarted = new RemoteServer(new Controller(), 0, loopbackOnly: true); await restarted.StartAsync();
        using var newClient = new HttpClient { BaseAddress = new Uri(restarted.Address) };
        newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await newClient.GetAsync("/api/state")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await newClient.PostAsJsonAsync("/login", new { code = oldCode })).StatusCode);
    }
}
