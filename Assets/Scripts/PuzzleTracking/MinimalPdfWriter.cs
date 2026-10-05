using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class MinimalPdfWriter
{
    private class TextItem { public float x, y, size; public string text; public bool bold; public string colorHex; }
    private class RectItem { public float x, y, w, h; public string fillHex; public string borderHex; public float borderWidth; }
    private class DrawCommand { public bool isText; public TextItem text; public RectItem rect; }

    private List<List<DrawCommand>> pages = new List<List<DrawCommand>>();
    private List<DrawCommand> currentPage;

    private static readonly Encoding PdfTextEncoding = Encoding.GetEncoding("ISO-8859-1");

    public MinimalPdfWriter() => NewPage();

    public void NewPage()
    {
        currentPage = new List<DrawCommand>();
        pages.Add(currentPage);
    }

    public void DrawText(float x, float y, string text, float size = 12, bool bold = false, string colorHex = "222222")
    {
        currentPage.Add(new DrawCommand
        {
            isText = true,
            text = new TextItem { x = x, y = y, size = size, text = EscapePdfText(text), bold = bold, colorHex = colorHex }
        });
    }

    // fillHex/borderHex en formato "RRGGBB" (sin #). Pasar null para omitir relleno o borde.
    public void DrawRect(float x, float y, float width, float height, string fillHex = null, string borderHex = null, float borderWidth = 1f)
    {
        currentPage.Add(new DrawCommand
        {
            isText = false,
            rect = new RectItem { x = x, y = y, w = width, h = height, fillHex = fillHex, borderHex = borderHex, borderWidth = borderWidth }
        });
    }

    // Línea simple (usada para separadores o bordes de tabla)
    public void DrawLine(float x1, float y1, float x2, float y2, string colorHex = "D1D5DB", float width = 1f)
    {
        // Se representa como un rectángulo delgado
        if (Mathf_Approximately(y1, y2))
            DrawRect(x1, y1 - width / 2f, x2 - x1, width, fillHex: colorHex);
        else
            DrawRect(x1 - width / 2f, y2, width, y1 - y2, fillHex: colorHex);
    }

    private bool Mathf_Approximately(float a, float b) => System.Math.Abs(a - b) < 0.01f;

    private string EscapePdfText(string s)
        => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static (float r, float g, float b) HexToRgb01(string hex)
    {
        hex = hex.TrimStart('#');
        int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
        return (r / 255f, g / 255f, b / 255f);
    }

    public byte[] GetBytes()
    {
        var buffer = new List<byte>();
        var offsets = new List<int>();

        void WriteAscii(string s) => buffer.AddRange(Encoding.ASCII.GetBytes(s));
        int CurrentOffset() => buffer.Count;

        WriteAscii("%PDF-1.4\n");

        int catalogId = 1, pagesId = 2, fontRegularId = 3, fontBoldId = 4;
        var pageIds = new List<int>();
        var contentIds = new List<int>();
        int nextId = 5;

        foreach (var _ in pages)
        {
            contentIds.Add(nextId++);
            pageIds.Add(nextId++);
        }

        offsets.Add(CurrentOffset());
        WriteAscii($"{catalogId} 0 obj\n<< /Type /Catalog /Pages {pagesId} 0 R >>\nendobj\n");

        string kids = string.Join(" ", pageIds.ConvertAll(id => $"{id} 0 R"));
        offsets.Add(CurrentOffset());
        WriteAscii($"{pagesId} 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageIds.Count} >>\nendobj\n");

        offsets.Add(CurrentOffset());
        WriteAscii($"{fontRegularId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

        offsets.Add(CurrentOffset());
        WriteAscii($"{fontBoldId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        for (int i = 0; i < pages.Count; i++)
        {
            var streamBytes = new List<byte>();
            void SWrite(string s) => streamBytes.AddRange(Encoding.ASCII.GetBytes(s));
            void SWriteText(string s) => streamBytes.AddRange(PdfTextEncoding.GetBytes(s));

            foreach (var cmd in pages[i])
            {
                if (!cmd.isText)
                {
                    var r = cmd.rect;
                    SWrite("q\n");

                    if (!string.IsNullOrEmpty(r.fillHex))
                    {
                        var (fr, fg, fb) = HexToRgb01(r.fillHex);
                        SWrite($"{fr.ToString(CultureInfo.InvariantCulture)} {fg.ToString(CultureInfo.InvariantCulture)} {fb.ToString(CultureInfo.InvariantCulture)} rg\n");
                        SWrite($"{r.x.ToString(CultureInfo.InvariantCulture)} {r.y.ToString(CultureInfo.InvariantCulture)} {r.w.ToString(CultureInfo.InvariantCulture)} {r.h.ToString(CultureInfo.InvariantCulture)} re\n");
                        SWrite(string.IsNullOrEmpty(r.borderHex) ? "f\n" : "f\n");
                    }

                    if (!string.IsNullOrEmpty(r.borderHex))
                    {
                        var (br, bg, bb) = HexToRgb01(r.borderHex);
                        SWrite($"{br.ToString(CultureInfo.InvariantCulture)} {bg.ToString(CultureInfo.InvariantCulture)} {bb.ToString(CultureInfo.InvariantCulture)} RG\n");
                        SWrite($"{r.borderWidth.ToString(CultureInfo.InvariantCulture)} w\n");
                        SWrite($"{r.x.ToString(CultureInfo.InvariantCulture)} {r.y.ToString(CultureInfo.InvariantCulture)} {r.w.ToString(CultureInfo.InvariantCulture)} {r.h.ToString(CultureInfo.InvariantCulture)} re\n");
                        SWrite("S\n");
                    }

                    SWrite("Q\n");
                }
            }

            SWrite("BT\n");
            foreach (var cmd in pages[i])
            {
                if (!cmd.isText) continue;
                var item = cmd.text;
                string fontKey = item.bold ? "/FB" : "/FR";
                var (tr, tg, tb) = HexToRgb01(item.colorHex ?? "222222");

                SWrite($"{tr.ToString(CultureInfo.InvariantCulture)} {tg.ToString(CultureInfo.InvariantCulture)} {tb.ToString(CultureInfo.InvariantCulture)} rg\n");
                SWrite($"{fontKey} {item.size.ToString(CultureInfo.InvariantCulture)} Tf\n");
                SWrite($"1 0 0 1 {item.x.ToString(CultureInfo.InvariantCulture)} {item.y.ToString(CultureInfo.InvariantCulture)} Tm\n");
                SWrite("(");
                SWriteText(item.text); // <-- corregido: antes decía item.text.text
                SWrite(") Tj\n");
            }
            SWrite("ET");

            int len = streamBytes.Count;

            offsets.Add(CurrentOffset());
            WriteAscii($"{contentIds[i]} 0 obj\n<< /Length {len} >>\nstream\n");
            buffer.AddRange(streamBytes);
            WriteAscii("\nendstream\nendobj\n");

            offsets.Add(CurrentOffset());
            WriteAscii($"{pageIds[i]} 0 obj\n<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 595 842] " +
                       $"/Resources << /Font << /FR {fontRegularId} 0 R /FB {fontBoldId} 0 R >> >> " +
                       $"/Contents {contentIds[i]} 0 R >>\nendobj\n");
        }

        int xrefStart = CurrentOffset();
        int totalObjs = 4 + pages.Count * 2;
        WriteAscii($"xref\n0 {totalObjs + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets)
            WriteAscii(off.ToString("D10") + " 00000 n \n");

        WriteAscii($"trailer\n<< /Size {totalObjs + 1} /Root {catalogId} 0 R >>\nstartxref\n{xrefStart}\n%%EOF");

        return buffer.ToArray();
    }

    public void Save(string path) => File.WriteAllBytes(path, GetBytes());
}