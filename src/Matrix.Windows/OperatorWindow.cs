using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Matrix.Core;
using Microsoft.Win32;
using SkiaSharp;
using MosaicGrid = Matrix.Core.Grid;

namespace Matrix.Windows;

public sealed class OperatorWindow : Window
{
    private readonly TextBlock status = new() { Text = "Neues Projekt anlegen oder ein gespeichertes Projekt öffnen.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock printStatus = new() { Text = "Zusatzdruck pausiert · noch kein Drucker gewählt", TextWrapping = TextWrapping.Wrap };
    private readonly List<SavedPhoto> savedPhotos = [];
    private string? printerName;
    private bool printEnabled;
    private bool printBusy;
    private string? lastPrintError;
    private readonly TextBlock projectLabel = new() { TextWrapping = TextWrapping.Wrap };
    private readonly DispatcherTimer scan = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly Dictionary<string, (long Length, DateTime Modified)> observed = new(StringComparer.OrdinalIgnoreCase);
    private PresentationHistory? history;
    private MosaicGrid? grid;
    private PresentationWindow? presentation;
    private string? projectDirectory;
    private string? inputDirectory;
    private FileStream? projectLock;
    private bool busy;
    private bool closing;

    public OperatorWindow()
    {
        Title = "Matrix – Steuerung"; Width = 650; Height = 820; MinWidth = 550; MinHeight = 450;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "Matrix · Fotomosaik", FontSize = 28, Margin = new Thickness(0, 0, 0, 20) });
        panel.Children.Add(PresentationWindow.ActionButton("Neues Projekt: zuerst Logo auswählen …", CreateProject));
        panel.Children.Add(PresentationWindow.ActionButton("Gespeichertes Projekt öffnen …", OpenProject));
        panel.Children.Add(PresentationWindow.ActionButton("Präsentationsfenster öffnen", ShowPresentation));
        var import = PresentationWindow.ActionButton("Fotos hinzufügen …", () => _ = SelectPhotos()); panel.Children.Add(import);
        panel.Children.Add(PresentationWindow.ActionButton("Eingangsordner überwachen …", WatchFolder));
        panel.Children.Add(PresentationWindow.ActionButton("Überwachung stoppen", () =>
        { scan.Stop(); inputDirectory = null; observed.Clear(); status.Text = "Ordnerüberwachung gestoppt."; }));
        panel.Children.Add(new TextBlock
        {
            Text = "Präsentationsfenster auf den zweiten Bildschirm ziehen; F11 für Vollbild.\n← / → blättern · Leertaste Diashow · Ende neuestes Bild · Esc Fenster",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 16)
        });
        panel.Children.Add(PresentationWindow.ActionButton("Zusatzdruck einrichten / starten …", ConfigurePrinting));
        panel.Children.Add(PresentationWindow.ActionButton("Zusatzdruck pausieren", () => { printEnabled = false; UpdatePrintStatus(); }));
        panel.Children.Add(PresentationWindow.ActionButton("Ausgewähltes Bild drucken / nachdrucken", QueueSelectedPhoto));
        panel.Children.Add(printStatus);
        panel.Children.Add(projectLabel); panel.Children.Add(status);
        scan.Tick += async (_, _) => await ScanFolder();
        Closed += (_, _) => { closing = true; scan.Stop(); presentation?.Close(); if (!busy) projectLock?.Dispose(); };
    }

    private bool CanChangeProject()
    {
        if (!busy && !printBusy) return true;
        status.Text = "Bitte warten, bis der laufende Import beendet ist."; return false;
    }

    private void CreateProject()
    {
        if (!CanChangeProject()) return;
        try
        {
            var dialog = new OpenFileDialog { Title = "Großes Motiv auswählen", Filter = ImageFilter };
            if (dialog.ShowDialog(this) != true) return;
            if (!CanChangeProject()) return;
            var setup = new GridSetupWindow(dialog.FileName) { Owner = this };
            if (setup.ShowDialog() != true || setup.SelectedGrid is not { } nextGrid) return;
            if (!CanChangeProject()) return;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Matrix", "Projects", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.Copy(dialog.FileName, Path.Combine(dir, "master"));
            File.WriteAllText(Path.Combine(dir, "project.json"), JsonSerializer.Serialize(new SavedProject(nextGrid.Columns, nextGrid.Rows, [], nextGrid.Format)));
            Activate(dir, nextGrid, new PresentationHistory(nextGrid));
        }
        catch (Exception ex) { status.Text = $"Projekt konnte nicht erstellt werden: {ex.Message}"; }
    }

    private void OpenProject()
    {
        if (!CanChangeProject()) return;
        var dialog = new OpenFileDialog { Title = "Matrix-Projekt öffnen", Filter = "Matrix-Projekt|project.json" };
        if (dialog.ShowDialog(this) != true) return;
        if (!CanChangeProject()) return;
        try
        {
            var saved = JsonSerializer.Deserialize<SavedProject>(File.ReadAllText(dialog.FileName)) ?? throw new IOException("Leere Projektdatei.");
            var nextGrid = new MosaicGrid(saved.Columns, saved.Rows, saved.Format); nextGrid.Validate();
            var nextHistory = new PresentationHistory(nextGrid);
            string dir = Path.GetDirectoryName(dialog.FileName)!;
            using var preview = Mosaic.RenderMaster(Path.Combine(dir, "master"), nextGrid);
            foreach (var photo in saved.Photos)
            {
                if (photo.Id.Length != 64 || photo.Id.Any(c => !Uri.IsHexDigit(c))) throw new IOException("Ungültige Bildkennung.");
                string tile = Path.Combine(dir, photo.Id + ".png");
                if (!File.Exists(tile)) throw new IOException($"Bilddatei fehlt: {tile}");
                if (!nextHistory.Add(new(photo.Id, tile, photo.Column, photo.Row))) throw new IOException("Doppeltes Bild im Projekt.");
            }
            Activate(dir, nextGrid, nextHistory, saved);
        }
        catch (Exception ex) { status.Text = $"Projekt konnte nicht geöffnet werden: {ex.Message}"; }
    }

    private void Activate(string dir, MosaicGrid nextGrid, PresentationHistory nextHistory, SavedProject? saved = null)
    {
        if (!string.Equals(dir, projectDirectory, StringComparison.OrdinalIgnoreCase))
        {
            var nextLock = new FileStream(Path.Combine(dir, "project.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            projectLock?.Dispose(); projectLock = nextLock;
        }
        scan.Stop(); inputDirectory = null; observed.Clear(); presentation?.Close(); presentation = null;
        projectDirectory = dir; grid = nextGrid; history = nextHistory;
        savedPhotos.Clear(); savedPhotos.AddRange((saved?.Photos ?? []).Select(PrintRecovery.AfterRestart));
        printerName = saved?.PrinterName; printEnabled = false; lastPrintError = null; UpdatePrintStatus();
        SaveProject();
        projectLabel.Text = $"Projekt: {Path.Combine(dir, "project.json")}";
        status.Text = grid.Format == PhotoFormat.Square
            ? "Altes quadratisches Projekt: nur Ansicht. Für 10 × 15 cm ein neues Projekt anlegen und Originalfotos importieren."
            : $"{history.Items.Count} Bilder geladen · {(grid.Format == PhotoFormat.Landscape10x15 ? "15 × 10" : "10 × 15")} cm. Fotos hinzufügen oder einen Eingangsordner wählen.";
        ShowPresentation();
    }

    private void ShowPresentation()
    {
        if (history is null || grid is null || projectDirectory is null)
        { status.Text = "Bitte zuerst ein Projekt anlegen oder öffnen."; return; }
        try
        {
            if (presentation is null)
            {
                var window = new PresentationWindow(Path.Combine(projectDirectory, "master"), grid, history);
                window.Closed += (_, _) => presentation = null;
                presentation = window; window.Show();
            }
            else { presentation.Show(); presentation.Activate(); }
        }
        catch (Exception ex) { presentation = null; status.Text = $"Anzeige fehlgeschlagen: {ex.Message}"; }
    }

    private async Task SelectPhotos()
    {
        if (history is null || busy || !CanImportFormat()) return;
        var dialog = new OpenFileDialog { Filter = ImageFilter, Multiselect = true };
        if (dialog.ShowDialog(this) == true) await Import(dialog.FileNames);
    }

    private void WatchFolder()
    {
        if (history is null) { status.Text = "Bitte zuerst ein Projekt anlegen."; return; }
        if (!CanImportFormat()) return;
        var dialog = new OpenFolderDialog { Title = "Fotobox-Eingangsordner auswählen" };
        if (dialog.ShowDialog(this) != true) return;
        var selected = Path.GetFullPath(dialog.FolderName).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var output = Path.GetFullPath(projectDirectory!).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (output.StartsWith(selected, StringComparison.OrdinalIgnoreCase) || selected.StartsWith(output, StringComparison.OrdinalIgnoreCase))
        { status.Text = "Eingangsordner und Projektordner müssen getrennt sein."; return; }
        inputDirectory = dialog.FolderName; observed.Clear(); scan.Start();
        status.Text = "Überwachung aktiv. Auch vorhandene Bilder werden eingelesen; identische Bilder werden übersprungen.";
    }

    private async Task ScanFolder()
    {
        if (busy || inputDirectory is null || closing) return;
        try
        {
            var stable = new List<string>();
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFiles(inputDirectory).Where(IsImage).Order(StringComparer.OrdinalIgnoreCase))
            {
                present.Add(path);
                var file = new FileInfo(path); var signature = (file.Length, file.LastWriteTimeUtc);
                if (observed.TryGetValue(path, out var previous) && previous == signature && file.Length > 0) stable.Add(path);
                observed[path] = signature;
            }
            foreach (var missing in observed.Keys.Where(x => !present.Contains(x)).ToArray()) observed.Remove(missing);
            await Import(stable);
        }
        catch (Exception ex) { status.Text = $"Ordnerprüfung: {ex.Message}"; }
    }

    private async Task Import(IEnumerable<string> files)
    {
        if (busy || grid is null || history is null || projectDirectory is null || !CanImportFormat()) return;
        busy = true;
        try
        {
            foreach (var path in files)
            {
                if (closing) break;
                try
                {
                    // Exclusive snapshot prevents reading a file while the photobooth is writing it.
                    byte[] bytes = await Task.Run(() =>
                    {
                        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                        using var copy = new MemoryStream(); source.CopyTo(copy); return copy.ToArray();
                    });
                    string id = Convert.ToHexString(SHA256.HashData(bytes));
                    if (history.Items.Any(x => x.Id == id)) continue;
                    if (history.Items.Count >= grid.Columns * grid.Rows)
                    { status.Text = "Die Matrix ist vollständig belegt. Es werden keine weiteren Felder vergeben."; scan.Stop(); break; }
                    int cell = GridPlanning.RandomFreeCell(grid, history.Items)
                        ?? throw new InvalidOperationException("Die Matrix ist vollständig belegt.");
                    int column = cell % grid.Columns, row = cell / grid.Columns;
                    string snapshot = Path.Combine(projectDirectory, "source.tmp"), tile = Path.Combine(projectDirectory, id + ".png");
                    try
                    {
                        await File.WriteAllBytesAsync(snapshot, bytes);
                        await Task.Run(() =>
                        {
                            using var image = Mosaic.Render(snapshot, Path.Combine(projectDirectory, "master"), grid, column, row);
                            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                            using var output = File.Create(tile + ".tmp"); data.SaveTo(output);
                        });
                        File.Move(tile + ".tmp", tile, true);
                        // Remember extra-print requests before they can be sent to Windows.
                        var entry = new SavedPhoto(id, column, row, printerName is null ? PrintState.NotRequested : PrintState.Pending);
                        savedPhotos.Add(entry);
                        try { SaveProject(); }
                        catch { savedPhotos.RemoveAt(savedPhotos.Count - 1); throw; }
                        history.Add(new(id, tile, column, row));
                        presentation?.NewArrival();
                        status.Text = $"Neues Bild: {grid.Coordinate(column, row)} · {history.Items.Count} von {grid.Columns * grid.Rows}";
                    }
                    finally { File.Delete(snapshot); }
                }
                catch (Exception ex) { status.Text = $"{Path.GetFileName(path)}: {ex.Message}"; }
            }
        }
        finally
        {
            busy = false; if (closing) projectLock?.Dispose();
            else { UpdatePrintStatus(); _ = ProcessPrintQueue(); }
        }
    }

    private void SaveProject()
    {
        if (grid is null || projectDirectory is null) return;
        string manifest = Path.Combine(projectDirectory, "project.json");
        File.WriteAllText(manifest + ".tmp", JsonSerializer.Serialize(new SavedProject(grid.Columns, grid.Rows, savedPhotos, grid.Format, printerName)));
        File.Move(manifest + ".tmp", manifest, true);
    }

    private void ConfigurePrinting()
    {
        if (grid is null || projectDirectory is null || busy || printBusy || !CanImportFormat()) return;
        try
        {
            var selected = PhotoPrinter.Choose(this, grid.Format);
            if (selected is null) return;
            File.WriteAllBytes(Path.Combine(projectDirectory, "print-ticket.xml"), selected.Value.Ticket);
            printerName = selected.Value.PrinterName; SaveProject();
            printEnabled = true; lastPrintError = null; UpdatePrintStatus(); _ = ProcessPrintQueue();
        }
        catch (Exception ex) { printEnabled = false; printStatus.Text = $"Druckereinrichtung: {ex.Message}"; }
    }

    private void QueueSelectedPhoto()
    {
        if (busy || printBusy || history?.Current is null || printerName is null || !CanImportFormat())
        { printStatus.Text = "Zuerst einen Drucker einrichten und ein Foto auswählen."; return; }
        var current = history.Current;
        int index = savedPhotos.FindIndex(p => p.Id == current.Id);
        if (index < 0) return;
        var previous = savedPhotos[index];
        if (previous.PrintState is PrintState.Submitted or PrintState.NeedsReview &&
            MessageBox.Show(this, "Dieses Foto könnte bereits gedruckt worden sein. Einen weiteren Ausdruck mit derselben Koordinate anfordern?",
                "Nachdruck", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            savedPhotos[index] = previous with { PrintState = PrintState.Pending };
            SaveProject(); UpdatePrintStatus(); _ = ProcessPrintQueue();
        }
        catch (Exception ex) { savedPhotos[index] = previous; printStatus.Text = $"Druckauftrag nicht gespeichert: {ex.Message}"; }
    }

    private void UpdatePrintStatus()
    {
        int pending = savedPhotos.Count(p => p.PrintState == PrintState.Pending);
        int review = savedPhotos.Count(p => p.PrintState == PrintState.NeedsReview);
        int submitted = savedPhotos.Count(p => p.PrintState == PrintState.Submitted);
        printStatus.Text = $"Zusatzdruck {(printEnabled ? "aktiv" : "pausiert")} · {printerName ?? "kein Drucker"}\n" +
            $"{pending} wartend · {submitted} an Windows übergeben · {review} manuell prüfen" +
            (lastPrintError is null ? "" : $"\nDruck angehalten: {lastPrintError}");
    }

    private async Task ProcessPrintQueue()
    {
        if (printBusy || busy || !printEnabled || closing || printerName is null || projectDirectory is null || grid is null) return;
        printBusy = true;
        try
        {
            while (printEnabled && !closing)
            {
                // Let navigation, new arrivals and the pause button run between submissions.
                await Dispatcher.Yield(DispatcherPriority.Background);
                if (!printEnabled || closing || busy) break;
                var photo = PrintRecovery.Next(savedPhotos);
                if (photo is null) break;
                int index = savedPhotos.FindIndex(p => p.Id == photo.Id);
                try
                {
                    savedPhotos[index] = photo with { PrintState = PrintState.Submitting };
                    SaveProject(); // Persist before calling the spooler to prevent blind duplicate retries.
                    PhotoPrinter.Submit(printerName, Path.Combine(projectDirectory, "print-ticket.xml"),
                        Path.Combine(projectDirectory, photo.Id + ".png"), grid.Coordinate(photo.Column, photo.Row));
                    savedPhotos[index] = photo with { PrintState = PrintState.Submitted };
                    SaveProject(); UpdatePrintStatus();
                }
                catch (Exception ex)
                {
                    savedPhotos[index] = photo with { PrintState = PrintState.NeedsReview };
                    printEnabled = false;
                    try { SaveProject(); } catch { /* The persisted Submitting state is recovered for manual review. */ }
                    lastPrintError = ex.Message; UpdatePrintStatus();
                }
            }
        }
        finally { printBusy = false; }
    }

    private bool CanImportFormat()
    {
        if (grid?.Format != PhotoFormat.Square) return true;
        status.Text = "Für 10 × 15 cm bitte ein neues Projekt anlegen. Das alte quadratische Projekt bleibt unverändert.";
        return false;
    }

    private const string ImageFilter = "Bilder|*.jpg;*.jpeg;*.png;*.webp;*.bmp";
    private static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
