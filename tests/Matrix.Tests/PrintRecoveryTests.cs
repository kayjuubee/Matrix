using System.Text.Json;
using Matrix.Core;
using Xunit;

public class PrintRecoveryTests
{
    [Fact] public void RestartDoesNotAutomaticallyRepeatUncertainOrSubmittedJobs()
    {
        var photos = new[]
        {
            new SavedPhoto("original-only", 0, 0),
            new SavedPhoto("already-sent", 1, 0, PrintState.Submitted),
            new SavedPhoto("interrupted", 2, 0, PrintState.Submitting),
            new SavedPhoto("waiting", 3, 0, PrintState.Pending)
        };
        var project = new SavedProject(5, 5, [.. photos], PhotoFormat.Landscape10x15, "DNP RX1");
        var restored = JsonSerializer.Deserialize<SavedProject>(JsonSerializer.Serialize(project))!;
        var recovered = restored.Photos.Select(PrintRecovery.AfterRestart).ToList();
        Assert.Equal("DNP RX1", restored.PrinterName);
        Assert.Equal(PrintState.NotRequested, recovered[0].PrintState);
        Assert.Equal(PrintState.Submitted, recovered[1].PrintState);
        Assert.Equal(PrintState.NeedsReview, recovered[2].PrintState);
        Assert.Equal("waiting", PrintRecovery.Next(recovered)!.Id);
        recovered[3] = recovered[3] with { PrintState = PrintState.Submitted };
        Assert.Null(PrintRecovery.Next(recovered));
        // An explicit reprint keeps the original wall coordinate.
        recovered[2] = recovered[2] with { PrintState = PrintState.Pending };
        Assert.Equal(2, PrintRecovery.Next(recovered)!.Column);
    }
}
