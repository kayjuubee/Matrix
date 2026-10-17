using Matrix.Core;
using Xunit;

public class GridPlanningTests
{
    [Theory]
    [InlineData(1500, 1000, PhotoFormat.Landscape10x15)]
    [InlineData(1000, 1000, PhotoFormat.Landscape10x15)]
    [InlineData(3000, 1000, PhotoFormat.Landscape10x15)]
    [InlineData(1000, 2000, PhotoFormat.Landscape10x15)]
    [InlineData(1500, 1000, PhotoFormat.Portrait10x15)]
    public void SuggestionsFitLogoAndRespectApproximatePhotoCount(int width, int height, PhotoFormat format)
    {
        var grid = GridPlanning.Suggest(width, height, 300, format);
        grid.Validate();
        Assert.Equal(format, grid.Format);
        Assert.InRange(grid.Columns * grid.Rows, 240, 360);
        Assert.InRange(Math.Abs(grid.WallAspect / ((double)width / height) - 1), 0, 0.05);
    }

    [Fact] public void TargetCountChangesWallSizeAndInvalidInputIsRejected()
    {
        var small = GridPlanning.Suggest(1500, 1000, 25);
        var big = GridPlanning.Suggest(1500, 1000, 300);
        Assert.Equal(25, small.Columns * small.Rows);
        Assert.True(big.Columns * big.Rows > small.Columns * small.Rows);
        Assert.Throws<ArgumentOutOfRangeException>(() => GridPlanning.Suggest(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => GridPlanning.Suggest(100, 100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GridPlanning.Suggest(100, 100, 10001));
    }

    [Fact] public void RandomAssignmentFillsEveryFreeCellOnceAndPreservesExistingPositions()
    {
        var grid = new Grid(5, 5);
        var existing = new PresentationItem("existing", "old.png", 3, 2);
        var history = new PresentationHistory(grid);
        history.Add(existing);
        while (history.Items.Count < 25)
        {
            int cell = GridPlanning.RandomFreeCell(grid, history.Items)!.Value;
            Assert.DoesNotContain(history.Items, p => p.Row * grid.Columns + p.Column == cell);
            history.Add(new($"photo-{cell}", $"{cell}.png", cell % grid.Columns, cell / grid.Columns));
        }
        Assert.Null(GridPlanning.RandomFreeCell(grid, history.Items));
        Assert.Equal(25, history.Items.Select(p => (p.Column, p.Row)).Distinct().Count());
        Assert.Equal(existing, history.Items[0]);
    }

    [Fact] public void RemainingCountTracksImportsRatherThanHistorySelectionOrDuplicateEvents()
    {
        var grid = new Grid(2, 1);
        var history = new PresentationHistory(grid);
        Assert.Equal(2, GridPlanning.Remaining(grid, history.Items.Count));
        var first = new PresentationItem("first", "first.png", 1, 0);
        history.Add(first); history.Add(first);
        Assert.Equal(1, GridPlanning.Remaining(grid, history.Items.Count));
        history.Add(new("second", "second.png", 0, 0)); history.Previous();
        Assert.Equal(0, GridPlanning.Remaining(grid, history.Items.Count));
        Assert.Throws<ArgumentOutOfRangeException>(() => GridPlanning.Remaining(grid, 3));
    }
}
