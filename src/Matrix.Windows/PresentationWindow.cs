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
    private readonly Rectangle highlight;
    private readonly Image photo = new() { Stretch = Stretch.Uniform };
    private readonly TextBlock coordinate = new() { FontSize = 64, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
    private readonly TextBlock position = new() { FontSize = 18, Foreground = Brushes.LightGray };
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
        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        DockPanel.SetDock(controls, Dock.Bottom); root.Children.Add(controls);
        controls.Children.Add(ActionButton("← Zurück", () => { history.Previous(); Refresh(); }));
        play = ActionButton("Diashow starten", () => { history.ToggleSlideshow(); ResetTimer(); Refresh(); });
        controls.Children.Add(play);
        controls.Children.Add(ActionButton("Weiter →", () => { history.Next(); Refresh(); }));
        controls.Children.Add(ActionButton("Neuestes Bild", () => { history.Latest(); Refresh(); }));
        controls.Children.Add(ActionButton("Vollbild (F11)", ToggleFullscreen));
        var layout = new System.Windows.Controls.Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        root.Children.Add(layout);

        double width = grid.Columns * 60, height = grid.Rows * 60;
        wall = new Canvas { Width = width, Height = height, Background = Brushes.White };
        using (var master = Mosaic.RenderMaster(masterPath, grid))
            wall.Children.Add(new Image { Width = width, Height = height, Stretch = Stretch.Fill, Source = ToSource(master) });
        var viewbox = new Viewbox { Child = wall, Stretch = Stretch.Uniform, Margin = new Thickness(12) };
        layout.Children.Add(viewbox);
        for (int c = 0; c <= grid.Columns; c++)
            wall.Children.Add(new Line { X1 = c * 60, X2 = c * 60, Y1 = 0, Y2 = height, Stroke = Brushes.Black, Opacity = 0.45, StrokeThickness = 1 });
        for (int r = 0; r <= grid.Rows; r++)
            wall.Children.Add(new Line { X1 = 0, X2 = width, Y1 = r * 60, Y2 = r * 60, Stroke = Brushes.Black, Opacity = 0.45, StrokeThickness = 1 });
        highlight = new Rectangle
        {
            Width = 60, Height = 60, Stroke = Brushes.OrangeRed, StrokeThickness = 5,
            Fill = new SolidColorBrush(Color.FromArgb(110, 255, 190, 0)), Visibility = Visibility.Collapsed
        };
        wall.Children.Add(highlight);
        var side = new DockPanel { Margin = new Thickness(20) };
        System.Windows.Controls.Grid.SetColumn(side, 1); layout.Children.Add(side);
        var info = new StackPanel { Margin = new Thickness(0, 12, 0, 12) };
        info.Children.Add(coordinate); info.Children.Add(position);
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
        Closed += (_, _) => slideshow.Stop();
        ResetTimer(); Refresh();
    }

    public void NewArrival()
    {
        // Give each new image a full slide interval, even if the previous timer was about to tick.
        ResetTimer(); Refresh();
    }

    private void ResetTimer() { slideshow.Stop(); if (history.IsPlaying) slideshow.Start(); }

    private void Refresh()
    {
        play.Content = history.IsPlaying ? "Diashow pausieren" : "Diashow starten";
        if (!history.IsPlaying) slideshow.Stop();
        foreach (var item in history.Items)
        {
            if (occupied.ContainsKey(item.Id)) continue;
            var marker = new Rectangle { Width = 8, Height = 8, Fill = Brushes.LimeGreen };
            Canvas.SetLeft(marker, item.Column * 60 + 4); Canvas.SetTop(marker, item.Row * 60 + 4);
            wall.Children.Add(marker); occupied[item.Id] = marker;
        }
        var current = history.Current;
        if (current is null)
        {
            coordinate.Text = "Bereit"; position.Text = "Warte auf das erste Foto";
            highlight.Visibility = Visibility.Collapsed; photo.Source = null; return;
        }
        Canvas.SetLeft(highlight, current.Column * 60); Canvas.SetTop(highlight, current.Row * 60);
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

    internal static BitmapImage LoadSource(string path)
    {
        using var stream = File.OpenRead(path); return ReadSource(stream);
    }
    private static BitmapImage ToSource(SKBitmap bitmap)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream(); return ReadSource(stream);
    }
    private static BitmapImage ReadSource(Stream stream)
    {
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
}
