using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public enum PdfFont { Times, Helvetica }

/// <summary>
/// Escritor de PDF mínimo (A4, fuentes estándar Type1, texto Latin-1). Basado en el MinimalPdfWriter del juego base,
/// pero con fuente configurable y sin depender de sus scripts.
/// </summary>
public class FinalPdfWriter
{
    private struct Cmd
    {
        public bool isText;
        public float x, y, w, h, size, lineWidth;
        public string text, color;
        public bool bold;
    }

    private readonly List<List<Cmd>> pages = new List<List<Cmd>>();
    private List<Cmd> page;
    private readonly PdfFont font;

    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    public FinalPdfWriter(PdfFont font = PdfFont.Times)
    {
        this.font = font;
        NewPage();
    }

    public int PageCount => pages.Count;

    /// <summary>Ancho promedio de un carácter, en fracción del tamaño de letra (para cortar líneas).</summary>
    public float AverageCharWidth => font == PdfFont.Times ? 0.46f : 0.5f;

    public void NewPage()
    {
        page = new List<Cmd>();
        pages.Add(page);
    }

    public void Text(float x, float y, string text, float size, bool bold = false, string color = "000000") =>
        page.Add(new Cmd { isText = true, x = x, y = y, size = size, bold = bold, color = color, text = Escape(text ?? "") });

    public void Line(float x1, float y, float x2, float width = 0.5f, string color = "000000") =>
        page.Add(new Cmd { x = x1, y = y, w = x2 - x1, lineWidth = width, color = color });

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Rgb(string hex)
    {
        hex = (hex ?? "000000").TrimStart('#');
        float c(int i) => System.Convert.ToInt32(hex.Substring(i, 2), 16) / 255f;
        return $"{F(c(0))} {F(c(2))} {F(c(4))}";
    }

    public byte[] GetBytes()
    {
        var buf = new List<byte>();
        var offsets = new List<int>();
        void W(string s) => buf.AddRange(Encoding.ASCII.GetBytes(s));

        string regular = font == PdfFont.Times ? "Times-Roman" : "Helvetica";
        string bold = font == PdfFont.Times ? "Times-Bold" : "Helvetica-Bold";

        W("%PDF-1.4\n");
        var pageIds = new List<int>();
        var contentIds = new List<int>();
        int next = 5;
        foreach (var _ in pages) { contentIds.Add(next++); pageIds.Add(next++); }

        offsets.Add(buf.Count);
        W("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        offsets.Add(buf.Count);
        W($"2 0 obj\n<< /Type /Pages /Kids [{string.Join(" ", pageIds.ConvertAll(id => id + " 0 R"))}] /Count {pageIds.Count} >>\nendobj\n");
        offsets.Add(buf.Count);
        W($"3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /{regular} /Encoding /WinAnsiEncoding >>\nendobj\n");
        offsets.Add(buf.Count);
        W($"4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /{bold} /Encoding /WinAnsiEncoding >>\nendobj\n");

        for (int i = 0; i < pages.Count; i++)
        {
            var s = new List<byte>();
            void SW(string t) => s.AddRange(Encoding.ASCII.GetBytes(t));

            foreach (var c in pages[i])
            {
                if (c.isText) continue;
                SW($"q {Rgb(c.color)} RG {F(c.lineWidth)} w {F(c.x)} {F(c.y)} m {F(c.x + c.w)} {F(c.y)} l S Q\n");
            }

            SW("BT\n");
            foreach (var c in pages[i])
            {
                if (!c.isText) continue;
                SW($"{Rgb(c.color)} rg {(c.bold ? "/FB" : "/FR")} {F(c.size)} Tf 1 0 0 1 {F(c.x)} {F(c.y)} Tm (");
                s.AddRange(Latin1.GetBytes(c.text));
                SW(") Tj\n");
            }
            SW("ET");

            offsets.Add(buf.Count);
            W($"{contentIds[i]} 0 obj\n<< /Length {s.Count} >>\nstream\n");
            buf.AddRange(s);
            W("\nendstream\nendobj\n");

            offsets.Add(buf.Count);
            W($"{pageIds[i]} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
              "/Resources << /Font << /FR 3 0 R /FB 4 0 R >> >> " +
              $"/Contents {contentIds[i]} 0 R >>\nendobj\n");
        }

        int xref = buf.Count;
        int total = 4 + pages.Count * 2;
        W($"xref\n0 {total + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets) W(off.ToString("D10") + " 00000 n \n");
        W($"trailer\n<< /Size {total + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return buf.ToArray();
    }

    public void Save(string path) => File.WriteAllBytes(path, GetBytes());
}
