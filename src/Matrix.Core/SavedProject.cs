namespace Matrix.Core;

public enum PrintState { NotRequested, Pending, Submitting, Submitted, NeedsReview }

public sealed record SavedPhoto(string Id, int Column, int Row, PrintState PrintState = PrintState.NotRequested);

// Projects saved before format support used square tiles. Never silently reinterpret their layout.
public sealed record SavedProject(int Columns, int Rows, List<SavedPhoto> Photos,
    PhotoFormat Format = PhotoFormat.Square, string? PrinterName = null);

public static class PrintRecovery
{
    // The spooler may already have accepted a job when the application stops.
    // Recover it for manual review, never as an automatic retry.
    public static SavedPhoto AfterRestart(SavedPhoto photo) => photo.PrintState == PrintState.Submitting
        ? photo with { PrintState = PrintState.NeedsReview } : photo;

    public static SavedPhoto? Next(IEnumerable<SavedPhoto> photos) => photos.FirstOrDefault(p => p.PrintState == PrintState.Pending);
}
