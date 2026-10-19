using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Matrix.Windows;

public sealed partial class OperatorWindow
{
    private readonly TextBlock dashboardCount = new() { FontSize = 30, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock dashboardState = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar dashboardProgress = new() { Height = 8, Maximum = 1, Foreground = new SolidColorBrush(Color.FromRgb(57, 200, 186)), Background = new SolidColorBrush(Color.FromRgb(41, 53, 72)), Margin = new Thickness(0, 12, 0, 16) };
    private readonly Image dashboardImage = new() { Stretch = Stretch.Uniform, MaxHeight = 390 };
    private readonly TextBlock dashboardImageTitle = new() { Text = "Dein Fotomosaik beginnt hier", FontSize = 20, Margin = new Thickness(0, 0, 0, 14), TextWrapping = TextWrapping.Wrap };
    private string? dashboardImageKey;

    private UIElement BuildStudioLayout()
    {
        var root = new DockPanel { Margin = new Thickness(28) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 22) };
        header.Children.Add(new TextBlock { Text = "MATRIX", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(57, 200, 186)) });
        header.Children.Add(new TextBlock { Text = "FOTOBOX STUDIO  /  Gemeinsam wird ein großes Bild daraus.", Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 4, 0, 0) });
        DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var footer = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        footer.Children.Add(status); projectLabel.FontSize = 11; projectLabel.Foreground = Brushes.LightSlateGray;
        projectLabel.Margin = new Thickness(0, 8, 0, 0); footer.Children.Add(projectLabel);
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var tabs = new TabControl(); root.Children.Add(tabs);
        var session = Page(tabs, "Veranstaltung");
        var columns = new System.Windows.Controls.Grid();
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        var preview = new StackPanel(); preview.Children.Add(dashboardImageTitle); preview.Children.Add(dashboardImage);
        preview.Children.Add(new TextBlock { Text = "Die Gästeansicht öffnest du über „Anzeige“ auf dem zweiten Bildschirm.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 16, 0, 0) });
        columns.Children.Add(Card(preview));
        var actions = new StackPanel();
        actions.Children.Add(dashboardCount); actions.Children.Add(dashboardProgress); actions.Children.Add(dashboardState);
        actions.Children.Add(Heading("Projekt & Fotoeingang"));
        Add(actions, "Neues Projekt · Logo auswählen", CreateProject, true);
        Add(actions, "Gespeichertes Projekt öffnen", OpenProject);
        Add(actions, "Fotos hinzufügen", () => _ = SelectPhotos());
        Add(actions, "Eingangsordner überwachen", WatchFolder, true);
        Add(actions, "Überwachung pausieren", () => { scan.Stop(); observed.Clear(); status.Text = "Ordnerüberwachung pausiert."; RefreshDashboard(); });
        var actionCard = Card(actions); System.Windows.Controls.Grid.SetColumn(actionCard, 1); columns.Children.Add(actionCard); session.Children.Add(columns);
        var printing = Page(tabs, "Druck & Abschluss");
        printing.Children.Add(Heading("Zusatzdruck für das Mosaik"));
        printing.Children.Add(new TextBlock { Text = "Das Fotoboxfoto bleibt bei DSLRBooth / LumaBooth. Matrix druckt zusätzlich die Mosaikkachel im eingerichteten 10 × 15-cm-Format.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 4, 16) });
        Add(printing, "Zusatzdruck einrichten / starten", ConfigurePrinting, true);
        Add(printing, "Zusatzdruck pausieren", () => { printEnabled = false; UpdatePrintStatus(); });
        Add(printing, "Ausgewähltes Bild drucken / nachdrucken", QueueSelectedPhoto); printing.Children.Add(printStatus);
        printing.Children.Add(Heading("Veranstaltung abschließen"));
        Add(printing, "Restfelder mit / ohne Fotos abschließen", async () => await CompleteRemaining());
        Add(printing, "Laufenden Abschluss abbrechen", () => completionCancel?.Cancel());
        var display = Page(tabs, "Anzeige");
        display.Children.Add(Heading("Gästeansicht & Einladung"));
        Add(display, "Präsentationsfenster öffnen", ShowPresentation, true);
        Add(display, "Einladungstext und Wartezeit einstellen", ConfigureInvitation);
        display.Children.Add(new TextBlock { Text = "Fenster auf den zweiten Bildschirm ziehen.\n\nF11 · Vollbild\n← / → · Durch Fotos blättern\nLeertaste · Diashow\nEnde · Neuestes Foto\nEsc · Vollbild verlassen", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 20, 4, 0) });
        var sharing = Page(tabs, "Browser & QR-Code"); AddRemoteControls(sharing); AddPreviewSharing(sharing);
        RefreshDashboard(); return root;
    }

    private static StackPanel Page(TabControl tabs, string title)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };
        tabs.Items.Add(new TabItem { Header = title, Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } });
        return panel;
    }

    private static Border Card(UIElement child) => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(28, 35, 48)), CornerRadius = new CornerRadius(14),
        Padding = new Thickness(20), Margin = new Thickness(0, 0, 12, 0), Child = child
    };

    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 20, 4, 12) };

    private static void Add(Panel panel, string title, Action action, bool primary = false)
    {
        var button = PresentationWindow.ActionButton(title, action);
        if (primary)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(35, 109, 112));
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(57, 200, 186));
            button.FontWeight = FontWeights.SemiBold;
        }
        panel.Children.Add(button);
    }

    private void RefreshDashboard()
    {
        int total = grid is null ? 0 : grid.Columns * grid.Rows, count = history?.Items.Count ?? 0;
        dashboardCount.Text = total == 0 ? "Bereit für dein Event" : $"{count} / {total} Fotos";
        dashboardProgress.Value = total == 0 ? 0 : (double)count / total;
        dashboardState.Text = $"{(total == 0 ? "Noch kein Projekt" : $"{total - count} Fotos fehlen noch")}\nFotoeingang: {(scan.IsEnabled ? "aktiv" : "pausiert")} · Druck: {(printEnabled ? "aktiv" : "pausiert")}";
        var current = history?.Current;
        string? path = current?.TilePath ?? (projectDirectory is null ? null : Path.Combine(projectDirectory, "master"));
        string? key = path;
        if (key == dashboardImageKey) return;
        dashboardImageKey = key;
        try
        {
            if (path is null) { dashboardImage.Source = null; return; }
            if (current is not null)
            {
                dashboardImage.Source = PresentationWindow.LoadSource(path, 900);
                dashboardImageTitle.Text = $"Aktuelles Foto · Feld {grid!.Coordinate(current.Column, current.Row)}";
            }
            else
            {
                using var bitmap = Matrix.Core.Mosaic.RenderMaster(path, grid!);
                dashboardImage.Source = PresentationWindow.ToSource(bitmap); dashboardImageTitle.Text = "Dein Motiv · bereit für die ersten Fotos";
            }
        }
        catch { dashboardImage.Source = null; dashboardImageTitle.Text = "Vorschau derzeit nicht verfügbar"; dashboardImageKey = null; }
    }
}
