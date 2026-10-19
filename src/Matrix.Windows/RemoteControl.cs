using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using Matrix.Core;
using Matrix.Remote;
using SkiaSharp;

namespace Matrix.Windows;

public sealed partial class OperatorWindow
{
    private RemoteServer? remoteServer;
    private bool changingRemote;
    private readonly TextBox remoteInfo = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Text = "Browser-Steuerung ist ausgeschaltet.", Margin = new Thickness(4) };
    private readonly ConcurrentDictionary<string, byte[]> remoteImages = new();

    private void AddRemoteControls(Panel panel)
    {
        panel.Children.Add(new TextBlock { Text = "Steuerung per Handy / zweitem Rechner", FontSize = 20, Margin = new Thickness(4, 20, 4, 8) });
        panel.Children.Add(PresentationWindow.ActionButton("Browser-Steuerung starten", async () => await StartRemote()));
        panel.Children.Add(PresentationWindow.ActionButton("Steuerung hier im Browser öffnen", () =>
        {
            if (remoteServer is not null) Process.Start(new ProcessStartInfo("http://127.0.0.1:8787") { UseShellExecute = true });
        }));
        panel.Children.Add(PresentationWindow.ActionButton("Zweiten Bildschirm im Browser öffnen", () =>
        {
            if (remoteServer is not null) Process.Start(new ProcessStartInfo("http://127.0.0.1:8787/presentation") { UseShellExecute = true });
            else remoteInfo.Text = "Bitte zuerst die Browser-Steuerung starten.";
        }));
        panel.Children.Add(PresentationWindow.ActionButton("Browser-Steuerung stoppen", async () => await StopRemote()));
        panel.Children.Add(remoteInfo);
    }

    private async Task StartRemote()
    {
        if (remoteServer is not null || changingRemote) return;
        changingRemote = true;
        var candidate = new RemoteServer(new WindowController(this));
        try
        {
            await candidate.StartAsync(); remoteServer = candidate;
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
                .Select(a => $"http://{a.Address}:8787").Distinct();
            remoteInfo.Text = "Zugangscode: " + candidate.AccessCode + "\nAuf diesem Rechner: http://127.0.0.1:8787\n" +
                "Im WLAN / über Tailscale:\n" + string.Join("\n", addresses) +
                "\nInternet: Tailscale auf PC und Handy verbinden; alternativ Tailscale Serve laut README nutzen. Keine Router-Portfreigabe erforderlich.";
        }
        catch (Exception ex)
        {
            remoteInfo.Text = "Browser-Steuerung konnte nicht starten: " + ex.Message;
            await candidate.DisposeAsync();
        }
        finally { changingRemote = false; }
    }

    private async Task StopRemote()
    {
        if (changingRemote || remoteServer is null) return;
        changingRemote = true;
        try
        {
            var running = remoteServer; remoteServer = null;
            await running.DisposeAsync(); remoteImages.Clear();
            remoteInfo.Text = "Browser-Steuerung gestoppt. Import, Präsentation und Druck laufen lokal weiter. Beim Neustart gilt ein neuer Zugangscode.";
        }
        catch (Exception ex) { remoteInfo.Text = "Browser-Steuerung beendet: " + ex.Message; }
        finally { changingRemote = false; }
    }

    private RemoteState RemoteSnapshot()
    {
        var printStates = savedPhotos.ToDictionary(p => p.Id, p => p.PrintState.ToString());
        var photos = history?.Items.Select(item => new RemotePhoto(item.Id, grid!.Coordinate(item.Column, item.Row), item.Column, item.Row,
            printStates.GetValueOrDefault(item.Id, "NotRequested"))).ToArray() ?? [];
        return new(projectDirectory is null ? null : Path.GetFileName(projectDirectory), grid?.Columns ?? 0, grid?.Rows ?? 0,
            grid?.TileAspect ?? 1.5, grid is null ? 0 : grid.Columns * grid.Rows,
            grid is null ? 0 : GridPlanning.Remaining(grid, photos.Length), photos, history?.Current?.Id,
            history?.IsPlaying ?? false, mosaicVisible, scan.IsEnabled, printEnabled, printerName, lastPrintError, CurrentInvitation());
    }

    private RemoteResult RemoteExecute(RemoteCommand command)
    {
        if (closing || history is null || grid is null || projectDirectory is null) return new(false, "Bitte am Fotobox-Rechner ein Projekt öffnen.");
        if (command.ProjectId != Path.GetFileName(projectDirectory)) return new(false, "Projekt wurde gewechselt. Bitte die Anzeige aktualisieren.");
        switch (command.Action)
        {
            case "previous": case "next": case "latest": case "select": case "slideshow": case "mosaic": case "presentation":
                if (command.Action == "presentation") { ShowPresentation(); return new(presentation is not null, "Windows-Präsentation geöffnet."); }
                if (presentation is not null)
                    return new(presentation.RemoteAction(command.Action, command.ImageId), "Präsentationssteuerung aktualisiert.");
                // Browser-only presentation: the shared clock and selection do not require a native window.
                switch (command.Action)
                {
                    case "previous": history.Previous(); break;
                    case "next": history.Next(); break;
                    case "latest": history.Latest(); break;
                    case "select": if (command.ImageId is null || !history.Select(command.ImageId)) return new(false, "Foto nicht verfügbar."); break;
                    case "slideshow": history.ToggleSlideshow(); break;
                    case "mosaic": mosaicVisible = !mosaicVisible; break;
                }
                ResetPresentationClock();
                return new(true, "Browser-Präsentation aktualisiert.");
            case "monitor-pause": scan.Stop(); return new(true, "Import pausiert. Laufender Import wird noch abgeschlossen.");
            case "monitor-resume":
                if (completing) return new(false, "Veranstaltungsabschluss läuft. Bitte zuerst abschließen oder abbrechen.");
                if (inputDirectory is null) return new(false, "Bitte zuerst am PC einen Eingangsordner auswählen.");
                if (history.Items.Count >= grid.Columns * grid.Rows) return new(false, "Das Mosaik ist vollständig.");
                scan.Start(); return new(true, "Ordnerüberwachung fortgesetzt.");
            case "print-pause": printEnabled = false; UpdatePrintStatus(); return new(true, "Weitere Zusatzdrucke pausiert. Bereits an Windows übergebene Aufträge laufen weiter.");
            case "print-resume":
                if (completing) return new(false, "Veranstaltungsabschluss läuft. Drucke können anschließend freigegeben werden.");
                if (printerName is null || !File.Exists(Path.Combine(projectDirectory, "print-ticket.xml")))
                    return new(false, "Bitte zuerst Drucker und Papierformat am PC einrichten.");
                if (!CanImportFormat()) return new(false, "Altes quadratisches Projekt: nur Ansicht.");
                printEnabled = true; lastPrintError = null; UpdatePrintStatus(); _ = ProcessPrintQueue(); return new(true, "Zusatzdruck fortgesetzt.");
            case "print":
                if (busy || printBusy) return new(false, "Ein Import oder Druckauftrag läuft. Bitte danach erneut versuchen.");
                if (!command.ConfirmReprint) return new(false, "Zusätzlichen Ausdruck bitte bestätigen.");
                if (printerName is null || !CanImportFormat()) return new(false, "Bitte Drucker am PC einrichten.");
                int index = savedPhotos.FindIndex(p => p.Id == command.ImageId);
                if (index < 0) return new(false, "Das ausgewählte Foto ist nicht mehr verfügbar.");
                var previous = savedPhotos[index];
                if (previous.PrintState == PrintState.Pending) return new(true, "Dieses Foto wartet bereits auf den Zusatzdruck.");
                savedPhotos[index] = previous with { PrintState = PrintState.Pending };
                try { SaveProject(); }
                catch { savedPhotos[index] = previous; throw; }
                UpdatePrintStatus(); _ = ProcessPrintQueue();
                return new(true, $"Zusatzdruck für {grid.Coordinate(previous.Column, previous.Row)} angefordert{(printEnabled ? "." : " – wartet, solange der Zusatzdruck pausiert ist.")}");
            default: return new(false, "Unbekannter Steuerbefehl.");
        }
    }

    private sealed class WindowController(OperatorWindow owner) : IRemoteController
    {
        public Task<RemoteState> ReadState() => owner.Dispatcher.InvokeAsync(owner.RemoteSnapshot).Task;
        public async Task<RemoteResult> Execute(RemoteCommand command)
        {
            try { return await owner.Dispatcher.InvokeAsync(() => owner.RemoteExecute(command)); }
            catch (Exception ex) { return new(false, "Aktion fehlgeschlagen: " + ex.Message); }
        }
        public async Task<byte[]?> ReadImage(string id, bool large = false, string? projectId = null)
        {
            var spec = await owner.Dispatcher.InvokeAsync(() =>
            {
                if (owner.projectDirectory is null || owner.grid is null) return null;
                if (projectId is not null && projectId != Path.GetFileName(owner.projectDirectory)) return null;
                string? path = id == "master" ? Path.Combine(owner.projectDirectory, "master") : owner.history?.Items.FirstOrDefault(p => p.Id == id)?.TilePath;
                return path is null ? null : new ImageSpec(path, owner.grid, id == "master");
            });
            if (spec is null) return null;
            string key = spec.Path;
            if (!large && owner.remoteImages.TryGetValue(key, out var cached)) return cached;
            try
            {
                return await Task.Run(() =>
                {
                    using var image = spec.Master ? Mosaic.RenderMaster(spec.Path, spec.Grid) : SKBitmap.Decode(spec.Path);
                    if (image is null) return null;
                    int width = spec.Master ? image.Width : large ? 800 : (int)Math.Clamp(1600.0 / spec.Grid.Columns, 16, 300);
                    int height = Math.Max(1, (int)Math.Round((double)image.Height * width / image.Width));
                    using var thumbnail = new SKBitmap(width, height);
                    using (var canvas = new SKCanvas(thumbnail))
                        canvas.DrawBitmap(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear));
                    using var data = thumbnail.Encode(SKEncodedImageFormat.Png, 100);
                    var bytes = data.ToArray(); if (!large) owner.remoteImages[key] = bytes; return bytes;
                });
            }
            catch (IOException) { return null; }
        }
    }
    private sealed record ImageSpec(string Path, Matrix.Core.Grid Grid, bool Master);
}
