namespace Matrix.Core;

public sealed record InvitationSettings(
    bool Enabled = true,
    int DelaySeconds = 20,
    string InvitationText = "Mach auch du ein Foto und vervollständige unser Logo!",
    string CompletionText = "Gemeinsam geschafft – danke fürs Mitmachen!")
{
    public void Validate()
    {
        if (DelaySeconds is < 5 or > 3600) throw new ArgumentException("Die Wartezeit muss zwischen 5 und 3600 Sekunden liegen.");
        if (string.IsNullOrWhiteSpace(InvitationText) || InvitationText.Length > 500 ||
            string.IsNullOrWhiteSpace(CompletionText) || CompletionText.Length > 500)
            throw new ArgumentException("Bitte Einladungs- und Abschlusstext eingeben (jeweils höchstens 500 Zeichen).");
    }

    public string? Message(DateTimeOffset lastPhoto, DateTimeOffset now, int remaining, int total)
    {
        Validate();
        if (!Enabled || now - lastPhoto < TimeSpan.FromSeconds(DelaySeconds)) return null;
        return (remaining == 0 ? CompletionText : InvitationText)
            .Replace("{rest}", remaining.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{gesamt}", total.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}
