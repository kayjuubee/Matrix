using Matrix.Core;
using SkiaSharp;
using Xunit;

public class CompletionTests
{
    [Fact] public void PlanUsesOnlyUnoccupiedFieldsAndRepeatsSelectedPhotosWhenNeeded()
    {
        var grid = new Grid(3, 2);
        var existing = new PresentationItem("existing", "old.png", 2, 0);
        var plan = CompletionPlan.Create(grid, [existing], ["first.jpg", "second.jpg"]);
        Assert.Equal(5, plan.Count);
        Assert.DoesNotContain(plan, cell => cell.Column == 2 && cell.Row == 0);
        Assert.Equal(5, plan.Select(cell => (cell.Column, cell.Row)).Distinct().Count());
        Assert.All(plan, cell => Assert.Contains(cell.PhotoPath, new[] { "first.jpg", "second.jpg" }));
        Assert.Contains(plan, cell => cell.PhotoPath == "first.jpg");
        Assert.Contains(plan, cell => cell.PhotoPath == "second.jpg");
        var updated = new[] { existing }.Concat(plan.Select((cell, i) => new PresentationItem($"fill-{i}", "fill.png", cell.Column, cell.Row)));
        Assert.Empty(CompletionPlan.Create(grid, updated));
        Assert.Throws<ArgumentException>(() => CompletionPlan.Create(grid, [], []));
    }

    [Fact] public void WithoutPhotosEachMissingCellContainsOnlyItsFullColorLogoSection()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try
        {
            using (var master = new SKBitmap(600, 200))
            {
                master.Erase(SKColors.Red);
                using (var canvas = new SKCanvas(master))
                using (var paint = new SKPaint { Color = SKColors.Blue }) canvas.DrawRect(300, 0, 300, 200, paint);
                using var data = master.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(path); data.SaveTo(file);
            }
            var grid = new Grid(2, 1);
            var plan = CompletionPlan.Create(grid, [new PresentationItem("taken", "old.png", 0, 0)]);
            Assert.Single(plan); Assert.Null(plan[0].PhotoPath);
            using var tile = Mosaic.RenderLogoTile(path, grid, plan[0].Column, plan[0].Row);
            Assert.Equal(1800, tile.Width); Assert.Equal(1200, tile.Height);
            Assert.Equal(SKColors.Blue, tile.GetPixel(0, 0));
            Assert.Equal(SKColors.Blue, tile.GetPixel(900, 600));
            Assert.Equal(SKColors.Blue, tile.GetPixel(1799, 1199));
        }
        finally { File.Delete(path); }
    }
}
