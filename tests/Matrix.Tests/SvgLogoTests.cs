using Matrix.Core;
using SkiaSharp;
using Xunit;

public sealed class SvgLogoTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    public SvgLogoTests() => Directory.CreateDirectory(directory);
    public void Dispose() => Directory.Delete(directory, true);
    private string Svg(string source, string name = "logo.svg")
    { string path = Path.Combine(directory, name); File.WriteAllText(path, source); return path; }

    [Fact] public void TinySvgIsRenderedAsVectorsAtTileResolutionNotUpscaledFromItsNominalPixels()
    {
        string path = Svg("""
            <svg xmlns="http://www.w3.org/2000/svg" width="3" height="1" viewBox="0 0 3 1">
              <rect width="1.5" height="1" fill="red"/>
              <rect x="1.5" width="1.5" height="1" fill="blue"/>
              <rect x="2.245" y=".1" width=".01" height=".8" fill="#00ff00"/>
            </svg>
            """);
        var grid = new Grid(2, 1);
        using var tile = Mosaic.RenderLogoTile(path, grid, 1, 0);
        Assert.Equal(1800, tile.Width); Assert.Equal(1200, tile.Height);
        Assert.Equal(SKColors.Blue, tile.GetPixel(0, 0));
        Assert.Equal(SKColors.Lime, tile.GetPixel(900, 600)); // 0.01 SVG units become a sharp 12-pixel stripe.
        Assert.Equal(SKColors.Blue, tile.GetPixel(885, 600));
        Assert.Equal(SKColors.Blue, tile.GetPixel(915, 600));
    }

    [Fact] public void SvgTransparencyAndWholeLayerOpacityPreserveThePhoto()
    {
        string path = Svg("""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 3 2">
              <rect width="1.5" height="2" fill="blue"/>
            </svg>
            """);
        string photoPath = Path.Combine(directory, "photo.png");
        using (var photo = new SKBitmap(600, 400))
        {
            photo.Erase(SKColors.Red);
            using var data = photo.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(photoPath); data.SaveTo(file);
        }
        using var tile = Mosaic.Render(photoPath, path, new Grid(1, 1), 0, 0);
        var overlay = tile.GetPixel(200, 600);
        Assert.InRange(overlay.Red, (byte)152, (byte)154);
        Assert.InRange(overlay.Blue, (byte)101, (byte)103);
        Assert.Equal(SKColors.Red, tile.GetPixel(1600, 600));
        using var preview = Mosaic.RenderMaster(path, new Grid(1, 1));
        Assert.Equal(SKColors.Blue, preview.GetPixel(10, 30));
        Assert.Equal(SKColors.White, preview.GetPixel(90, 30));
    }

    [Fact] public void ExtensionlessProjectCopyRetainsSvgGeometryWithOffsetViewBoxAndInternalReferences()
    {
        string path = Svg("""
            <svg xmlns="http://www.w3.org/2000/svg" width="300" height="200" viewBox="10 20 30 20">
              <defs><linearGradient id="paint"><stop stop-color="red"/><stop offset="1" stop-color="blue"/></linearGradient></defs>
              <path d="M10 20H40V40H10Z" fill="url(#paint)"/>
            </svg>
            """, "master");
        var info = Mosaic.ReadLogoInfo(path);
        Assert.True(info.IsVector);
        Assert.InRange(info.Width / info.Height, 1.499, 1.501);
        using var tile = Mosaic.RenderLogoTile(path, new Grid(1, 1), 0, 0);
        Assert.True(tile.GetPixel(100, 600).Red > tile.GetPixel(1700, 600).Red);
        Assert.True(tile.GetPixel(1700, 600).Blue > tile.GetPixel(100, 600).Blue);
        Assert.Equal((byte)255, tile.GetPixel(900, 600).Alpha);
    }

    [Fact] public void FractionalVectorDimensionsProduceTheCorrectGridSuggestion()
    {
        string path = Svg("""<svg xmlns="http://www.w3.org/2000/svg" width=".3" height=".2" viewBox="0 0 3 2"><path d="M0 0H3V2H0Z"/></svg>""");
        var info = Mosaic.ReadLogoInfo(path);
        var suggestion = GridPlanning.Suggest(info.Width, info.Height, 25);
        Assert.Equal(5, suggestion.Columns); Assert.Equal(5, suggestion.Rows);
    }

    [Theory]
    [InlineData("""<image href="https://example.com/photo.png"/>""")]
    [InlineData("""<use href="file:///tmp/not-a-project-file.svg#logo"/>""")]
    [InlineData("""<style>@import "https://example.com/styles.css";</style>""")]
    [InlineData("""<rect width="3" height="2" fill="url('https://example.com/fill.svg#paint')"/>""")]
    public void MissingExternalResourcesAreRejectedInsteadOfSilentlyChangingLogo(string content)
    {
        string path = Svg($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 3 2">{content}</svg>""");
        Assert.Throws<ArgumentException>(() => Mosaic.ReadLogoInfo(path));
    }

    [Fact] public void StandardSvgDoctypeDoesNotNeedAnExternalDtd()
    {
        string path = Svg("""
            <?xml version="1.0"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 3 2"><rect width="3" height="2" fill="red"/></svg>
            """);
        using var tile = Mosaic.RenderLogoTile(path, new Grid(1, 1), 0, 0);
        Assert.Equal(SKColors.Red, tile.GetPixel(900, 600));
    }

    [Fact] public void EmbeddedRasterImageRemainsAvailableWithoutExternalFiles()
    {
        using var bitmap = new SKBitmap(3, 2); bitmap.Erase(SKColors.Lime);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        string encoded = Convert.ToBase64String(data.ToArray());
        string path = Svg($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 3 2"><image width="3" height="2" href="data:image/png;base64,{encoded}"/></svg>""");
        using var tile = Mosaic.RenderLogoTile(path, new Grid(1, 1), 0, 0);
        Assert.Equal(SKColors.Lime, tile.GetPixel(900, 600));
    }
}
