# Matrix

Fotomosaik für Veranstaltungen: Fotos werden mit Ausschnitten eines großen Motivs
überlagert. Ein separates Präsentationsfenster zeigt, wo jedes Foto an der Wand
hingehört. Die ausgegebenen PNGs enthalten **keine Koordinaten und kein Raster**.

Eine Übersicht der Funktionen pro Gerät steht in [Geräteübersicht](docs/GERAETE.md).

## Windows-Anwendung

Voraussetzung: .NET SDK 10.0 unter Windows (siehe global.json).

```powershell
dotnet run --project src/Matrix.Windows
```

1. „Neues Projekt: zuerst Logo auswählen“ wählen und das große Motiv öffnen.
2. Im Planungsfenster die ungefähre Bildanzahl (Standard: 300) und Papierausrichtung
   einstellen. Das Programm schlägt Spalten und Zeilen anhand des ausgerichteten
   Logo-Seitenverhältnisses und der rechteckigen Kacheln vor. Der Vorschlag wägt
   möglichst wenig Motivbeschnitt gegen die gewünschte Bildanzahl ab. Spalten und
   Zeilen können manuell geändert werden. Vorschau, tatsächliche Bildanzahl und
   nominelle Wandmaße prüfen, dann „Projekt mit diesem Raster erstellen“ wählen.
   Das Präsentationsfenster öffnet sich separat.
3. Das Fenster auf den zweiten Bildschirm ziehen, mit F11 maximieren.
4. „Fotos hinzufügen“ oder „Eingangsordner überwachen“ wählen.

Im Präsentationsfenster stehen das große Motiv mit Raster und orange markiertem
Zielfeld, die fertige Fotokachel und die Koordinate nebeneinander. Kleine grüne
Markierungen zeigen bereits vergebene Felder; sie bestätigen keine physische
Platzierung. Spalte A beginnt links, Zeile 01 oben. Das Fenster zeigt die fertige
Fotokachel mit Motivüberlagerung, wie sie später ausgedruckt werden kann.

- **Zurück / Weiter** oder Pfeiltasten: durch bisherige Bilder blättern; pausiert
  die Diashow. An den Enden bleibt die manuelle Navigation stehen.
- **Diashow** oder Leertaste: alle bisherigen Bilder im Abstand von acht Sekunden
  durchlaufen, mit Wiederholung nach dem letzten Bild.
- **Neuestes Bild** oder Ende: zum letzten Bild springen und pausieren.
- **F11 / Esc**: Vollbild umschalten / verlassen. Vollbild gilt für den Bildschirm,
  auf den das Fenster vorher gezogen wurde.
- **Neues Bild**: springt immer sofort auf dieses Bild, auch beim Zurückblättern
  oder in Pause. Eine laufende Diashow läuft nach acht Sekunden weiter.
- **Restanzeige**: zeigt groß, wie viele Fotos noch bis zur vollständigen Matrix
  fehlen. Bei null erscheint „Mosaik vollständig!“. Gezählt werden erfolgreich
  importierte, unterschiedliche Bilder, keine Druckaufträge. Zurückblättern,
  doppelte Dateien und Nachdrucke verändern den Restzähler nicht.

Mit **„Fotomosaik anzeigen“** wird statt des reinen Motivs die wachsende Gesamtwand
angezeigt: belegte Felder zeigen genau die erzeugten Kacheln mit Motivüberlagerung,
freie Felder weiterhin das große Motiv. Raster und aktuelle Markierung bleiben
sichtbar. „Nur Motiv anzeigen“ schaltet zurück. Die Vorschau zählt importierte
Kacheln, nicht bestätigte Ausdrucke oder physisch aufgeklebte Fotos.

Die Gesamtansicht lädt kleine Vorschaubilder im Hintergrund und hält sie im
Speicher. Beim ersten Öffnen erscheint der Ladefortschritt; erneutes Umschalten
verwendet die bereits geladenen Bilder. Neue Importe ergänzen nur ihre eigene
Kachel. Die Druckbilder werden dabei nicht neu berechnet. Die Vorschau im Präsentationsfenster bleibt lokal; die separat aktivierbare
Browser-Steuerung kann dieselben Fotokacheln an angemeldete Geräte im lokalen Netz
oder über Tailscale ausliefern. Es gibt keinen automatischen Upload zu einer Cloud.

Das Schließen des Präsentationsfensters beendet den Import nicht; es kann wieder
geöffnet werden. Das Schließen des Steuerungsfensters beendet die Anwendung.

## Steuerung am PC, im WLAN und über das Internet

Die Windows-Steuerung auf dem Fotobox-Rechner bleibt vollständig verfügbar.
Zusätzlich ist eine für Handy und Tablet geeignete Browser-Steuerung eingebaut.
Projekt, Logo/Raster, Eingangsordner und RX1-Druckeinstellungen werden zunächst
am Fotobox-Rechner eingerichtet. Danach lassen sich aus dem Browser bedienen:

- Gesamtmosaik, aktuelles Foto, Koordinate, Restzahl und Druckstatus ansehen;
- zurück/weiter, neuestes Bild und Diashow steuern;
- belegte Rasterfelder antippen, um ein bestimmtes Foto auszuwählen;
- Mosaikansicht auf dem Präsentationsbildschirm umschalten;
- Ordnerüberwachung und Zusatzdruck pausieren/fortsetzen;
- für ein konkret ausgewähltes Foto einen bestätigten Zusatz-/Nachdruck anfordern.

### Zweiter Bildschirm als Browseranzeige

Über **„Zweiten Bildschirm im Browser öffnen“** oder die Adresse der Steuerung mit
`/presentation` am Ende wird eine eigene Vollbildanzeige geöffnet. Sie zeigt
Raster/Mosaik, markiertes Feld, aktuelle Fotokachel, Koordinate und Restzähler.
Einmal mit dem Bedien-Zugangscode anmelden; über „Vollbild“ oder F11 auf dem
gewünschten Bildschirm anzeigen. Sie funktioniert auch auf einem anderen Rechner
im WLAN oder über die private Tailscale-Verbindung, ohne dort Matrix zu installieren.

Auswahl, Motiv-/Mosaikmodus und Acht-Sekunden-Diashow teilen sich Windows-Fenster
und Browser. Der Diashow-Takt läuft jetzt zentral im Fotobox-PC; das native
Präsentationsfenster darf geschlossen werden. Die Browseranzeige liest den Zustand
ungefähr jede Sekunde und kann entsprechend leicht verzögert sein. Bei einem
neuen Foto wird dessen Koordinate sofort angezeigt; bis die passende Fotodatei
geladen ist, erscheint „Bild lädt“ statt des vorherigen Fotos unter einer falschen
Koordinate. Bei Verbindungsverlust bleibt der letzte Stand mit Warnung sichtbar.

Die Anzeige enthält keine Druckknöpfe. Steuern lässt sie sich über Windows oder
die separate Browser-Steuerung. Sie nutzt deren privaten Bedienzugang und ist
nicht die öffentliche Gäste-QR-Vorschau auf Port 8788.

### Auf dem Fotobox-Rechner und im WLAN

1. Im Windows-Programm **„Browser-Steuerung starten“** wählen. Der Zugang ist
   standardmäßig ausgeschaltet und wird beim nächsten Programmstart nicht
   automatisch gestartet. Ein zufälliger Zugangscode und die Rechneradressen
   erscheinen im Steuerungsfenster; der Text kann markiert und kopiert werden.
2. Am selben Rechner „Steuerung hier im Browser öffnen“ verwenden. Alternativ
   `http://127.0.0.1:8787` öffnen und den angezeigten Code eingeben.
3. Handy oder zweiten Rechner mit demselben lokalen Netz verbinden. Eine der
   angezeigten WLAN-/LAN-Adressen, beispielsweise `http://192.168.1.50:8787`, im
   Browser öffnen und den Zugangscode eingeben. Eine App auf dem Handy ist im
   WLAN nicht nötig. Angezeigte VPN-/virtuelle Adapteradressen sind nicht in jedem
   WLAN erreichbar; die zum gemeinsamen Netz passende Adresse verwenden.
4. Falls Windows den Netzwerkzugriff abfragt, ihn für das **private** Netz erlauben.
   Bei einer bestehenden Firewall muss eingehendes TCP 8787 im privaten lokalen
   Netz erlaubt sein. Gäste-WLAN mit Client-Isolation funktioniert nicht.

### Über das Internet mit Tailscale

Die Anwendung richtet keine öffentliche Router-Portfreigabe ein. Für den privaten
Fernzugang Tailscale auf dem Fotobox-PC und dem Handy/zweiten Rechner installieren
und beide Geräte im selben eigenen Tailscale-Netz anmelden. Das kann nur auf den
jeweiligen Geräten erfolgen; die Cloud-Entwicklung richtet deinen Account oder
PC dabei nicht automatisch ein.

Empfohlener Zugang mit HTTPS über **Tailscale Serve**:

1. Matrix auf dem Fotobox-PC starten und die Browser-Steuerung einschalten.
2. Tailscale auf PC und Handy verbinden. Auf dem PC in PowerShell ausführen:

   ```powershell
   tailscale serve --bg http://127.0.0.1:8787
   ```

3. Falls Tailscale zur Aktivierung von HTTPS/Serve auffordert, den angezeigten
   Einrichtungsschritten folgen. Die ausgegebene HTTPS-Adresse auf dem Handy
   öffnen und den Matrix-Zugangscode eingeben. Tailscale muss am Handy verbunden
   bleiben; die Seite ist nur für berechtigte Geräte im eigenen Tailscale-Netz.
4. Die aktuelle Serve-Adresse lässt sich mit `tailscale serve status` prüfen;
   diese Weiterleitung mit `tailscale serve reset` wieder entfernen. Reset entfernt
   die Serve-Konfiguration dieses Rechners, also bei weiteren eigenen Serve-Diensten
   stattdessen gezielt deren bestehende Konfiguration berücksichtigen.

Alternativ funktioniert die im Programm angezeigte `http://100.x.x.x:8787`-Adresse
über die private Tailscale-Verbindung, wenn die Windows-Firewall diesen Zugriff
zulässt. Mit Serve bleibt der Zugriff auf Matrix am PC über die Loopback-Adresse.
Tailscale Funnel beziehungsweise eine öffentliche Freigabe werden nicht benötigt.
Offizielle Einrichtung: https://tailscale.com/download und
https://tailscale.com/kb/1242/tailscale-serve.

### Verhalten und Zugriffsschutz

Lokale Oberfläche und Browser teilen sich Projekt, aktuelle Fotoauswahl und
Präsentationszustand. Neu eintreffende Bilder werden weiterhin sofort ausgewählt.
Nachdrucke beziehen sich auf die konkrete, im Browser bestätigte Bildkennung;
neu eingetroffene Fotos ändern diesen Auftrag nicht. Eine veraltete Browseransicht
kann keinen Auftrag in einem inzwischen gewechselten Projekt auslösen. Wiederholte
identische Browseraufträge werden im laufenden Server abgefangen, damit eine
verlorene Antwort keinen doppelten Zusatzdruck verursacht.

Ohne Zugangscode sind Foto-, Status- und Steuerungs-APIs gesperrt; Fehlversuche beim
Login sind begrenzt. Nach dem Stoppen und erneuten Starten der Browser-Steuerung
gibt es einen neuen Code; vorherige Browsersitzungen müssen sich neu anmelden.
„Abmelden“ beendet nur die Sitzung in diesem Browser. Der Code gehört den Bedienern;
die HTTP-Adresse im WLAN für ein vertrauenswürdiges Netz nutzen, HTTPS über
Tailscale Serve für den privaten Internetzugang.

Bei verlorener WLAN-/Internetverbindung laufen lokale Ordnerüberwachung,
Präsentation und Druck weiter. Der Browser zeigt die unterbrochene Verbindung und
versucht alle zwei Sekunden erneut, den aktuellen Stand zu laden. Auch „Browser-
Steuerung stoppen“ unterbricht keine lokalen Arbeitsabläufe. Erst das Schließen
von Matrix beendet die Windows-Anwendung und ihren Webserver.

## Gästevorschau per QR-Code teilen

Die Gästevorschau ist eine zusätzliche, eigenständige Freigabe auf **Port 8788**.
Sie zeigt das wachsende Mosaik, Restzahl und einzelne Fotokacheln. Sie enthält
keine Steuerungs-, Import- oder Druckfunktionen und keine Drucker-/Fehlerdaten.
Der Zugangscode der Bedienoberfläche gehört nicht in den Gäste-QR-Code.

1. Projekt öffnen und **„Vorschau teilen / QR-Code …“** wählen.
2. Für das WLAN die Adresse des Fotobox-PCs mit Port **8788** auswählen, zum
   Beispiel `http://192.168.1.50:8788`. Das Gerät muss den PC im Netz erreichen
   können; bei einer Firewall TCP 8788 für das private lokale Netz erlauben.
3. „Link und QR-Code erstellen“ anklicken. Der vollständige Link enthält die
   Freigabekennung. Jeder mit diesem Link kann die Bilder dieses Projekts ansehen,
   ohne App und ohne Eingabe des Steuerungscodes.
4. Link kopieren oder **„QR-Code als PNG speichern“** verwenden, beispielsweise
   zum Anzeigen auf einem Bildschirm oder zum Ausdrucken für Gäste.
5. „Vorschau-Freigabe beenden“ sperrt den bisherigen Link. Auch bei einem
   Projektwechsel wird die Freigabe beendet; ein Link darf niemals unbemerkt
   die Fotos der nächsten Veranstaltung zeigen. Danach einen neuen QR-Code erzeugen.

Das Schließen des QR-Fensters beendet die laufende Freigabe nicht. Sie läuft bis
zum bewussten Beenden, Projektwechsel oder Programmende. Nach einem Neustart ist
ein neuer QR-Code erforderlich. Das Stoppen der Fernsteuerung auf 8787 betrifft
die separate Gästevorschau nicht und umgekehrt. Die Vorschau aktualisiert sich
alle drei Sekunden; bei Verbindungsproblemen zeigt sie den letzten geladenen
Stand. Vor der ersten Erreichbarkeit von einem echten Gastgerät ist die Anzeige
im QR-Fenster noch kein Nachweis für einen funktionierenden Netzwerkzugang.

### Öffentlich über das Internet, ohne Tailscale-App bei Gästen

Eine private Tailscale-Serve-Adresse ist nicht für beliebige Gäste erreichbar.
Dafür lässt sich **Tailscale Funnel** auf dem Fotobox-PC einrichten. Dabei werden
nur Vorschau-Port **8788**, nicht Steuerungs-Port **8787**, veröffentlicht.
Die Veröffentlichung erfolgt auf deinem PC, nicht automatisch aus dieser
Entwicklungsumgebung. Matrix und Internetverbindung müssen weiterlaufen.

Mit Tailscale auf dem PC angemeldet und Gästevorschau gestartet:

```powershell
tailscale funnel --bg --https=8443 http://127.0.0.1:8788
```

Den Einrichtungsdialog von Tailscale einschließlich erforderlicher Funnel-/HTTPS-
Freigabe im eigenen Konto abschließen. Die tatsächlich ausgegebene öffentliche
HTTPS-Basisadresse inklusive **`:8443`** in das QR-Fenster übernehmen, anschließend
„Link und QR-Code erstellen“ wählen. Gäste benötigen dann weder Tailscale noch
eine Mitgliedschaft im WLAN. Mit dem eigenen Handy über Mobilfunk ausprobieren.

Der separate HTTPS-Port 8443 lässt eine vorhandene private Steuerung über
Tailscale Serve auf 443 bestehen. Falls auf 8443 schon ein eigener Dienst läuft,
dessen Konfiguration nicht überschreiben, sondern einen freien unterstützten
Port wählen. `tailscale funnel status` zeigt die bestehende Freigabe. Die gezielte
Funnel-Weiterleitung kann mit `tailscale funnel --https=8443 off` beendet werden;
das ist unabhängig vom Sperren des Matrix-Freigabelinks.
Offizielle Anleitung: https://tailscale.com/kb/1223/funnel.

Der Link ist die Berechtigung zum Ansehen. Er wird als URL-Fragment im QR-Code
übertragen und nicht als Anfragepfad oder Zugangscode an die Steuerung geschickt.
Der QR-Code entsteht lokal (QRCoder, MIT-Lizenz), ohne Bilder oder Links an einen
externen QR-Dienst zu senden. Die Internetverbindung läuft bei Funnel über dessen
Infrastruktur. Bereits von Gästen angesehene/gespeicherte Bilder lassen sich durch
Sperren des Links nicht von deren Geräten zurückholen.

## Import und Speicherung

Projekte werden unter `%LOCALAPPDATA%\Matrix\Projects\<Kennung>` abgelegt. Der
vollständige Projektpfad erscheint im Steuerungsfenster. „Gespeichertes Projekt
öffnen“ lädt dessen `project.json` mit Bildhistorie und Feldbelegung; die Diashow
und Ordnerüberwachung starten nach dem Öffnen pausiert beziehungsweise gestoppt.
Das Motiv wird ins Projekt kopiert, die Originalfotos werden nicht verändert.
Kacheln werden als PNG und die Zuordnung atomar als JSON gespeichert. Den ganzen
Projektordner zusammen aufbewahren; einzelne Dateien nicht verschieben.

Die Ordnerprüfung läuft alle zwei Sekunden und liest auch vorhandene Bilder ein.
Dateien müssen bei zwei Prüfungen dieselbe Größe und Änderungszeit haben und zum
Kopieren exklusiv lesbar sein. Noch gesperrte oder nicht lesbare Dateien werden
bei späteren Prüfungen erneut versucht. PNG, JPEG, WebP und BMP werden unterstützt.
Identische Dateiinhalte werden per SHA-256 nur einmal vergeben. Jedes neue Bild
bekommt zufällig eines der noch freien Felder; keine Position wird doppelt vergeben.
Gespeicherte Positionen bleiben beim Wiederöffnen und beim Nachdruck erhalten.
Bei voller Matrix stoppt die Überwachung.
Eingangs- und Projektordner müssen getrennt sein. „Importiert“ bedeutet weder
„gedruckt“ noch „aufgeklebt“.

## Bildformat und DNP RX1

Neue Projekte verwenden rechteckige 10 × 15-cm-Kacheln: standardmäßig Querformat
(15 cm breit, 10 cm hoch), wahlweise Hochformat. Die Ausrichtung wird bei der Planung gewählt und pro Projekt
fest gespeichert. Auch Raster, Hervorhebung und Motivzuschnitt verwenden dieses Format.
Beispiel: 5 × 5 Kacheln im Querformat ergeben nominell eine Wand von 75 × 50 cm.

Fotos werden anhand ihrer EXIF-Orientierung ausgerichtet und **vollständig,
proportional und ohne Zuschnitt** eingepasst. Abweichende Seitenverhältnisse
bekommen weiße Ränder; es gibt keine automatische Drehung zur Papierausrichtung.
Anschließend wird der Motivausschnitt mit 40 % Deckkraft überlagert, auch über
den Rändern. Nur das große Motiv wird mittig auf das Seitenverhältnis der gesamten
Wand zugeschnitten. Die Präsentationsansicht verwendet denselben Ausschnitt.

Ausgabe: 1800 × 1200 Pixel im Querformat oder 1200 × 1800 Pixel im Hochformat.
Das entspricht dem Seitenverhältnis 3:2 für das nominelle 10 × 15-/4 × 6-Zoll-Format.
Die PNG-Pixelmaße allein legen keine physische Druckgröße fest. Beim DNP RX1 müssen
Papierformat, Ausrichtung und Skalierung im Windows-Druckertreiber zum Projekt
passen. Ein randloser Druck kann je nach Treibereinstellung die Ränder vergrößern
und beschneiden; der endgültige Ausdruck muss mit dem echten Gerät geprüft werden.
Die Windows-Anwendung kann zusätzliche Mosaikdrucke über die installierte
Windows-Warteschlange anfordern. Der echte RX1 wurde in der Cloud nicht getestet.

## Zusatzdruck neben DSLRBooth / LumaBooth

DSLRBooth/LumaBooth druckt weiterhin das normale Fotoboxfoto. Matrix überwacht den
Ausgabeordner und sendet **einen zusätzlichen Mosaikdruck** an denselben installierten
Windows-Drucker. Windows verwaltet die Druckaufträge beider Anwendungen; eine feste
Reihenfolge „Original, direkt danach Kachel“ über beide Anwendungen ist nicht
garantiert. Fotoboxdateien werden nicht überschrieben, die Kacheln bleiben getrennt.

1. Projekt anlegen und „Zusatzdruck einrichten / starten“ wählen.
2. Im Windows-Dialog die RX1-Warteschlange auswählen, 10 × 15 / 4 × 6 Zoll und
   Querformat einstellen. Der Dialog übernimmt die Einstellung; er druckt noch
   kein einzelnes Foto. Matrix übernimmt keine globalen Druckerstandardänderungen.
3. Anschließend den **fertigen Ausgabe-/Layoutordner** der Fotobox überwachen.
   Nicht gleichzeitig Rohbilder, Vorschaubilder und fertige Layouts beobachten;
   sonst würden mehrere unterschiedliche Dateien pro Aufnahme importiert.
4. Jedes danach importierte Bild bekommt einen gespeicherten Zusatzdruckauftrag.
   Bilder, die vor der ersten Druckereinrichtung importiert wurden, werden nicht
   rückwirkend automatisch gedruckt.
5. „Zusatzdruck pausieren“ stoppt weitere Übergaben an Windows. Import und
   Feldvergabe laufen weiter; neue Aufträge warten. Bereits an Windows übergebene
   Aufträge werden durch diesen Knopf nicht aus der Windows-Warteschlange entfernt.
6. Zum Fortsetzen „Zusatzdruck einrichten / starten“ wählen. Nach einem Neustart
   ist der Zusatzdruck immer pausiert. Druckerzuordnung, Ticket und Aufträge bleiben
   gespeichert, werden aber erst nach bewusster Aktivierung weiterverarbeitet.
7. Für einen Nachdruck im Präsentationsfenster das Foto wählen, dann im
   Steuerungsfenster „Ausgewähltes Bild drucken / nachdrucken“. Die Koordinate
   bleibt gleich. Bei möglichem vorherigem Druck wird eine Bestätigung angezeigt.

Die Anzeige „an Windows übergeben“ bestätigt keine physische Ausgabe. Bei einem
Fehler oder Abbruch während der Übergabe wird der Auftrag als „manuell prüfen“
geführt; er wird nicht automatisch erneut gesendet. Vor einem Nachdruck die
Windows-Warteschlange und die tatsächlichen Ausdrucke kontrollieren. Bereits
übergebene Aufträge nach Papierstau werden vom Windows-Treiber verwaltet.
Die Anwendung passt das gesamte Bild proportional in den gemeldeten Druckbereich;
Treiberformat, Ausrichtung und randlose Vergrößerung müssen am RX1 getestet werden.
Im Fehlerfall pausiert die Matrix-Druckwarteschlange; die Fotobox wird nicht gesteuert.

Alte Projekte ohne gespeichertes Format bleiben quadratisch und können weiter
angezeigt werden. Neue Importe sind dort gesperrt, damit alte und neue Formate nicht
vermischt werden. Für 10 × 15 cm ein neues Projekt mit den Originalfotos anlegen;
aus bereits beschnittenen alten PNGs lassen sich fehlende Bildteile nicht zurückholen.

## Veranstaltungsabschluss: Restfelder mit oder ohne Fotos

Am Ende eines Events **„Restfelder abschließen (mit / ohne Fotos)“** wählen.
Das Programm zeigt die genaue Zahl noch freier Felder und zusätzlicher Druckaufträge.
Danach besteht die Wahl:

- **Mit Fotos:** Original-/Fotoboxfotos auswählen. Sie werden proportional ohne
  Zuschnitt mit dem passenden Motivausschnitt überlagert. Reichen die ausgewählten
  Fotos nicht für alle Restfelder, werden sie wiederverwendet. Keine bereits mit
  dem Logo überlagerten Matrix-PNGs auswählen, da das Motiv sonst zweimal enthalten wäre.
- **Ohne Fotos:** Nur die fehlenden Ausschnitte des großen Motivs erzeugen, mit
  voller Motivdeckkraft auf weißem Hintergrund bei Transparenz.

Erst **„Kacheln erzeugen und Drucke vormerken“** bestätigt den gewählten Abschluss.
Belegte Felder und ihre Zuordnung bleiben erhalten. Die Restfelder werden in
zufälliger Reihenfolge ergänzt. Die neuen Kacheln erscheinen in Präsentation und
Gästevorschau und sind mit derselben Größe wie die übrigen Fotos druckbereit.

Import und Zusatzdruck sind während des Abschlusses pausiert. Nach dem Erzeugen
bleiben sie pausiert; mit „Zusatzdruck einrichten / starten“ am PC oder „Zusatzdruck
fortsetzen“ in der angemeldeten Browser-Steuerung die wartenden Aufträge bewusst
freigeben. Auch schon vorher wartende Druckaufträge bleiben in der Warteschlange.
Falls noch kein Drucker eingerichtet ist, zuerst den RX1 mit passendem Papierformat
wählen. Die Abschlussfunktion sendet nicht sofort unbemerkt alle Ausdrucke ab.

**„Laufenden Abschluss abbrechen“** stoppt nach der gerade bearbeiteten Kachel.
Bereits erzeugte Kacheln und Aufträge bleiben gespeichert. Bei einem Fehler wird
angehalten; erneutes Starten ergänzt nur die dann noch freien Felder. Abbrechen
im anfänglichen Auswahldialog stellt den vorherigen Import-/Druckbetrieb wieder her.
Der Restzähler zählt auch diese Abschlusskacheln als belegte Felder; er ist kein
Zähler ausschließlich neu aufgenommener Gästefotos und keine Druckbestätigung.

## Bildverarbeitung und CLI

SkiaSharp wird unter MIT-Lizenz verwendet.

```sh
dotnet restore Matrix.sln
dotnet build Matrix.sln --no-restore --configuration Release
dotnet test Matrix.sln --no-build --configuration Release
dotnet run --project src/Matrix.Cli -- foto.jpg logo.png kachel.png 20 15 2 7 quer
```

Das CLI-Beispiel erzeugt Feld B07 in einem Raster mit 20 Spalten und 15 Zeilen.
Positionen beginnen bei 1; die Ausgabedatei darf noch nicht existieren.
Das optionale letzte Argument ist `quer` (Standard) oder `hoch`.
Die Zuordnung steht in der Konsolenausgabe, nicht im PNG. CLI-Ausgaben werden
nicht automatisch in ein Windows-Projekt importiert.

## Prüfung

GitHub Actions baut die Projekte und führt die Tests unter Linux und Windows aus.
Unter Linux lässt sich die WPF-Anwendung mit `EnableWindowsTargeting` kompilieren,
aber nicht ausführen. Automatisierte Tests prüfen Motivzuschnitt, Überlagerung,
koordinatenfreie Ausgabe, vollständige Fotoecken, Einpassung ohne Verzerrung,
rechteckige Rastergeometrie und Formatpersistenz sowie Historie, Diashow,
Vorrang neuer Bilder und die sichere Wiederaufnahme von Druckaufträgen. Weitere
Tests prüfen Rastervorschläge für verschiedene Logoformate und Bildanzahlen,
Zufallsvergabe ohne doppelte Felder bis zur vollständig belegten Matrix und den
von der Diashow unabhängigen Restzähler. Echte HTTP-Tests prüfen den Webserver,
Anmeldeschutz für Status/Bilder/Befehle, Loginbegrenzung, Sitzungswiderruf und die
Vermeidung doppelter Befehlsausführung bei parallelen Wiederholungen. Die Browser-
Präsentationsseite sowie projektgebundene Bildanfragen werden ebenfalls geprüft. Zusätzliche
Tests prüfen Gästezugang, fehlende Schreib-/Druckrouten, Trennung der Zugangstoken,
Projektbindung, Linkvalidierung und QR-PNG-Erzeugung. Die Abschlussplanung und
vollfarbige Motivkacheln ohne Foto werden ebenfalls geprüft.

Manuelle Abnahme unter Windows:

1. Logo zuerst wählen, Bildanzahl 25 eingeben, Vorschlag prüfen und bei Bedarf
   manuell 5 × 5 wählen. Präsentationsfenster auf zweiten Bildschirm verschieben.
2. Drei Fotos importieren; Koordinate, orange Markierung und Fotokachel abgleichen.
3. Zurückblättern, Diashow starten/pausieren und ein weiteres Foto hinzufügen:
   In beiden Zuständen muss sofort das neue Bild erscheinen; bei laufender Diashow
   darf erst nach acht Sekunden wieder zum nächsten Bild gewechselt werden.
4. Ordner überwachen, ein neues Foto hineinkopieren und dieselbe Datei erneut
   ablegen: nur eine Feldvergabe. Alle PNGs müssen ohne Beschriftung sein.
5. Fenster schließen/öffnen und Projekt neu laden: Historie und Belegung erhalten.
6. F11 und Esc am zweiten Bildschirm prüfen.
7. Ein 3:2-Foto mit erkennbaren Details an allen vier Rändern importieren:
   die Details müssen erhalten bleiben. Ein Hochformatfoto importieren und prüfen,
   dass es vollständig mit seitlichen Rändern erscheint.
8. Neues Querformatprojekt schließen und erneut laden: rechteckiges Raster und
   1800 × 1200-Ausgaben bleiben erhalten. Altes quadratisches Projekt öffnen:
   nur Ansicht, kein Import und keine Änderung seiner bestehenden PNG-Dateien.
9. RX1 auswählen und genau ein neues Foto importieren: ein normaler Fotoboxdruck
   plus ein Matrix-Zusatzdruck. Ecken, Papierformat und Ausrichtung am Papier prüfen.
10. Zusatzdruck pausieren, ein Foto importieren und fortsetzen: genau ein wartender
    Auftrag. Projekt neu laden: zunächst pausiert, schon übergebene Aufträge nicht
    automatisch erneut drucken. Nachdruck behält dieselbe Wandkoordinate.
11. „Fotomosaik anzeigen“ wählen: Kacheln müssen an den richtigen Feldern erscheinen.
    Zu „Nur Motiv anzeigen“ zurückschalten und wieder öffnen: Vorschau aus dem Cache.
    Währenddessen ein Foto importieren: Gesamtwand ergänzt die neue Kachel automatisch,
    Foto, Koordinate und aktuelle Markierung springen auf das neue Bild.
12. Neues Bild importieren: zufälliges freies Feld, Restzähler sinkt genau um eins.
    Zurückblättern und dieselbe Datei erneut einlesen: Restzähler unverändert.
    Projekt neu laden: Feldzuordnung und Restzahl bleiben gleich; bei vollständiger
    Matrix erscheint die Fertigmeldung.
13. Browser-Steuerung aktivieren und im Handy-Browser anmelden. Auswahl und
    Diashow mit dem zweiten Bildschirm abgleichen. Nachdruck für eine konkrete
    Kachel bestätigen und zwischenzeitlich ein neues Foto importieren: nur die
    bestätigte Koordinate darf als Zusatzdruck angefordert werden.
14. Mit Tailscale auf dem Handy WLAN abschalten und über Mobilfunk verbinden.
    Vorschau/Befehle prüfen. Verbindung trennen: lokale Importe und Drucke laufen
    weiter. Browser-Steuerung stoppen/neu starten: alter Zugang nicht mehr gültig.
15. Gäste-QR-Code am Handy scannen: Mosaik und Kacheln sichtbar, keine Steuerung.
    Projektwechsel oder Freigabeende: alter Link gibt keine neuen Fotos mehr frei.
16. Für öffentliche Freigabe Funnel auf 8443 → 8788 einrichten, öffentlichen Link
    als QR-Code erzeugen und auf einem Handy ohne Tailscale über Mobilfunk testen.
    Private Steuerung auf 443 muss dabei weiterhin nur über Tailscale erreichbar sein.
17. Ein kleines Projekt teilweise belegen und Abschluss mit ausgewählten Fotos testen:
    vorhandene Felder unverändert, nur fehlende Felder ergänzt, Drucke noch pausiert.
    In einem zweiten Projekt „ohne Fotos“ wählen: richtige vollständige Motivausschnitte.
    Abbrechen/erneutes Starten erzeugt keine zweite Zuordnung für belegte Felder.

## Noch offen

Abnahme am echten DNP RX1 einschließlich gemeinsamem Betrieb mit DSLRBooth/LumaBooth;
farboptimierte Feldvergabe; Rasterplan für die physische Wand.
Die Windows-Oberfläche muss zusätzlich auf einem echten Windows-System und der
Druckablauf mit dem vorgesehenen Drucker geprüft werden.

Zusätzliche Abnahme Browser-Präsentation: `/presentation` auf einem zweiten
Rechner öffnen und anmelden. Vom Handy Bilder wechseln und Diashow starten;
Foto, Koordinate, Markierung und Restzahl müssen folgen. Natives Fenster schließen:
Browser-Diashow läuft weiter. Ein neues Bild muss für ein volles Diaintervall
zentral ausgewählt bleiben. Projektwechsel und Verbindungsunterbrechung prüfen.
