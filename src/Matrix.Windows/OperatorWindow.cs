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

public sealed record SavedPhoto(string Id, int Column, int Row);
public sealed record SavedProject(int Columns, int Rows, List<SavedPhoto> Photos);

public sealed class OperatorWindow : Window
{
    private readonly TextBox columns = new() { Text = "20", Width = 70 };
    private readonly TextBox rows = new() { Text = "15", Width = 70 };
    private readonly TextBlock status = new() { Text = "Neues Projekt anlegen oder ein gespeichertes Projekt öffnen.", TextWrapping = TextWrapping.Wrap };
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
        Title = "Matrix – Steuerung"; Width = 650; Height = 570; MinWidth = 550; MinHeight = 450;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Matrix · Fotomosaik", FontSize = 28, Margin = new Thickness(0, 0, 0, 20) });
        var dimensions = new StackPanel { Orientation = Orientation.Horizontal };
        dimensions.Children.Add(new TextBlock { Text = "Spalten ", VerticalAlignment = VerticalAlignment.Center }); dimensions.Children.Add(columns);
        dimensions.Children.Add(new TextBlock { Text = "   Zeilen ", VerticalAlignment = VerticalAlignment.Center }); dimensions.Children.Add(rows);
        panel.Children.Add(dimensions);
        panel.Children.Add(PresentationWindow.ActionButton("Neues Projekt mit Motiv anlegen …", CreateProject));
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
        panel.Children.Add(projectLabel); panel.Children.Add(status);
        scan.Tick += async (_, _) => await ScanFolder();
        Closed += (_, _) => { closing = true; scan.Stop(); presentation?.Close(); if (!busy) projectLock?.Dispose(); };
    }

    private bool CanChangeProject()
    {
        if (!busy) return true;
        status.Text = "Bitte warten, bis der laufende Import beendet ist."; return false;
    }

    private void CreateProject()
    {
        if (!CanChangeProject()) return;
        try
        {
            var nextGrid = new MosaicGrid(int.Parse(columns.Text), int.Parse(rows.Text)); nextGrid.Validate();
            var dialog = new OpenFileDialog { Title = "Großes Motiv auswählen", Filter = ImageFilter };
            if (dialog.ShowDialog(this) != true) return;
            // Decode before switching projects so an invalid motif leaves the previous project intact.
            using var preview = Mosaic.RenderMaster(dialog.FileName, nextGrid);
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Matrix", "Projects", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.Copy(dialog.FileName, Path.Combine(dir, "master"));
            File.WriteAllText(Path.Combine(dir, "project.json"), JsonSerializer.Serialize(new SavedProject(nextGrid.Columns, nextGrid.Rows, [])));
            Activate(dir, nextGrid, new PresentationHistory(nextGrid));
        }
        catch (Exception ex) { status.Text = $"Projekt konnte nicht erstellt werden: {ex.Message}"; }
    }

    private void OpenProject()
    {
        if (!CanChangeProject()) return;
        var dialog = new OpenFileDialog { Title = "Matrix-Projekt öffnen", Filter = "Matrix-Projekt|project.json" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var saved = JsonSerializer.Deserialize<SavedProject>(File.ReadAllText(dialog.FileName)) ?? throw new IOException("Leere Projektdatei.");
            var nextGrid = new MosaicGrid(saved.Columns, saved.Rows); nextGrid.Validate();
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
            Activate(dir, nextGrid, nextHistory);
        }
        catch (Exception ex) { status.Text = $"Projekt konnte nicht geöffnet werden: {ex.Message}"; }
    }

    private void Activate(string dir, MosaicGrid nextGrid, PresentationHistory nextHistory)
    {
        if (!string.Equals(dir, projectDirectory, StringComparison.OrdinalIgnoreCase))
        {
            var nextLock = new FileStream(Path.Combine(dir, "project.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            projectLock?.Dispose(); projectLock = nextLock;
        }
        scan.Stop(); inputDirectory = null; observed.Clear(); presentation?.Close(); presentation = null;
        projectDirectory = dir; grid = nextGrid; history = nextHistory;
        columns.Text = grid.Columns.ToString(); rows.Text = grid.Rows.ToString();
        projectLabel.Text = $"Projekt: {Path.Combine(dir, "project.json")}";
        status.Text = $"{history.Items.Count} Bilder geladen. Fotos hinzufügen oder einen Eingangsordner wählen.";
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
        if (history is null || busy) return;
        var dialog = new OpenFileDialog { Filter = ImageFilter, Multiselect = true };
        if (dialog.ShowDialog(this) == true) await Import(dialog.FileNames);
    }

    private void WatchFolder()
    {
        if (history is null) { status.Text = "Bitte zuerst ein Projekt anlegen."; return; }
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
        if (busy || grid is null || history is null || projectDirectory is null) return;
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
                    var used = history.Items.Select(x => x.Row * grid.Columns + x.Column).ToHashSet();
                    int cell = Enumerable.Range(0, grid.Columns * grid.Rows).First(x => !used.Contains(x));
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
                        var photos = history.Items.Select(x => new SavedPhoto(x.Id, x.Column, x.Row)).ToList();
                        photos.Add(new(id, column, row));
                        string manifest = Path.Combine(projectDirectory, "project.json");
                        await File.WriteAllTextAsync(manifest + ".tmp", JsonSerializer.Serialize(new SavedProject(grid.Columns, grid.Rows, photos)));
                        File.Move(manifest + ".tmp", manifest, true);
                        history.Add(new(id, tile, column, row));
                        presentation?.NewArrival();
                        status.Text = $"Neues Bild: {grid.Coordinate(column, row)} · {history.Items.Count} von {grid.Columns * grid.Rows}";
                    }
                    finally { File.Delete(snapshot); }
                }
                catch (Exception ex) { status.Text = $"{Path.GetFileName(path)}: {ex.Message}"; }
            }
        }
        finally { busy = false; if (closing) projectLock?.Dispose(); }
    }

    private const string ImageFilter = "Bilder|*.jpg;*.jpeg;*.png;*.webp;*.bmp";
    private static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
