using Matrix.Core;
using SkiaSharp;

if (args.Length != 7)
{
    Console.Error.WriteLine("Aufruf: Matrix.Cli foto motiv ausgabe.png spalten zeilen spalte zeile (Positionen ab 1)");
    return 1;
}
try
{
    var grid = new Grid(int.Parse(args[3]), int.Parse(args[4]));
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
