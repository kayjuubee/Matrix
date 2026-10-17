using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Matrix.Core;

namespace Matrix.Windows;

internal static class PhotoPrinter
{
    public static (string PrinterName, byte[] Ticket)? Choose(Window owner, PhotoFormat format)
    {
        var dialog = new PrintDialog();
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmerica4x6);
        dialog.PrintTicket.PageOrientation = format == PhotoFormat.Portrait10x15 ? PageOrientation.Portrait : PageOrientation.Landscape;
        dialog.PrintTicket.CopyCount = 1;
        if (dialog.ShowDialog() != true) return null;
        using var ticket = dialog.PrintTicket.GetXmlStream();
        using var bytes = new MemoryStream(); ticket.CopyTo(bytes);
        return (dialog.PrintQueue.FullName, bytes.ToArray());
    }

    public static void Submit(string printerName, string ticketPath, string imagePath, string coordinate)
    {
        // Use the same installed Windows queue as the photobooth; do not touch its defaults.
        using var server = new LocalPrintServer();
        using var queue = server.GetPrintQueue(printerName);
        using var ticketFile = File.OpenRead(ticketPath);
        var ticket = new PrintTicket(ticketFile);
        var validated = queue.MergeAndValidatePrintTicket(queue.DefaultPrintTicket, ticket).ValidatedPrintTicket;
        validated.CopyCount = 1; // Exactly one additional mosaic copy per job.
        validated.PageScalingFactor = 100;
        var media = validated.PageMediaSize;
        bool correctMedia = media?.PageMediaSizeName == PageMediaSizeName.NorthAmerica4x6;
        if (media?.Width is double mw && media.Height is double mh)
            correctMedia = Math.Min(mw, mh) is >= 350 and <= 410 && Math.Max(mw, mh) is >= 545 and <= 600;
        if (!correctMedia)
            throw new IOException("Bitte im Druckertreiber 10 × 15 cm / 4 × 6 Zoll auswählen und den Zusatzdruck neu einrichten.");
        var dialog = new PrintDialog { PrintQueue = queue, PrintTicket = validated };
        double width = dialog.PrintableAreaWidth, height = dialog.PrintableAreaHeight;
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
            throw new IOException("Der Druckertreiber meldet keinen gültigen Druckbereich.");
        var source = PresentationWindow.LoadSource(imagePath);
        if ((source.PixelWidth > source.PixelHeight) != (width > height))
            throw new IOException("Die Papierausrichtung passt nicht zum Projekt. Bitte Quer-/Hochformat im Druckdialog korrigieren.");
        double scale = Math.Min(width / source.PixelWidth, height / source.PixelHeight);
        double w = source.PixelWidth * scale, h = source.PixelHeight * scale;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            context.DrawImage(source, new Rect((width - w) / 2, (height - h) / 2, w, h));
        }
        dialog.PrintVisual(visual, $"Matrix Zusatzdruck {coordinate}");
    }
}
