using Matrix.Core;
using SkiaSharp;

if (args.Length is not (7 or 8))
{
    Console.Error.WriteLine("Aufruf: Matrix.Cli foto motiv ausgabe.png spalten zeilen spalte zeile [quer|hoch] (Positionen ab 1)");
    return 1;
}
try
{
    var format = args.Length == 7 ? PhotoFormat.Landscape10x15 : args[7] switch
    {
        "quer" => PhotoFormat.Landscape10x15,
        "hoch" => PhotoFormat.Portrait10x15,
        _ => throw new ArgumentException("Format muss quer oder hoch sein.")
    };
    var grid = new Grid(int.Parse(args[3]), int.Parse(args[4]), format);
    int column = int.Parse(args[5]) - 1, row = int.Parse(args[6]) - 1;
    if (File.Exists(args[2])) throw new IOException("Ausgabedatei existiert bereits.");
    using var tile = Mosaic.Render(args[0], args[1], grid, column, row);
    using var data = tile.Encode(SKEncodedImageFormat.Png, 100);
    using var output = new FileStream(args[2], FileMode.CreateNew);
    data.SaveTo(output);
    Console.WriteLine($"Kachel {grid.Coordinate(column, row)} gespeichert: {args[2]}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
