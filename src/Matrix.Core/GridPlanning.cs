using System.Security.Cryptography;

namespace Matrix.Core;

public static class GridPlanning
{
    public static Grid Suggest(int logoWidth, int logoHeight, int targetCount = 300,
        PhotoFormat format = PhotoFormat.Landscape10x15)
    {
        if (logoWidth <= 0 || logoHeight <= 0 || targetCount is < 1 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(targetCount));
        double logoAspect = (double)logoWidth / logoHeight;
        Grid? best = null;
        double bestScore = double.PositiveInfinity;
        for (int columns = 1; columns <= 100; columns++)
        for (int rows = 1; rows <= 100; rows++)
        {
            var candidate = new Grid(columns, rows, format);
            double aspectError = Math.Log(candidate.WallAspect / logoAspect);
            double countError = Math.Log((double)(columns * rows) / targetCount);
            double score = 4 * aspectError * aspectError + countError * countError;
            if (score < bestScore) { best = candidate; bestScore = score; }
        }
        return best!;
    }

    public static int? RandomFreeCell(Grid grid, IEnumerable<PresentationItem> occupied)
    {
        grid.Validate();
        var used = new HashSet<int>();
        foreach (var item in occupied)
        {
            grid.Coordinate(item.Column, item.Row);
            used.Add(item.Row * grid.Columns + item.Column);
        }
        var free = Enumerable.Range(0, grid.Columns * grid.Rows).Where(i => !used.Contains(i)).ToArray();
        return free.Length == 0 ? null : free[RandomNumberGenerator.GetInt32(free.Length)];
    }

    public static int Remaining(Grid grid, int imported)
    {
        grid.Validate();
        if (imported < 0 || imported > grid.Columns * grid.Rows)
            throw new ArgumentOutOfRangeException(nameof(imported));
        return grid.Columns * grid.Rows - imported;
    }
}
