# Matrix

Fotomosaik für Veranstaltungen: Fotos werden mit Ausschnitten eines großen Motivs
überlagert. Ein separates Präsentationsfenster zeigt, wo jedes Foto an der Wand
hingehört. Die ausgegebenen PNGs enthalten **keine Koordinaten und kein Raster**.

## Windows-Anwendung

Voraussetzung: .NET SDK 10.0 unter Windows (siehe global.json).

```powershell
dotnet run --project src/Matrix.Windows
```

1. Spalten, Zeilen und Papierausrichtung einstellen (Standard: 15 × 10 cm Querformat),
   dann „Neues Projekt mit Motiv anlegen“ wählen.
2. Das große Motiv auswählen. Das Präsentationsfenster öffnet sich separat.
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

Mit **„Fotomosaik anzeigen“** wird statt des reinen Motivs die wachsende Gesamtwand
angezeigt: belegte Felder zeigen genau die erzeugten Kacheln mit Motivüberlagerung,
freie Felder weiterhin das große Motiv. Raster und aktuelle Markierung bleiben
sichtbar. „Nur Motiv anzeigen“ schaltet zurück. Die Vorschau zählt importierte
Kacheln, nicht bestätigte Ausdrucke oder physisch aufgeklebte Fotos.

Die Gesamtansicht lädt kleine Vorschaubilder im Hintergrund und hält sie im
Speicher. Beim ersten Öffnen erscheint der Ladefortschritt; erneutes Umschalten
verwendet die bereits geladenen Bilder. Neue Importe ergänzen nur ihre eigene
Kachel. Die Druckbilder werden dabei nicht neu berechnet. Es handelt sich um eine
lokale Programmvorschau, keinen Weblink und keinen Upload.

Das Schließen des Präsentationsfensters beendet den Import nicht; es kann wieder
geöffnet werden. Das Schließen des Steuerungsfensters beendet die Anwendung.

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
Identische Dateiinhalte werden per SHA-256 nur einmal vergeben. Die erste Version
vergibt freie Felder der Reihe nach; bei voller Matrix stoppt die Überwachung.
Eingangs- und Projektordner müssen getrennt sein. „Importiert“ bedeutet weder
„gedruckt“ noch „aufgeklebt“.

## Bildformat und DNP RX1

Neue Projekte verwenden rechteckige 10 × 15-cm-Kacheln: standardmäßig Querformat
(15 cm breit, 10 cm hoch), wahlweise Hochformat. Die Ausrichtung wird pro Projekt
fest gespeichert. Eine Änderung der Auswahl wirkt erst beim Anlegen eines neuen
Projekts. Auch Raster, Hervorhebung und Motivzuschnitt verwenden dieses Format.
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
Vorrang neuer Bilder und die sichere Wiederaufnahme von Druckaufträgen.

Manuelle Abnahme unter Windows:

1. 5 × 5 Projekt anlegen, Präsentationsfenster auf zweiten Bildschirm verschieben.
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

## Noch offen

Abnahme am echten DNP RX1 einschließlich gemeinsamem Betrieb mit DSLRBooth/LumaBooth;
zufällige oder farboptimierte Feldvergabe; Rasterplan für die physische Wand.
Die Windows-Oberfläche muss zusätzlich auf einem echten Windows-System und der
Druckablauf mit dem vorgesehenen Drucker geprüft werden.
