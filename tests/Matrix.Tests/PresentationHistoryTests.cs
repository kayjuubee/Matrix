using Matrix.Core;
using Xunit;

public class PresentationHistoryTests
{
    [Fact] public void SelectingSpecificRemotePhotoPausesSlideshowAndNewArrivalStillWins()
    {
        var history = new PresentationHistory(new Grid(5, 1));
        history.Add(Item(0)); history.Add(Item(1)); history.ToggleSlideshow();
        Assert.True(history.Select(Item(0).Id));
        Assert.Equal(Item(0), history.Current); Assert.False(history.IsPlaying);
        Assert.False(history.Select("missing")); Assert.Equal(Item(0), history.Current);
        history.Add(Item(2)); Assert.Equal(Item(2), history.Current);
    }

    private static PresentationItem Item(int n) => new($"photo-{n}", $"tile-{n}.png", n, 0);

    [Fact] public void NewArrivalInterruptsHistoryAndDuplicateDoesNotMoveSelection()
    {
        var history = new PresentationHistory(new Grid(5, 1));
        history.Add(Item(0)); history.Add(Item(1)); history.Previous();
        Assert.Equal(Item(0), history.Current);
        Assert.False(history.Add(Item(1)));
        Assert.Equal(Item(0), history.Current);
        history.Add(Item(2));
        Assert.Equal(Item(2), history.Current);
        Assert.False(history.IsPlaying);
    }

    [Fact] public void SlideshowWrapsAndNewArrivalBecomesCurrentWithoutStoppingPlayback()
    {
        var history = new PresentationHistory(new Grid(5, 1));
        history.Add(Item(0)); history.Add(Item(1)); history.ToggleSlideshow();
        history.Tick(); Assert.Equal(Item(0), history.Current);
        history.Add(Item(2)); Assert.Equal(Item(2), history.Current);
        Assert.True(history.IsPlaying);
        history.Previous(); Assert.False(history.IsPlaying);
        history.Tick(); Assert.Equal(Item(1), history.Current);
        history.Latest(); Assert.Equal(Item(2), history.Current);
    }

    [Fact] public void EmptyHistoryAndNavigationBoundariesAreSafe()
    {
        var history = new PresentationHistory(new Grid(2, 1));
        history.Previous(); history.Next(); history.Latest(); history.ToggleSlideshow(); history.Tick();
        Assert.Null(history.Current);
        history.Add(Item(0)); history.Previous(); Assert.Equal(0, history.Index);
        history.Next(); Assert.Equal(0, history.Index);
        Assert.Throws<InvalidOperationException>(() => history.Add(new("other", "other.png", 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => history.Add(Item(2)));
    }
}
