using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Matrix.Core;
using SkiaSharp;
using MosaicGrid = Matrix.Core.Grid;

namespace Matrix.Windows;

public sealed class PresentationWindow : Window
{
    private readonly PresentationHistory history;
    private readonly MosaicGrid mosaicGrid;
    private readonly Canvas wall;
    private readonly Canvas mosaicLayer;
    private readonly Button previewButton;
    private readonly TextBlock previewStatus = new() { Foreground = Brushes.LightGray, Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Center };
    private readonly HashSet<string> previewLoaded = [];
    private readonly HashSet<string> previewFailed = [];
    private bool showMosaic;
    private bool loadingPreview;
    private bool closed;
    private readonly double cellWidth;
    private const double CellHeight = 60;
    private readonly Rectangle highlight;
    private readonly Image photo = new() { Stretch = Stretch.Uniform };
    private readonly TextBlock coordinate = new() { FontSize = 64, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
    private readonly TextBlock position = new() { FontSize = 18, Foreground = Brushes.LightGray };
    private readonly TextBlock remaining = new() { FontSize = 30, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
    private readonly Button play;
    private readonly DispatcherTimer slideshow = new() { Interval = TimeSpan.FromSeconds(8) };
    private readonly Dictionary<string, Rectangle> occupied = [];

    public PresentationWindow(string masterPath, MosaicGrid grid, PresentationHistory history)
    {
        this.history = history;
        mosaicGrid = grid;
        Title = "Matrix – Präsentation";
        Width = 1280; Height = 800; MinWidth = 800; MinHeight = 500;
        Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
        var root = new DockPanel { Margin = new Thickness(24) };
        Content = root;
        var controls = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        DockPanel.SetDock(controls, Dock.Bottom); root.Children.Add(controls);
        controls.Children.Add(ActionButton("← Zurück", () => { history.Previous(); Refresh(); }));
        play = ActionButton("Diashow starten", () => { history.ToggleSlideshow(); ResetTimer(); Refresh(); });
        controls.Children.Add(play);
        controls.Children.Add(ActionButton("Weiter →", () => { history.Next(); Refresh(); }));
        controls.Children.Add(ActionButton("Neuestes Bild", () => { history.Latest(); Refresh(); }));
        controls.Children.Add(ActionButton("Vollbild (F11)", ToggleFullscreen));
        previewButton = ActionButton("Fotomosaik anzeigen", ToggleMosaic);
        controls.Children.Add(previewButton); controls.Children.Add(previewStatus);
        var layout = new System.Windows.Controls.Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        root.Children.Add(layout);

        cellWidth = CellHeight * grid.TileAspect;
        double width = grid.Columns * cellWidth, height = grid.Rows * CellHeight;
        wall = new Canvas { Width = width, Height = height, Background = Brushes.White };
        using (var master = Mosaic.RenderMaster(masterPath, grid))
            wall.Children.Add(new Image { Width = width, Height = height, Stretch = Stretch.Fill, Source = ToSource(master) });
        mosaicLayer = new Canvas { Width = width, Height = height, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
        wall.Children.Add(mosaicLayer);
        var viewbox = new Viewbox { Child = wall, Stretch = Stretch.Uniform, Margin = new Thickness(12) };
        layout.Children.Add(viewbox);
        for (int c = 0; c <= grid.Columns; c++)
            wall.Children.Add(new Line { X1 = c * cellWidth, X2 = c * cellWidth, Y1 = 0, Y2 = height, Stroke = Brushes.Black, Opacity = 0.45, StrokeThickness = 1 });
        for (int r = 0; r <= grid.Rows; r++)
            wall.Children.Add(new Line { X1 = 0, X2 = width, Y1 = r * CellHeight, Y2 = r * CellHeight, Stroke = Brushes.Black, Opacity = 0.45, StrokeThickness = 1 });
        highlight = new Rectangle
        {
            Width = cellWidth, Height = CellHeight, Stroke = Brushes.OrangeRed, StrokeThickness = 5,
            Fill = new SolidColorBrush(Color.FromArgb(110, 255, 190, 0)), Visibility = Visibility.Collapsed
        };
        wall.Children.Add(highlight);
        var side = new DockPanel { Margin = new Thickness(20) };
        System.Windows.Controls.Grid.SetColumn(side, 1); layout.Children.Add(side);
        var info = new StackPanel { Margin = new Thickness(0, 12, 0, 12) };
        info.Children.Add(remaining); info.Children.Add(coordinate); info.Children.Add(position);
        DockPanel.SetDock(info, Dock.Bottom); side.Children.Add(info); side.Children.Add(photo);
        slideshow.Tick += (_, _) => { history.Tick(); Refresh(); };
        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Left: history.Previous(); break;
                case Key.Right: history.Next(); break;
                case Key.Space: history.ToggleSlideshow(); ResetTimer(); break;
                case Key.End: history.Latest(); break;
                case Key.F11: ToggleFullscreen(); break;
                case Key.Escape: WindowStyle = WindowStyle.SingleBorderWindow; WindowState = WindowState.Normal; break;
                default: return;
            }
            e.Handled = true; Refresh();
        };
        Closed += (_, _) => { closed = true; slideshow.Stop(); };
        ResetTimer(); Refresh();
    }

    private void ToggleMosaic()
    {
        showMosaic = !showMosaic;
        mosaicLayer.Visibility = showMosaic ? Visibility.Visible : Visibility.Collapsed;
        previewButton.Content = showMosaic ? "Nur Motiv anzeigen" : "Fotomosaik anzeigen";
        previewFailed.Clear();
        if (showMosaic) _ = UpdateMosaicPreview();
        else previewStatus.Text = "";
    }

    private async Task UpdateMosaicPreview()
    {
        if (loadingPreview || !showMosaic || closed) return;
        loadingPreview = true;
        try
        {
            while (showMosaic && !closed)
            {
                var item = history.Items.FirstOrDefault(x => !previewLoaded.Contains(x.Id) && !previewFailed.Contains(x.Id));
                if (item is null) break;
                previewStatus.Text = $"Vorschau lädt: {previewLoaded.Count} / {history.Items.Count}";
                try
                {
                    // Decode thumbnails away from the UI thread; never hold every print-resolution photo in memory.
                    int decodeWidth = (int)Math.Clamp(1600.0 / mosaicGrid.Columns, 16, 300);
                    var source = await Task.Run(() => LoadSource(item.TilePath, decodeWidth));
                    if (closed) return;
                    var image = new Image { Width = cellWidth, Height = CellHeight, Stretch = Stretch.Fill, Source = source };
                    Canvas.SetLeft(image, item.Column * cellWidth); Canvas.SetTop(image, item.Row * CellHeight);
                    mosaicLayer.Children.Add(image); previewLoaded.Add(item.Id);
                }
                catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException)
                { previewFailed.Add(item.Id); }
            }
        }
        finally
        {
            loadingPreview = false;
            previewStatus.Text = showMosaic
                ? $"{previewLoaded.Count} Fotokacheln{(previewFailed.Count == 0 ? "" : $" · {previewFailed.Count} nicht lesbar")}" : "";
        }
    }

    public void NewArrival()
    {
        // Give each new image a full slide interval, even if the previous timer was about to tick.
        ResetTimer(); Refresh();
        if (showMosaic) _ = UpdateMosaicPreview();
    }

    private void ResetTimer() { slideshow.Stop(); if (history.IsPlaying) slideshow.Start(); }

    private void Refresh()
    {
        int missing = GridPlanning.Remaining(mosaicGrid, history.Items.Count);
        remaining.Text = missing == 0 ? "Mosaik vollständig!" : $"{missing} {(missing == 1 ? "Bild fehlt" : "Bilder fehlen")} noch";
        play.Content = history.IsPlaying ? "Diashow pausieren" : "Diashow starten";
        if (!history.IsPlaying) slideshow.Stop();
        foreach (var item in history.Items)
        {
            if (occupied.ContainsKey(item.Id)) continue;
            var marker = new Rectangle { Width = 8, Height = 8, Fill = Brushes.LimeGreen };
            Canvas.SetLeft(marker, item.Column * cellWidth + 4); Canvas.SetTop(marker, item.Row * CellHeight + 4);
            wall.Children.Add(marker); occupied[item.Id] = marker;
        }
        var current = history.Current;
        if (current is null)
        {
            coordinate.Text = "Bereit"; position.Text = "Warte auf das erste Foto";
            highlight.Visibility = Visibility.Collapsed; photo.Source = null; return;
        }
        Canvas.SetLeft(highlight, current.Column * cellWidth); Canvas.SetTop(highlight, current.Row * CellHeight);
        highlight.Visibility = Visibility.Visible;
        coordinate.Text = mosaicGrid.Coordinate(current.Column, current.Row);
        position.Text = $"Bild {history.Index + 1} von {history.Items.Count} · Spalte {current.Column + 1}, Zeile {current.Row + 1}";
        try { photo.Source = LoadSource(current.TilePath); }
        catch (Exception ex) when (ex is IOException or NotSupportedException)
        { photo.Source = null; position.Text += " · Bilddatei nicht lesbar"; }
    }

    private void ToggleFullscreen()
    {
        if (WindowStyle == WindowStyle.None)
        { WindowStyle = WindowStyle.SingleBorderWindow; WindowState = WindowState.Normal; }
        else { WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; }
    }

    internal static Button ActionButton(string title, Action action)
    {
        var button = new Button { Content = title, Margin = new Thickness(4), Padding = new Thickness(12, 8, 12, 8) };
        button.Click += (_, _) => action(); return button;
    }

    internal static BitmapImage LoadSource(string path, int decodeWidth = 0)
    {
        using var stream = File.OpenRead(path); return ReadSource(stream, decodeWidth);
    }
    internal static BitmapImage ToSource(SKBitmap bitmap)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream(); return ReadSource(stream);
    }
    private static BitmapImage ReadSource(Stream stream, int decodeWidth = 0)
    {
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        if (decodeWidth > 0) image.DecodePixelWidth = decodeWidth;
        image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
}
