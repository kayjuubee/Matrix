# Geräteübersicht

Matrix läuft auf dem Windows-PC mit der Fotoboxsoftware und dem DNP RX1. Auf den
anderen Geräten genügt ein aktueller Browser. Für privaten Internetzugriff ist
zusätzlich Tailscale nötig; für Gäste mit öffentlichem Vorschau-Link nicht.

| Gerät / Rolle | Möglichkeiten | Einrichtung |
| --- | --- | --- |
| Fotobox-PC, Windows-Steuerung | Logo/Raster (auch SVG), Projekte, Fotoordner, Drucker und Einladungstexte/Wartezeit einrichten; Fotos importieren und Zusatzdrucke verarbeiten; Restfelder mit/ohne Fotos abschließen; Browserzugänge und Gäste-QR erstellen | Matrix starten, Fotobox und RX1 einrichten |
| Fotobox-PC, Browser | Laufenden Betrieb steuern oder Browser-Präsentation anzeigen | Browser-Steuerung starten, lokale Seite öffnen, mit Zugangscode verbinden |
| Zweiter PC / Notebook, Browser-Steuerung | Mosaik/Status ansehen, Fotos auswählen, Diashow steuern, Import und Zusatzdruck pausieren/fortsetzen, konkrete Fotos nachdrucken | Im selben WLAN/LAN die angezeigte Adresse öffnen; von außerhalb über private Tailscale-Verbindung; Zugangscode nötig |
| Handy / Tablet als Bediengerät | Dieselben Betriebsfunktionen wie die Browser-Steuerung am zweiten PC | Browser und Zugangscode; im WLAN direkt, unterwegs über Tailscale |
| Zweiter Bildschirm / anderer Rechner als Präsentation | Gesamtmotiv oder wachsendes Fotomosaik, aktuelle Fotokachel, hervorgehobenes Feld, Koordinate, Restzahl; Vollbild | Adresse der Browser-Steuerung mit `/presentation` am Ende öffnen, verbinden und Vollbild wählen; keine Matrix-Installation auf dem anderen Rechner nötig |
| Gästegerät per QR-Code | Wachsendes Mosaik, Restzahl und einzelne Kacheln ansehen; keine Steuerung und kein Druckzugriff | QR-Code scannen; im selben Netz per WLAN-Link, von überall per öffentlichem Funnel-Link |

## Was bleibt am Fotobox-PC?

Projektanlage, Logo- und Rasterplanung, Einladungstexte/Wartezeit, Auswahl lokaler Dateien/Ordner, Einrichtung
des Druckertreibers, Veranstaltungsabschluss und Erstellung der QR-Freigabe werden
in der Windows-Steuerung erledigt. Der Browser steuert danach den laufenden Betrieb.
Fotos werden vom Fotobox-PC gelesen und verarbeitet; der Browser druckt nicht auf
einem eigenen Drucker des Handys oder zweiten PCs.

## Browser als zweiter Bildschirm

1. In Matrix **Browser-Steuerung starten** wählen.
2. **Zweiten Bildschirm im Browser öffnen** verwenden oder auf dem anderen Gerät
   dieselbe Netzwerkadresse mit `/presentation` am Ende aufrufen.
3. Den Zugangscode eingeben, falls der Browser noch nicht angemeldet ist.
4. Browserfenster auf den gewünschten Bildschirm ziehen und **Vollbild** oder F11
   wählen. Die Anzeige folgt automatisch der gemeinsamen Auswahl und Diashow.
5. Vom Handy, Steuerungsbrowser oder Windows-Präsentationsfenster steuern.

Die Einladung erscheint nach einstellbarer Zeit ohne neues Foto über dem Mosaik.
Eigene Texte und Wartezeit werden am Fotobox-PC pro Projekt gespeichert und auch
im Browser angezeigt. Ein neues Foto blendet die Einladung wieder aus.

Die Browseranzeige aktualisiert den gemeinsamen Zustand ungefähr jede Sekunde;
mehrere Geräte können deshalb leicht zeitversetzt reagieren. Die Acht-Sekunden-
Diashow läuft zentral auf dem Fotobox-PC und benötigt kein geöffnetes natives
Windows-Präsentationsfenster. Neue Fotos werden sofort zentral ausgewählt und
erscheinen nach der nächsten Aktualisierung in allen verbundenen Browseranzeigen.

Die öffentliche Gästevorschau ist eine separate Seite. Ihren QR-Code teilen;
die interne Steuerungsadresse, den Zugangscode und `/presentation` nicht als
öffentlichen Gästezugang verwenden.

## Was muss laufen?

Der Fotobox-PC, Matrix und der RX1 müssen verfügbar bleiben. Wenn ein Handy oder
Anzeigerechner die Verbindung verliert, laufen lokale Verarbeitung und Druck weiter.
Die Browseranzeige zeigt dann den letzten geladenen Stand mit Verbindungswarnung.
Ein ausgeschalteter Fotobox-PC kann keine Live-Vorschau oder Fernsteuerung liefern.

Die genaue WLAN-, Tailscale- und Funnel-Einrichtung steht in [README](../README.md).
