# Matrix

Fotomosaik für Veranstaltungen: Fotos werden mit Ausschnitten eines großen Motivs
überlagert. Ein separates Präsentationsfenster zeigt, wo jedes Foto an der Wand
hingehört. Die ausgegebenen PNGs enthalten **keine Koordinaten und kein Raster**.

## Windows-Anwendung

Voraussetzung: .NET SDK 10.0 unter Windows (siehe global.json).

```powershell
dotnet run --project src/Matrix.Windows
```

1. Spalten und Zeilen einstellen und „Neues Projekt mit Motiv anlegen“ wählen.
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
Eingangs- und Projektordner müssen getrennt sein. Es gibt noch keine Druckanbindung;
„importiert“ bedeutet weder „gedruckt“ noch „aufgeklebt“.

## Bildverarbeitung und CLI

Fotos werden anhand ihrer EXIF-Orientierung ausgerichtet, mittig quadratisch
zugeschnitten und mit dem passenden Motivausschnitt bei 40 % Deckkraft überlagert.
Das Motiv wird mittig auf das Seitenverhältnis des gesamten Rasters zugeschnitten;
die Präsentationsansicht verwendet denselben Ausschnitt. Ausgabe: 600 × 600 Pixel.
SkiaSharp wird unter MIT-Lizenz verwendet.

```sh
dotnet restore Matrix.sln
dotnet build Matrix.sln --no-restore --configuration Release
dotnet test Matrix.sln --no-build --configuration Release
dotnet run --project src/Matrix.Cli -- foto.jpg logo.png kachel.png 20 15 2 7
```

Das CLI-Beispiel erzeugt Feld B07 in einem Raster mit 20 Spalten und 15 Zeilen.
Positionen beginnen bei 1; die Ausgabedatei darf noch nicht existieren.
Die Zuordnung steht in der Konsolenausgabe, nicht im PNG. CLI-Ausgaben werden
nicht automatisch in ein Windows-Projekt importiert.

## Prüfung

GitHub Actions baut die Projekte und führt die Tests unter Linux und Windows aus.
Unter Linux lässt sich die WPF-Anwendung mit `EnableWindowsTargeting` kompilieren,
aber nicht ausführen. Automatisierte Tests prüfen Motivzuschnitt, Überlagerung,
koordinatenfreie Ausgabe sowie Historie, Diashow und Vorrang neuer Bilder.

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

## Noch offen

Windows-Druck mit Druckformat, dauerhafter Druckwarteschlange, Pause und Nachdruck;
zufällige oder farboptimierte Feldvergabe; Rasterplan für die physische Wand.
Die Windows-Oberfläche muss zusätzlich auf einem echten Windows-System und der
Druckablauf später mit dem vorgesehenen Drucker geprüft werden.
