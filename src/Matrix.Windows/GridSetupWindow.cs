using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Matrix.Core;
using MosaicGrid = Matrix.Core.Grid;

namespace Matrix.Windows;

internal sealed class GridSetupWindow : Window
{
    private readonly string logoPath;
    private readonly (double Width, double Height, bool IsVector) logoSize;
    private readonly TextBox target = new() { Text = "300", Width = 90 };
    private readonly TextBox columns = new() { Width = 70 };
    private readonly TextBox rows = new() { Width = 70 };
    private readonly ComboBox format = new() { ItemsSource = new[] { "15 × 10 cm · Querformat", "10 × 15 cm · Hochformat" }, SelectedIndex = 0 };
    private readonly Image preview = new() { Height = 220, Stretch = Stretch.Uniform };
    private readonly TextBlock description = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) };
    private readonly Button create;
    private bool suggesting;
    public MosaicGrid? SelectedGrid { get; private set; }

    public GridSetupWindow(string logoPath)
    {
        this.logoPath = logoPath;
        logoSize = Mosaic.ReadLogoInfo(logoPath);
        Title = "Logo gewählt – Raster planen"; Width = 600; Height = 660;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "Passendes Raster für dein Logo", FontSize = 24, Margin = new Thickness(0, 0, 0, 16) });
        panel.Children.Add(new TextBlock { Text = $"Logo: {logoSize.Width:0.###} × {logoSize.Height:0.###} {(logoSize.IsVector ? "SVG-Zeicheneinheiten · skalierbar" : "Pixel")}" });
        panel.Children.Add(new TextBlock { Text = "Ungefähre gewünschte Bildanzahl", Margin = new Thickness(0, 12, 0, 4) });
        panel.Children.Add(target);
        panel.Children.Add(new TextBlock { Text = "Papierausrichtung", Margin = new Thickness(0, 12, 0, 4) });
        panel.Children.Add(format);
        panel.Children.Add(PresentationWindow.ActionButton("Raster erneut vorschlagen", Suggest));
        var dimensions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 4) };
        dimensions.Children.Add(new TextBlock { Text = "Spalten ", VerticalAlignment = VerticalAlignment.Center }); dimensions.Children.Add(columns);
        dimensions.Children.Add(new TextBlock { Text = "   Zeilen ", VerticalAlignment = VerticalAlignment.Center }); dimensions.Children.Add(rows);
        panel.Children.Add(dimensions); panel.Children.Add(description); panel.Children.Add(preview);
        create = PresentationWindow.ActionButton("Projekt mit diesem Raster erstellen", () =>
        { if (SelectedGrid is not null) DialogResult = true; });
        panel.Children.Add(create);
        target.TextChanged += (_, _) => Suggest();
        format.SelectionChanged += (_, _) => Suggest();
        columns.TextChanged += (_, _) => { if (!suggesting) UpdatePreview(); };
        rows.TextChanged += (_, _) => { if (!suggesting) UpdatePreview(); };
        Suggest();
    }

    private void Suggest()
    {
        if (!int.TryParse(target.Text, out int count) || count is < 1 or > 10000)
        { SelectedGrid = null; create.IsEnabled = false; description.Text = "Bitte eine gewünschte Bildanzahl von 1 bis 10.000 eingeben."; return; }
        var suggestion = GridPlanning.Suggest(logoSize.Width, logoSize.Height, count,
            format.SelectedIndex == 1 ? PhotoFormat.Portrait10x15 : PhotoFormat.Landscape10x15);
        suggesting = true;
        columns.Text = suggestion.Columns.ToString(); rows.Text = suggestion.Rows.ToString();
        suggesting = false; UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            if (!int.TryParse(columns.Text, out int c) || !int.TryParse(rows.Text, out int r))
                throw new ArgumentException("Bitte Spalten und Zeilen eingeben (jeweils 1 bis 100).");
            var grid = new MosaicGrid(c, r, format.SelectedIndex == 1 ? PhotoFormat.Portrait10x15 : PhotoFormat.Landscape10x15);
            grid.Validate();
            double aspectRatio = grid.WallAspect / ((double)logoSize.Width / logoSize.Height);
            double retained = Math.Min(aspectRatio, 1 / aspectRatio);
            int tileWidth = grid.Format == PhotoFormat.Landscape10x15 ? 15 : 10;
            int tileHeight = grid.Format == PhotoFormat.Landscape10x15 ? 10 : 15;
            description.Text = $"{c} Spalten × {r} Zeilen = {c * r} Bilder · Wand ca. {c * tileWidth} × {r * tileHeight} cm\n" +
                $"Motivbeschnitt: ca. {1 - retained:P1}. Du kannst das Raster manuell ändern.";
            using var bitmap = Mosaic.RenderMaster(logoPath, grid);
            preview.Source = PresentationWindow.ToSource(bitmap);
            SelectedGrid = grid; create.IsEnabled = true;
        }
        catch (Exception ex)
        { SelectedGrid = null; create.IsEnabled = false; description.Text = ex.Message; preview.Source = null; }
    }
}
