namespace Matrix.Core;

public sealed record PresentationItem(string Id, string TilePath, int Column, int Row);

/// <summary>One shared selection for the photo, coordinate and highlighted grid cell.</summary>
public sealed class PresentationHistory(Grid grid)
{
    private readonly List<PresentationItem> items = [];
    public IReadOnlyList<PresentationItem> Items => items.AsReadOnly();
    public int Index { get; private set; } = -1;
    public PresentationItem? Current => Index < 0 ? null : items[Index];
    public bool IsPlaying { get; private set; }

    public bool Add(PresentationItem item)
    {
        grid.Coordinate(item.Column, item.Row);
        if (items.Any(x => x.Id == item.Id)) return false;
        if (items.Any(x => x.Column == item.Column && x.Row == item.Row))
            throw new InvalidOperationException("Dieses Matrixfeld ist bereits belegt.");
        items.Add(item);
        Index = items.Count - 1; // New arrivals always interrupt browsing, even while paused.
        return true;
    }

    public void Previous() { IsPlaying = false; if (Index > 0) Index--; }
    public void Next() { IsPlaying = false; if (Index + 1 < items.Count) Index++; }
    public void Latest() { IsPlaying = false; Index = items.Count - 1; }
    public bool Select(string id)
    {
        int index = items.FindIndex(item => item.Id == id);
        if (index < 0) return false;
        Index = index; IsPlaying = false; return true;
    }
    public void ToggleSlideshow() => IsPlaying = !IsPlaying;
    public void Tick() { if (IsPlaying && items.Count > 0) Index = (Index + 1) % items.Count; }
}
