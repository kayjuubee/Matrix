using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;
using Svg;
using Svg.Model;
using Svg.Skia;

namespace Matrix.Core;

/// <summary>Keeps SVG commands as vectors until the final tile/preview canvas is rendered.</summary>
internal sealed class LogoArtwork : IDisposable
{
    private readonly SKBitmap? bitmap;
    private readonly SKSvg? svg;
    public SKRect Bounds { get; }
    public bool IsVector => svg is not null;

    private LogoArtwork(SKBitmap bitmap)
    { this.bitmap = bitmap; Bounds = new SKRect(0, 0, bitmap.Width, bitmap.Height); }

    private LogoArtwork(SKSvg svg)
    {
        this.svg = svg;
        Bounds = svg.Picture?.CullRect ?? SKRect.Empty;
        if (!float.IsFinite(Bounds.Width) || !float.IsFinite(Bounds.Height) || Bounds.Width <= 0 || Bounds.Height <= 0)
            throw new ArgumentException("Die SVG benötigt eine gültige Breite/Höhe oder viewBox.");
    }

    public static LogoArtwork Open(string path)
    {
        // Detect by content: saved project originals have the extensionless name 'master'.
        using (var probe = File.OpenRead(path))
        using (var codec = SKCodec.Create(probe))
            if (codec is not null) return new LogoArtwork(Mosaic.LoadOriented(path));
        using var stream = File.OpenRead(path);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024
        };
        XDocument document;
        using (var reader = XmlReader.Create(stream, settings)) document = XDocument.Load(reader);
        if (document.Root?.Name.LocalName != "svg" || document.Root.Name.NamespaceName != "http://www.w3.org/2000/svg")
            throw new ArgumentException("Bitte ein SVG-Logo oder ein unterstütztes Rasterbild auswählen.");
        if (document.Nodes().OfType<XProcessingInstruction>().Any(p => p.Target == "xml-stylesheet"))
            throw new ArgumentException("Externe SVG-Stylesheets bitte in das Logo einbetten.");
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.LocalName == "href" && !InternalResource(attribute.Value))
                    throw new ArgumentException("Das SVG verweist auf externe Dateien. Bilder bitte einbetten und Schriften in Pfade umwandeln.");
                ValidateCss(attribute.Value);
            }
            if (element.Name.LocalName == "style") ValidateCss(element.Value);
        }
        var svg = new SKSvg();
        try
        {
            svg.Settings.EnableJavaScript = false;
            svg.Settings.EnableExternalJavaScript = false;
            using var normalized = new MemoryStream(); document.Save(normalized); normalized.Position = 0;
            svg.Load(normalized, new SvgParameters(null, null, null, new SvgDocumentLoadOptions
            {
                ProcessingMode = SvgProcessingMode.SecureStatic,
                ExternalResources = SvgExternalResourcePolicy.SameDocumentAndDataOnly
            }));
            return new LogoArtwork(svg);
        }
        catch { svg.Dispose(); throw; }
    }

    private static bool InternalResource(string value)
    {
        value = value.Trim();
        return value.Length == 0 || value.StartsWith('#') || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCss(string css)
    {
        if (css.Contains("@import", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Externe SVG-Stylesheets bitte vor dem Import in die SVG einbetten.");
        foreach (Match match in Regex.Matches(css, @"url\s*\(\s*([^)]*)\)", RegexOptions.IgnoreCase))
            if (!InternalResource(match.Groups[1].Value.Trim().Trim('\'', '"')))
                throw new ArgumentException("Externe SVG-Ressourcen bitte einbetten; das Logo muss ohne zusätzliche Dateien auskommen.");
    }

    public SKRect Crop(float aspect)
    {
        float w = Bounds.Width, h = Bounds.Height;
        if (w / h > aspect) w = h * aspect; else h = w / aspect;
        return new SKRect(Bounds.MidX - w / 2, Bounds.MidY - h / 2, Bounds.MidX + w / 2, Bounds.MidY + h / 2);
    }

    public void Draw(SKCanvas canvas, SKRect source, SKRect destination, float opacity = 1)
    {
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(opacity * 255)) };
        if (bitmap is not null)
        {
            var bounds = new SKRectI(Math.Max(0, (int)Math.Floor(source.Left)), Math.Max(0, (int)Math.Floor(source.Top)),
                Math.Min(bitmap.Width, (int)Math.Ceiling(source.Right)), Math.Min(bitmap.Height, (int)Math.Ceiling(source.Bottom)));
            using var fragment = new SKBitmap();
            if (!bitmap.ExtractSubset(fragment, bounds)) throw new ArgumentException("Motivausschnitt konnte nicht erzeugt werden.");
            canvas.DrawBitmap(fragment, new SKRect(source.Left - bounds.Left, source.Top - bounds.Top, source.Right - bounds.Left, source.Bottom - bounds.Top),
                destination, new SKSamplingOptions(SKFilterMode.Linear), paint);
            return;
        }
        int saved = canvas.Save();
        try
        {
            canvas.ClipRect(destination);
            canvas.SaveLayer(destination, paint);
            canvas.Translate(destination.Left, destination.Top);
            canvas.Scale(destination.Width / source.Width, destination.Height / source.Height);
            canvas.Translate(-source.Left, -source.Top);
            canvas.DrawPicture(svg!.Picture!);
        }
        finally { canvas.RestoreToCount(saved); }
    }

    public void Dispose() { bitmap?.Dispose(); svg?.Dispose(); }
}
