using System.Text.Json;
using Matrix.Core;
using Xunit;

public class InvitationSettingsTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact] public void DelayUsesLastNewPhotoAndArrivalHidesTheInvitationAgain()
    {
        var settings = new InvitationSettings(DelaySeconds: 20);
        Assert.Null(settings.Message(Start, Start.AddSeconds(19), 87, 100));
        Assert.Equal(settings.InvitationText, settings.Message(Start, Start.AddSeconds(20), 87, 100));
        Assert.Null(settings.Message(Start.AddSeconds(21), Start.AddSeconds(21), 86, 100));
        Assert.Null(settings.Message(Start.AddSeconds(21), Start.AddSeconds(40), 86, 100));
        Assert.Equal(settings.InvitationText, settings.Message(Start.AddSeconds(21), Start.AddSeconds(41), 86, 100));
    }

    [Fact] public void CustomDelayTextsPlaceholdersAndCompletionUseTheSameRules()
    {
        var settings = new InvitationSettings(true, 5, "Hilf uns!\nNoch {rest} von {gesamt}.", "Danke, {gesamt} Felder fertig!");
        Assert.Equal("Hilf uns!\nNoch 2 von 25.", settings.Message(Start, Start.AddSeconds(5), 2, 25));
        Assert.Null(settings.Message(Start, Start.AddSeconds(4), 0, 25));
        Assert.Equal("Danke, 25 Felder fertig!", settings.Message(Start, Start.AddSeconds(5), 0, 25));
        Assert.Null((settings with { Enabled = false }).Message(Start, Start.AddHours(1), 2, 25));
    }

    [Fact] public void ProjectRoundTripPreservesConfigurationAndOldProjectsCanUseDefaults()
    {
        var project = new SavedProject(5, 5, [], PhotoFormat.Landscape10x15, Invitation: new(false, 45, "Willkommen", "Vielen Dank"));
        var restored = JsonSerializer.Deserialize<SavedProject>(JsonSerializer.Serialize(project))!;
        Assert.Equal(project.Invitation, restored.Invitation);
        var legacy = JsonSerializer.Deserialize<SavedProject>("""{"Columns":5,"Rows":5,"Photos":[]}""")!;
        var defaults = legacy.Invitation ?? new InvitationSettings();
        Assert.Equal(20, defaults.DelaySeconds); Assert.True(defaults.Enabled);
        Assert.Throws<ArgumentException>(() => (defaults with { DelaySeconds = 0 }).Validate());
        Assert.Throws<ArgumentException>(() => (defaults with { DelaySeconds = 3601 }).Validate());
        Assert.Throws<ArgumentException>(() => (defaults with { InvitationText = "" }).Validate());
        Assert.Throws<ArgumentException>(() => (defaults with { CompletionText = new string('x', 501) }).Validate());
    }
}
