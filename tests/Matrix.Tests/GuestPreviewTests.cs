using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Matrix.Remote;
using Xunit;

public class GuestPreviewTests
{
    private sealed class Controller : IRemoteController
    {
        public string Project = "event-a";
        public int Commands;
        public string? RequestedProject;
        public Task<RemoteState> ReadState() => Task.FromResult(new RemoteState(Project, 5, 5, 1.5, 25, 24,
            [new(new string('A', 64), "D03", 3, 2, "Pending")], new string('A', 64), false, false, true, false, "PRIVATE PRINTER", "PRIVATE ERROR"));
        public Task<byte[]?> ReadImage(string id, bool large = false, string? projectId = null)
        {
            RequestedProject = projectId;
            return Task.FromResult<byte[]?>(projectId == Project && id == "master" ? [1, 2, 3] : null);
        }
        public Task<RemoteResult> Execute(RemoteCommand command)
        { Commands++; return Task.FromResult(new RemoteResult(true, "Unexpected command")); }
    }

    private static string Token(GuestPreviewServer server) => new Uri(server.CreateLink(server.Address)).Fragment[1..];

    [Fact] public async Task GuestLinkGivesReadOnlyProjectDataButNoControlOrPrinterDetails()
    {
        var controller = new Controller();
        await using var guest = new GuestPreviewServer(controller, "event-a", 0, true); await guest.StartAsync();
        await using var admin = new RemoteServer(controller, 0, true); await admin.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(guest.Address) };
        Assert.Contains("Unser Fotomosaik", await client.GetStringAsync("/view"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/view/state")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/view/images/master")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(guest));
        string json = await client.GetStringAsync("/view/state");
        Assert.DoesNotContain("PRIVATE", json); Assert.DoesNotContain("printState", json);
        var state = JsonSerializer.Deserialize<PreviewState>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(24, state.Remaining); Assert.Equal("D03", state.Photos[0].Coordinate);
        Assert.Equal(new byte[] { 1, 2, 3 }, await client.GetByteArrayAsync("/view/images/master"));
        Assert.Equal("event-a", controller.RequestedProject);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/view/images/not-an-id")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/command", new RemoteCommand("print", "event-a", "attack"))).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/view/state", new { action = "print" })).StatusCode);
        using var adminClient = new HttpClient { BaseAddress = new Uri(admin.Address) };
        adminClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        Assert.Equal(HttpStatusCode.Unauthorized, (await adminClient.GetAsync("/api/state")).StatusCode);
        Assert.Equal(0, controller.Commands);
    }

    [Fact] public async Task ProjectChangeAndNewShareDoNotExposeAnotherEvent()
    {
        var controller = new Controller(); string oldToken;
        await using (var guest = new GuestPreviewServer(controller, "event-a", 0, true))
        {
            await guest.StartAsync(); oldToken = Token(guest);
            using var client = new HttpClient { BaseAddress = new Uri(guest.Address) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/view/state")).StatusCode);
            controller.Project = "event-b";
            Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync("/view/state")).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync("/view/images/master")).StatusCode);
        }
        await using var restarted = new GuestPreviewServer(controller, "event-b", 0, true); await restarted.StartAsync();
        using var newClient = new HttpClient { BaseAddress = new Uri(restarted.Address) };
        newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await newClient.GetAsync("/view/state")).StatusCode);
    }

    [Fact] public async Task LinksSupportLanAndPublicHttpsAndQrHasPngSignature()
    {
        await using var server = new GuestPreviewServer(new Controller(), "event-a", 0, true);
        var lan = new Uri(server.CreateLink("http://192.168.1.5:8788"));
        var internet = new Uri(server.CreateLink("https://my-event.example:8443/"));
        Assert.Equal("/view", lan.AbsolutePath); Assert.Equal("", lan.Query);
        Assert.Equal(lan.Fragment, internet.Fragment); Assert.True(internet.Fragment.Length > 40);
        var png = GuestPreviewServer.QrPng(internet.AbsoluteUri);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Throws<ArgumentException>(() => server.CreateLink("file:///private"));
        Assert.Throws<ArgumentException>(() => server.CreateLink("https://user:password@example.com"));
        Assert.Throws<ArgumentException>(() => server.CreateLink("https://example.com/api/command"));
        Assert.Throws<ArgumentException>(() => server.CreateLink("https://example.com/#old-code"));
    }
}
