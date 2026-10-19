using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Matrix.Core;
using Microsoft.Win32;
using SkiaSharp;

namespace Matrix.Windows;

public sealed partial class OperatorWindow
{
    private bool completing;
    private CancellationTokenSource? completionCancel;

    private async Task CompleteRemaining()
    {
        if (busy || printBusy || completing || history is null || grid is null || projectDirectory is null || !CanImportFormat()) return;
        int remaining = GridPlanning.Remaining(grid, history.Items.Count);
        if (remaining == 0) { status.Text = "Das Mosaik ist bereits vollständig."; return; }
        bool wasMonitoring = scan.IsEnabled, wasPrinting = printEnabled, accepted = false;
        scan.Stop(); printEnabled = false; busy = true; completing = true; UpdatePrintStatus();
        try
        {
            var dialog = new CompletionWindow(remaining, inputDirectory) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            accepted = true;
            var plan = CompletionPlan.Create(grid, history.Items, dialog.PhotoPaths);
            completionCancel = new CancellationTokenSource();
            int generated = 0;
            foreach (var cell in plan)
            {
                if (closing || completionCancel.IsCancellationRequested) break;
                string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                string tile = Path.Combine(projectDirectory, id + ".png");
                try
                {
                    await Task.Run(() =>
                    {
                        using var image = cell.PhotoPath is null
                            ? Mosaic.RenderLogoTile(Path.Combine(projectDirectory, "master"), grid, cell.Column, cell.Row)
                            : Mosaic.Render(cell.PhotoPath, Path.Combine(projectDirectory, "master"), grid, cell.Column, cell.Row);
                        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                        using var output = File.Create(tile + ".tmp"); data.SaveTo(output);
                    });
                    File.Move(tile + ".tmp", tile);
                    var entry = new SavedPhoto(id, cell.Column, cell.Row, PrintState.Pending);
                    savedPhotos.Add(entry);
                    try { SaveProject(); }
                    catch { savedPhotos.RemoveAt(savedPhotos.Count - 1); throw; }
                    history.Add(new(id, tile, cell.Column, cell.Row));
                    generated++; ResetPresentationClock(); presentation?.NewArrival(); UpdatePrintStatus();
                    status.Text = $"Abschluss: {generated} von {plan.Count} Restfeldern erzeugt. Zusatzdruck pausiert.";
                }
                finally { if (File.Exists(tile + ".tmp")) File.Delete(tile + ".tmp"); }
            }
            status.Text = $"{generated} Abschlusskacheln erzeugt und als Druckaufträge vorgemerkt. " +
                $"Noch {GridPlanning.Remaining(grid, history.Items.Count)} freie Felder. Zum Drucken Zusatzdruck einrichten/fortsetzen. Import bleibt pausiert.";
        }
        catch (Exception ex)
        {
            status.Text = "Abschluss angehalten: " + ex.Message + " Bereits erzeugte Kacheln bleiben gespeichert; Drucken ist pausiert. Erneut starten füllt nur die übrigen Felder.";
        }
        finally
        {
            completionCancel?.Dispose(); completionCancel = null; completing = false; busy = false;
            if (!accepted && !closing) { if (wasMonitoring) scan.Start(); printEnabled = wasPrinting; _ = ProcessPrintQueue(); }
            else { scan.Stop(); printEnabled = false; }
            UpdatePrintStatus();
            if (closing) projectLock?.Dispose();
        }
    }
}

internal sealed class CompletionWindow : Window
{
    private readonly RadioButton withPhotos = new() { Content = "Mit Fotos und Motivüberlagerung", Margin = new Thickness(0, 12, 0, 8) };
    private readonly RadioButton withoutPhotos = new() { Content = "Ohne Fotos – nur die Ausschnitte des großen Motivs", Margin = new Thickness(0, 8, 0, 12) };
    private readonly TextBlock selected = new() { Text = "Noch keine Fotos gewählt.", TextWrapping = TextWrapping.Wrap };
    private string[] photos = [];
    public IReadOnlyList<string>? PhotoPaths { get; private set; }

    public CompletionWindow(int remaining, string? inputDirectory)
    {
        Title = "Veranstaltungsabschluss"; Width = 600; Height = 440; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = $"{remaining} Restfelder vervollständigen", FontSize = 25 });
        panel.Children.Add(new TextBlock
        {
            Text = $"Es entstehen {remaining} zusätzliche Druckaufträge. Bestehende Felder bleiben erhalten. Import und Zusatzdruck sind während der Vorbereitung pausiert.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0)
        });
        panel.Children.Add(withPhotos);
        panel.Children.Add(PresentationWindow.ActionButton("Originalfotos auswählen …", () =>
        {
            var picker = new OpenFileDialog { Title = "Originalfotos für die Restfelder auswählen (keine bereits überlagerten Kacheln)", Multiselect = true, Filter = "Bilder|*.jpg;*.jpeg;*.png;*.webp;*.bmp" };
            if (inputDirectory is not null && Directory.Exists(inputDirectory)) picker.InitialDirectory = inputDirectory;
            if (picker.ShowDialog(this) == true) { photos = picker.FileNames; withPhotos.IsChecked = true; selected.Text = $"{photos.Length} Fotos gewählt. Bei Bedarf werden sie wiederverwendet."; }
        }));
        panel.Children.Add(selected); panel.Children.Add(withoutPhotos);
        panel.Children.Add(PresentationWindow.ActionButton($"{remaining} Kacheln erzeugen und Drucke vormerken", () =>
        {
            if (withPhotos.IsChecked == true && photos.Length == 0) { selected.Text = "Bitte zuerst Fotos auswählen."; return; }
            if (withPhotos.IsChecked != true && withoutPhotos.IsChecked != true) { selected.Text = "Bitte mit oder ohne Fotos wählen."; return; }
            PhotoPaths = withPhotos.IsChecked == true ? photos : null; DialogResult = true;
        }));
        panel.Children.Add(new TextBlock { Text = "Nach dem Erzeugen bleiben die Drucke pausiert. Du gibst sie anschließend selbst frei. Den laufenden Abschluss kannst du im Hauptfenster abbrechen.", TextWrapping = TextWrapping.Wrap });
    }
}
