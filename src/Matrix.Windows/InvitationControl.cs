using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Matrix.Core;

namespace Matrix.Windows;

public sealed partial class OperatorWindow
{
    private InvitationSettings invitationSettings = new();
    private DateTimeOffset lastPhotoAt = DateTimeOffset.UtcNow;
    private readonly DispatcherTimer invitationClock = new() { Interval = TimeSpan.FromSeconds(1) };

    private string? CurrentInvitation()
    {
        if (grid is null || history is null || completing) return null;
        return invitationSettings.Message(lastPhotoAt, DateTimeOffset.UtcNow,
            GridPlanning.Remaining(grid, history.Items.Count), grid.Columns * grid.Rows);
    }

    private void UpdateInvitation() => presentation?.SetInvitation(CurrentInvitation(),
        grid is null || history is null ? 0 : GridPlanning.Remaining(grid, history.Items.Count));

    private void PhotoArrived()
    {
        lastPhotoAt = DateTimeOffset.UtcNow; UpdateInvitation();
    }

    private void ConfigureInvitation()
    {
        if (projectDirectory is null) { status.Text = "Bitte zuerst ein Projekt öffnen."; return; }
        if (busy || printBusy) { status.Text = "Bitte den laufenden Vorgang abwarten."; return; }
        var dialog = new InvitationSettingsWindow(invitationSettings) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var previous = invitationSettings;
        try { invitationSettings = dialog.Settings; SaveProject(); UpdateInvitation(); status.Text = "Einblendung für dieses Projekt gespeichert. Die Browser-Präsentation übernimmt die Änderung automatisch."; }
        catch (Exception ex) { invitationSettings = previous; status.Text = "Einstellungen konnten nicht gespeichert werden: " + ex.Message; }
    }
}

internal sealed class InvitationSettingsWindow : Window
{
    public InvitationSettings Settings { get; private set; }

    public InvitationSettingsWindow(InvitationSettings settings)
    {
        Settings = settings; Title = "Einladung und Abschlusstext"; Width = 600; Height = 570; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var enabled = new CheckBox { Content = "Einblendung bei ausbleibenden neuen Fotos aktivieren", IsChecked = settings.Enabled, Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(enabled);
        panel.Children.Add(new TextBlock { Text = "Wartezeit seit dem letzten neuen Foto in Sekunden (5–3600):" });
        var delay = new TextBox { Text = settings.DelaySeconds.ToString(), Width = 100, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(delay);
        panel.Children.Add(new TextBlock { Text = "Einladungstext:" });
        var invite = new TextBox { Text = settings.InvitationText, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, MaxLength = 500, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(invite);
        panel.Children.Add(new TextBlock { Text = "Text bei vollständigem Mosaik:" });
        var complete = new TextBox { Text = settings.CompletionText, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, MaxLength = 500, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(complete);
        panel.Children.Add(new TextBlock { Text = "Platzhalter: {rest} = fehlende Bilder, {gesamt} = Gesamtzahl. Ein neues Foto blendet den Text sofort wieder aus.", TextWrapping = TextWrapping.Wrap });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(error);
        panel.Children.Add(PresentationWindow.ActionButton("Für dieses Projekt speichern", () =>
        {
            try
            {
                if (!int.TryParse(delay.Text, out int seconds)) throw new ArgumentException("Bitte eine ganze Sekundenzahl eingeben.");
                var next = new InvitationSettings(enabled.IsChecked == true, seconds, invite.Text, complete.Text); next.Validate();
                Settings = next; DialogResult = true;
            }
            catch (ArgumentException ex) { error.Text = ex.Message; }
        }));
    }
}
