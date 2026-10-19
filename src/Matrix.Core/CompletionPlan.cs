namespace Matrix.Core;

public sealed record CompletionCell(int Column, int Row, string? PhotoPath);

public static class CompletionPlan
{
    public static IReadOnlyList<CompletionCell> Create(Grid grid, IEnumerable<PresentationItem> occupied,
        IReadOnlyList<string>? photoPaths = null)
    {
        grid.Validate();
        if (photoPaths is not null && (photoPaths.Count == 0 || photoPaths.Any(string.IsNullOrWhiteSpace)))
            throw new ArgumentException("Für den Abschluss mit Fotos bitte mindestens ein Foto auswählen.");
        var used = new HashSet<int>();
        foreach (var item in occupied)
        { grid.Coordinate(item.Column, item.Row); used.Add(item.Row * grid.Columns + item.Column); }
        var free = Enumerable.Range(0, grid.Columns * grid.Rows).Where(i => !used.Contains(i)).ToArray();
        Random.Shared.Shuffle(free);
        var photos = photoPaths?.ToArray();
        if (photos is not null) Random.Shared.Shuffle(photos);
        return free.Select((cell, index) => new CompletionCell(cell % grid.Columns, cell / grid.Columns,
            photos is null ? null : photos[index % photos.Length])).ToArray();
    }
}
