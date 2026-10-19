using SkiaSharp;

namespace Matrix.Core;

public enum PhotoFormat { Square, Landscape10x15, Portrait10x15 }

public sealed record Grid(int Columns, int Rows, PhotoFormat Format = PhotoFormat.Landscape10x15)
{
    public double TileAspect => Format switch
    {
        PhotoFormat.Square => 1,
        PhotoFormat.Landscape10x15 => 1.5,
        PhotoFormat.Portrait10x15 => 1 / 1.5,
        _ => throw new ArgumentOutOfRangeException(nameof(Format))
    };
    public double WallAspect => (double)Columns / Rows * TileAspect;

    public void Validate()
    {
        if (Columns is < 1 or > 100 || Rows is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Columns), "Raster: jeweils 1 bis 100 Felder.");
        _ = TileAspect;
    }
    public string Coordinate(int column, int row)
    {
        Validate();
        if (column < 0 || column >= Columns || row < 0 || row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(column));
        var letters = "";
        for (var n = column + 1; n > 0; n = (n - 1) / 26)
            letters = (char)('A' + (n - 1) % 26) + letters;
        return $"{letters}{row + 1:00}";
    }
}

public static class Mosaic
{
    public static (int Width, int Height) ReadOrientedSize(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new ArgumentException("Unbekanntes Bildformat.");
        return (int)codec.EncodedOrigin >= 5 ? (codec.Info.Height, codec.Info.Width) : (codec.Info.Width, codec.Info.Height);
    }

    public static (double Width, double Height, bool IsVector) ReadLogoInfo(string path)
    {
        using var artwork = LogoArtwork.Open(path);
        return (artwork.Bounds.Width, artwork.Bounds.Height, artwork.IsVector);
    }

    public static SKBitmap Render(string photoPath, string masterPath, Grid grid,
        int column, int row, int size = 1800, float opacity = 0.4f)
        => RenderTile(photoPath, masterPath, grid, column, row, size, opacity);

    public static SKBitmap RenderLogoTile(string masterPath, Grid grid, int column, int row, int size = 1800)
        => RenderTile(null, masterPath, grid, column, row, size, 1);

    private static SKBitmap RenderTile(string? photoPath, string masterPath, Grid grid,
        int column, int row, int size, float opacity)
    {
        grid.Coordinate(column, row);
        if (size is < 100 or > 2400 || !float.IsFinite(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(size));
        using var master = LogoArtwork.Open(masterPath);
        using var photo = photoPath is null ? null : LoadOriented(photoPath);
        int width = grid.TileAspect >= 1 ? size : (int)Math.Round(size * grid.TileAspect);
        int height = grid.TileAspect >= 1 ? (int)Math.Round(size / grid.TileAspect) : size;
        var tile = new SKBitmap(width, height);
        try
        {
            using (var canvas = new SKCanvas(tile))
            {
                canvas.Clear(SKColors.White);
                // Fit the complete photo without cropping, stretching or implicit rotation.
                if (photo is not null)
                {
                    float scale = Math.Min((float)width / photo.Width, (float)height / photo.Height);
                    float photoWidth = photo.Width * scale, photoHeight = photo.Height * scale;
                    var destination = new SKRect((width - photoWidth) / 2, (height - photoHeight) / 2,
                        (width + photoWidth) / 2, (height + photoHeight) / 2);
                    canvas.DrawBitmap(photo, new SKRect(0, 0, photo.Width, photo.Height), destination, new SKSamplingOptions(SKFilterMode.Linear));
                }
                var crop = master.Crop((float)grid.WallAspect);
                var cell = new SKRect(crop.Left + column * crop.Width / grid.Columns,
                    crop.Top + row * crop.Height / grid.Rows,
                    crop.Left + (column + 1) * crop.Width / grid.Columns,
                    crop.Top + (row + 1) * crop.Height / grid.Rows);
                master.Draw(canvas, cell, new SKRect(0, 0, width, height), opacity);
            }
            return tile;
        }
        catch { tile.Dispose(); throw; }
    }

    internal static SKBitmap LoadOriented(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new ArgumentException("Unbekanntes Bildformat.");
        using var decoded = SKBitmap.Decode(codec) ?? throw new ArgumentException("Bild kann nicht gelesen werden.");
        var origin = codec.EncodedOrigin;
        bool swap = (int)origin >= 5;
        var result = new SKBitmap(swap ? decoded.Height : decoded.Width, swap ? decoded.Width : decoded.Height);
        using var canvas = new SKCanvas(result);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(result.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(result.Width, result.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, result.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(result.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(result.Width, result.Height); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, result.Height); canvas.RotateDegrees(-90); break;
        }
        canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return result;
    }

    public static SKBitmap RenderMaster(string path, Grid grid)
    {
        grid.Validate();
        using var source = LogoArtwork.Open(path);
        int width = Math.Min(1600, grid.Columns * 100);
        int height = Math.Max(1, (int)Math.Round(width / grid.WallAspect));
        if (height > 1600) { width = Math.Max(1, width * 1600 / height); height = 1600; }
        var result = new SKBitmap(width, height);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.White);
        source.Draw(canvas, source.Crop((float)grid.WallAspect), new SKRect(0, 0, width, height));
        return result;
    }
}
