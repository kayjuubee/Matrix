using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matrix.Remote;
using Microsoft.Win32;

namespace Matrix.Windows;

public sealed partial class OperatorWindow
{
    private GuestPreviewServer? guestServer;
    private bool changingGuest;
    private readonly TextBox guestInfo = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Text = "Vorschau-Freigabe ausgeschaltet.", Margin = new Thickness(4) };

    private void AddPreviewSharing(Panel panel)
    {
        panel.Children.Add(new TextBlock { Text = "Mosaikvorschau für Gäste teilen", FontSize = 20, Margin = new Thickness(4, 20, 4, 8) });
        panel.Children.Add(PresentationWindow.ActionButton("Vorschau teilen / QR-Code …", OpenPreviewSharing));
        panel.Children.Add(PresentationWindow.ActionButton("Vorschau-Freigabe beenden", async () => await StopGuestSharing()));
        panel.Children.Add(guestInfo);
    }

    private void OpenPreviewSharing()
    {
        if (projectDirectory is null || grid is null) { guestInfo.Text = "Bitte zuerst ein Projekt öffnen."; return; }
        var addresses = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
            .Select(a => $"http://{a.Address}:8788").Distinct().ToList();
        if (addresses.Count == 0) addresses.Add("http://127.0.0.1:8788");
        new PreviewShareWindow(addresses, StartGuestSharing) { Owner = this }.ShowDialog();
    }

    private async Task<string> StartGuestSharing(string baseAddress)
    {
        if (changingGuest || projectDirectory is null) throw new InvalidOperationException("Bitte einen Moment warten und ein Projekt auswählen.");
        changingGuest = true;
        try
        {
            if (guestServer is null)
            {
                var candidate = new GuestPreviewServer(new WindowController(this), Path.GetFileName(projectDirectory));
                try { _ = candidate.CreateLink(baseAddress); await candidate.StartAsync(); guestServer = candidate; }
                catch { await candidate.DisposeAsync(); throw; }
            }
            var link = guestServer.CreateLink(baseAddress);
            guestInfo.Text = "Freigegebene Vorschau (nur ansehen):\n" + link + "\nWLAN-Adresse nur im selben Netz; öffentliche HTTPS-Adresse erst nach Einrichtung des Internetzugangs erreichbar.";
            return link;
        }
        finally { changingGuest = false; }
    }

    private async Task StopGuestSharing()
    {
        if (changingGuest || guestServer is null) return;
        changingGuest = true;
        try { var running = guestServer; guestServer = null; await running.DisposeAsync(); guestInfo.Text = "Vorschau-Freigabe beendet. Bisherige Links sind gesperrt; die lokale Arbeit läuft weiter."; }
        catch (Exception ex) { guestInfo.Text = "Vorschau beendet: " + ex.Message; }
        finally { changingGuest = false; }
    }
}

internal sealed class PreviewShareWindow : Window
{
    private readonly ComboBox address;
    private readonly Image qr = new() { Width = 300, Height = 300, Stretch = Stretch.Uniform };
    private readonly TextBox link = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock info = new() { TextWrapping = TextWrapping.Wrap };
    private byte[]? png;

    public PreviewShareWindow(List<string> addresses, Func<string, Task<string>> generate)
    {
        Title = "Mosaikvorschau teilen"; Width = 650; Height = 780; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "QR-Code für die Live-Vorschau", FontSize = 24 });
        panel.Children.Add(new TextBlock { Text = "Jeder mit diesem Link kann die Fotovorschau ansehen. Der Link bietet keine Steuerung und keinen Druckzugriff.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 16) });
        panel.Children.Add(new TextBlock { Text = "WLAN-Adresse wählen oder öffentliche HTTPS-Basisadresse eingeben:" });
        address = new ComboBox { IsEditable = true, ItemsSource = addresses, Text = addresses[0], Margin = new Thickness(0, 8, 0, 8) }; panel.Children.Add(address);
        var create = PresentationWindow.ActionButton("Link und QR-Code erstellen", () => { });
        create.Click += async (_, _) =>
        {
            create.IsEnabled = false;
            png = null; qr.Source = null; link.Text = "";
            try
            {
                var url = await generate(address.Text); png = GuestPreviewServer.QrPng(url); link.Text = url;
                using var stream = new MemoryStream(png);
                var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); qr.Source = image;
                info.Text = "QR-Code scannen oder den vollständigen Link teilen. Zum Sperren die Vorschau-Freigabe im Hauptfenster beenden.";
            }
            catch (Exception ex) { info.Text = ex.Message; }
            finally { create.IsEnabled = true; }
        };
        panel.Children.Add(create); panel.Children.Add(qr); panel.Children.Add(link);
        var buttons = new WrapPanel(); panel.Children.Add(buttons);
        buttons.Children.Add(PresentationWindow.ActionButton("Link kopieren", () => SafeAction(() => { if (link.Text.Length > 0) Clipboard.SetText(link.Text); })));
        buttons.Children.Add(PresentationWindow.ActionButton("Vorschau öffnen", () => SafeAction(() => { if (link.Text.Length > 0) Process.Start(new ProcessStartInfo(link.Text) { UseShellExecute = true }); })));
        buttons.Children.Add(PresentationWindow.ActionButton("QR-Code als PNG speichern …", () => SafeAction(() =>
        {
            if (png is null) return;
            var dialog = new SaveFileDialog { FileName = "Mosaik-Vorschau-QR.png", Filter = "PNG-Bild|*.png" };
            if (dialog.ShowDialog(this) == true) File.WriteAllBytes(dialog.FileName, png);
        })));
        panel.Children.Add(info);
        panel.Children.Add(new TextBlock
        {
            Text = "Für Gäste außerhalb des WLANs: öffentliche HTTPS-Adresse für Vorschau-Port 8788 einrichten (z. B. Tailscale Funnel auf HTTPS-Port 8443, siehe README). Tailscale Serve allein ist privat und für Gäste ohne Tailscale nicht ausreichend. Matrix und Internetverbindung müssen weiterlaufen.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0)
        });
    }

    private void SafeAction(Action action)
    {
        try { action(); }
        catch (Exception ex) { info.Text = ex.Message; }
    }
}
