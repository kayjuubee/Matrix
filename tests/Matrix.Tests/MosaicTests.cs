using Matrix.Core;
using SkiaSharp;
using Xunit;

public class MosaicTests
{
    private static void Save(SKBitmap image, string path)
    { using var data = image.Encode(SKEncodedImageFormat.Png, 100); using var stream = File.Create(path); data.SaveTo(stream); }
    [Fact] public void CoordinatesSupportMultipleLetters()
    {
        var grid = new Grid(100, 15);
        Assert.Equal("A01", grid.Coordinate(0, 0));
        Assert.Equal("Z15", grid.Coordinate(25, 14));
        Assert.Equal("AA01", grid.Coordinate(26, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Coordinate(100, 0));
    }
    [Fact] public void RenderUsesCorrectMasterCellAndOpacity()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            string photoPath = Path.Combine(dir, "photo.png"), masterPath = Path.Combine(dir, "master.png");
            using (var photo = new SKBitmap(200, 200))
            { photo.Erase(SKColors.White); Save(photo, photoPath); }
            using (var master = new SKBitmap(400, 200))
            {
                master.Erase(SKColors.Red);
                for (int y = 0; y < 200; y++) for (int x = 200; x < 400; x++) master.SetPixel(x, y, SKColors.Blue);
                Save(master, masterPath);
            }
            using var tile = Mosaic.Render(photoPath, masterPath, new Grid(2, 1), 1, 0, 200, 0.5f);
            Assert.Equal(200, tile.Width);
            var pixel = tile.GetPixel(100, 100);
            Assert.InRange(pixel.Red, (byte)126, (byte)129);
            Assert.Equal((byte)255, pixel.Blue);
            Assert.Equal(pixel, tile.GetPixel(0, 0)); // No coordinate or white label on the print.
            Assert.Equal(pixel, tile.GetPixel(10, 10));
            using var preview = Mosaic.RenderMaster(masterPath, new Grid(2, 1));
            Assert.Equal(preview.Width, preview.Height * 2);
            Assert.Equal(SKColors.Red, preview.GetPixel(25, 50));
            Assert.Equal(SKColors.Blue, preview.GetPixel(175, 50));
            Assert.Throws<ArgumentOutOfRangeException>(() => Mosaic.Render(photoPath, masterPath, new Grid(2, 1), 2, 0));
        }
        finally { Directory.Delete(dir, true); }
    }
}
