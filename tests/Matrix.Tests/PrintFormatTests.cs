using System.Text.Json;
using Matrix.Core;
using SkiaSharp;
using Xunit;

public sealed class PrintFormatTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    public PrintFormatTests() => Directory.CreateDirectory(directory);
    public void Dispose() => Directory.Delete(directory, true);

    private string Save(string name, SKBitmap bitmap)
    {
        string path = Path.Combine(directory, name);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path); data.SaveTo(stream); return path;
    }

    [Fact] public void LandscapePrintPreservesEveryCornerOfMatchingPhoto()
    {
        using var photo = new SKBitmap(600, 400);
        photo.Erase(SKColors.Yellow);
        photo.SetPixel(0, 0, SKColors.Red); photo.SetPixel(599, 0, SKColors.Green);
        photo.SetPixel(0, 399, SKColors.Blue); photo.SetPixel(599, 399, SKColors.Black);
        string path = Save("photo.png", photo);
        using var tile = Mosaic.Render(path, path, new Grid(1, 1), 0, 0, 600, 0);
        Assert.Equal(600, tile.Width); Assert.Equal(400, tile.Height);
        Assert.Equal(SKColors.Red, tile.GetPixel(0, 0));
        Assert.Equal(SKColors.Green, tile.GetPixel(599, 0));
        Assert.Equal(SKColors.Blue, tile.GetPixel(0, 399));
        Assert.Equal(SKColors.Black, tile.GetPixel(599, 399));
    }

    [Fact] public void PortraitPhotoFitsLandscapeWithoutCroppingOrStretching()
    {
        using var photo = new SKBitmap(200, 400); photo.Erase(SKColors.Yellow);
        photo.SetPixel(0, 0, SKColors.Red); photo.SetPixel(199, 399, SKColors.Blue);
        string path = Save("portrait.png", photo);
        using var tile = Mosaic.Render(path, path, new Grid(1, 1), 0, 0, 600, 0);
        Assert.Equal(SKColors.White, tile.GetPixel(0, 200));
        Assert.Equal(SKColors.White, tile.GetPixel(599, 200));
        Assert.Equal(SKColors.Red, tile.GetPixel(200, 0));
        Assert.Equal(SKColors.Blue, tile.GetPixel(399, 399));
        Assert.Equal(SKColors.Yellow, tile.GetPixel(300, 200));
    }

    [Theory]
    [InlineData(PhotoFormat.Landscape10x15, 1800, 1200)]
    [InlineData(PhotoFormat.Portrait10x15, 1200, 1800)]
    public void PrintAndWallUseSameRectangularGeometry(PhotoFormat format, int width, int height)
    {
        using var master = new SKBitmap(width * 2, height); master.Erase(SKColors.Red);
        using (var canvas = new SKCanvas(master))
        using (var paint = new SKPaint { Color = SKColors.Blue })
            canvas.DrawRect(width, 0, width, height, paint);
        string path = Save("master.png", master);
        var grid = new Grid(2, 1, format);
        using var tile = Mosaic.Render(path, path, grid, 1, 0, opacity: 1);
        using var preview = Mosaic.RenderMaster(path, grid);
        Assert.Equal(width, tile.Width); Assert.Equal(height, tile.Height);
        Assert.Equal(SKColors.Blue, tile.GetPixel(width / 2, height / 2));
        Assert.Equal(SKColors.Red, preview.GetPixel(preview.Width / 4, preview.Height / 2));
        Assert.Equal(SKColors.Blue, preview.GetPixel(preview.Width * 3 / 4, preview.Height / 2));
        Assert.InRange(Math.Abs((double)preview.Width / preview.Height - grid.WallAspect), 0, 0.03);
    }

    [Fact] public void ProjectRoundTripRetainsFormatAndOldProjectsRemainSquare()
    {
        var saved = new SavedProject(5, 5, [new("photo", 1, 2)], PhotoFormat.Landscape10x15);
        var restored = JsonSerializer.Deserialize<SavedProject>(JsonSerializer.Serialize(saved))!;
        Assert.Equal(PhotoFormat.Landscape10x15, restored.Format);
        Assert.Equal(saved.Photos[0], restored.Photos[0]);
        var legacy = JsonSerializer.Deserialize<SavedProject>("{\"Columns\":5,\"Rows\":5,\"Photos\":[]}")!;
        Assert.Equal(PhotoFormat.Square, legacy.Format);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(5, 5, (PhotoFormat)999).Validate());
    }
}
