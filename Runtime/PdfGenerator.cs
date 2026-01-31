using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityProductivityTools.Runtime
{
    /// <summary>
    /// A lightweight, dependency-free PDF generator utility for Unity.
    /// Derived from PoC by Dhinesh Moorthy.
    /// </summary>
    public class PdfGenerator
    {
        private StringBuilder sb;
        private int pageCount;
        private List<int> offsets;
        private List<StringBuilder> pageStreams;
        public StringBuilder currentContent; // Made public for custom drawing
        public float cursorY; // Made public for custom positioning
        private float pageWidth = 595;
        private float pageHeight = 842;
        public float topMargin = 50;
        public float bottomMargin = 50;
        public float leftMargin = 50;
        public float rightMargin = 50;
        
        // Transparency support
        private Dictionary<float, string> opacityStates = new Dictionary<float, string>();
        private int gsCount = 0;

        public PdfGenerator()
        {
            sb = new StringBuilder();
            offsets = new List<int>();
            pageStreams = new List<StringBuilder>();
        }

        public void StartDocument()
        {
            sb.Clear();
            offsets.Clear();
            pageStreams.Clear();
            sb.AppendLine("%PDF-1.4");
            NewPage();
        }

        public void NewPage()
        {
            if (currentContent != null && !currentContent.ToString().EndsWith("ET\n"))
            {
                currentContent.AppendLine("ET");
            }

            currentContent = new StringBuilder();
            currentContent.AppendLine("BT");
            pageStreams.Add(currentContent);
            cursorY = pageHeight - topMargin - 30; // 30 is extra safety padding for fonts
        }

        public void AddVerticalSpace(float amount)
        {
            cursorY -= amount;
        }

        public void DrawText(string text, float x, float size, bool isBold = false)
        {
            string font = isBold ? "/F2" : "/F1";
            currentContent.AppendLine($"{font} {size} Tf");
            currentContent.AppendLine($"1 0 0 1 {x} {cursorY} Tm");
            currentContent.AppendLine($"({Escape(text)}) Tj");
        }

        public void SetColor(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;
            
            // rg for non-stroking (text/fills), RG for stroking (lines)
            currentContent.AppendLine($"{r:F3} {g:F3} {b:F3} rg");
            currentContent.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
        }

        public void DrawCenteredText(string text, float size, bool isBold = false)
        {
            // Approximate character width for Helvetica
            // Using 0.6f to be safer for wider characters (W, M, etc.)
            float approxWidth = text.Length * (size * 0.6f);
            float x = (pageWidth - approxWidth) / 2f;
            DrawText(text, x, size, isBold);
        }

        public void DrawLine(float xStart, float xEnd) => DrawLine(xStart, cursorY, xEnd, cursorY);

        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{x1:F3} {y1:F3} m {x2:F3} {y2:F3} l S");
            currentContent.AppendLine("BT");
        }

        public void DrawRect(float x, float y, float width, float height, bool fill = false)
        {
            currentContent.AppendLine("ET");
            // re (rect), S (stroke), f (fill)
            currentContent.AppendLine($"{x:F3} {y:F3} {width:F3} {height:F3} re");
            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
            currentContent.AppendLine("BT");
        }

        public void DrawRoundedRect(float x, float y, float w, float h, float r, bool fill = false)
        {
            if (r <= 0) { DrawRect(x, y, w, h, fill); return; }
            r = Mathf.Min(r, Mathf.Min(w / 2, h / 2));
            float k = 0.552284749831f; // (4/3)*(sqrt(2)-1)
            float kr = k * r;

            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{x + r:F3} {y:F3} m"); // Start bottom-left + radius
            currentContent.AppendLine($"{x + w - r:F3} {y:F3} l"); // Bottom edge
            currentContent.AppendLine($"{x + w - r + kr:F3} {y:F3} {x + w:F3} {y + r - kr:F3} {x + w:F3} {y + r:F3} c"); // Bottom-right corner
            currentContent.AppendLine($"{x + w:F3} {y + h - r:F3} l"); // Right edge
            currentContent.AppendLine($"{x + w:F3} {y + h - r + kr:F3} {x + w - r + kr:F3} {y + h:F3} {x + w - r:F3} {y + h:F3} c"); // Top-right corner
            currentContent.AppendLine($"{x + r:F3} {y + h:F3} l"); // Top edge
            currentContent.AppendLine($"{x + r - kr:F3} {y + h:F3} {x:F3} {y + h - r + kr:F3} {x:F3} {y + h - r:F3} c"); // Top-left corner
            currentContent.AppendLine($"{x:F3} {y + r:F3} l"); // Left edge
            currentContent.AppendLine($"{x:F3} {y + r - kr:F3} {x + r - kr:F3} {y:F3} {x + r:F3} {y:F3} c"); // Bottom-left corner
            
            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
            currentContent.AppendLine("BT");
        }

        public void DrawEllipse(float x, float y, float rw, float rh, bool fill = false)
        {
            float k = 0.552284749831f;
            float kx = k * rw;
            float ky = k * rh;

            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{x + rw:F3} {y:F3} m");
            currentContent.AppendLine($"{x + rw:F3} {y + ky:F3} {x + kx:F3} {y + rh:F3} {x:F3} {y + rh:F3} c");
            currentContent.AppendLine($"{x - kx:F3} {y + rh:F3} {x - rw:F3} {y + ky:F3} {x - rw:F3} {y:F3} c");
            currentContent.AppendLine($"{x - rw:F3} {y - ky:F3} {x - kx:F3} {y - rh:F3} {x:F3} {y - rh:F3} c");
            currentContent.AppendLine($"{x + kx:F3} {y - rh:F3} {x + rw:F3} {y - ky:F3} {x + rw:F3} {y:F3} c");

            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
            currentContent.AppendLine("BT");
        }

        public void DrawPolygon(Vector2[] points, bool fill = false)
        {
            if (points == null || points.Length < 2) return;
            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{points[0].x:F3} {points[0].y:F3} m");
            for (int i = 1; i < points.Length; i++)
                currentContent.AppendLine($"{points[i].x:F3} {points[i].y:F3} l");
            
            currentContent.AppendLine("h"); // Close path
            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
            currentContent.AppendLine("BT");
        }

        public void DrawPath(List<PdfPathSegment> segments, float offsetX, float offsetY, bool fill = false)
        {
            if (segments == null || segments.Count == 0) return;
            
            currentContent.AppendLine("ET");
            
            foreach (var segment in segments)
            {
                switch (segment.command)
                {
                    case PdfPathCommand.MoveTo:
                        currentContent.AppendLine($"{segment.p1.x + offsetX:F3} {segment.p1.y + offsetY:F3} m");
                        break;
                    case PdfPathCommand.LineTo:
                        currentContent.AppendLine($"{segment.p1.x + offsetX:F3} {segment.p1.y + offsetY:F3} l");
                        break;
                    case PdfPathCommand.CurveTo:
                        // c operator: x1 y1 x2 y2 x3 y3 c
                        currentContent.AppendLine($"{segment.p1.x + offsetX:F3} {segment.p1.y + offsetY:F3} {segment.p2.x + offsetX:F3} {segment.p2.y + offsetY:F3} {segment.p3.x + offsetX:F3} {segment.p3.y + offsetY:F3} c");
                        break;
                    case PdfPathCommand.Close:
                        currentContent.AppendLine("h");
                        break;
                }
            }

            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
            currentContent.AppendLine("BT");
        }

        public void SetLineJoin(PdfLineJoin join)
        {
            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{(int)join} j");
            currentContent.AppendLine("BT");
        }

        public void SetLineCap(PdfLineCap cap)
        {
            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{(int)cap} J");
            currentContent.AppendLine("BT");
        }

        public void SetOpacity(float opacity)
        {
            if (opacity >= 1.0f) return;
            
            // PDF needs an ExtGState for transparency
            if (!opacityStates.ContainsKey(opacity))
            {
                gsCount++;
                string name = $"GS{gsCount}";
                opacityStates[opacity] = name;
            }

            currentContent.AppendLine("ET");
            currentContent.AppendLine($"/{opacityStates[opacity]} gs");
            currentContent.AppendLine("BT");
        }

        public void SetLineWidth(float width)
        {
            currentContent.AppendLine("ET");
            currentContent.AppendLine($"{width} w");
            currentContent.AppendLine("BT");
        }

        public void SetDashPattern(float[] dashArray, float phase)
        {
            currentContent.AppendLine("ET");
            if (dashArray == null || dashArray.Length == 0)
            {
                currentContent.AppendLine("[] 0 d");
            }
            else
            {
                string arrayStr = string.Join(" ", dashArray);
                currentContent.AppendLine($"[{arrayStr}] {phase} d");
            }
            currentContent.AppendLine("BT");
        }

        public void DrawHorizontalRule()
        {
            DrawLine(leftMargin, pageWidth - rightMargin);
        }

        public string GetPdfString()
        {
            if (currentContent != null && !currentContent.ToString().EndsWith("ET\n"))
            {
                currentContent.AppendLine("ET");
            }

            // Create a temporary copy of sb to append the structural elements
            // without modifying the internal state permanently in case user calls it multiple times
            StringBuilder finalSb = new StringBuilder(sb.ToString());
            List<int> tempOffsets = new List<int>(offsets);

            void AddTempObject(string content)
            {
                tempOffsets.Add(finalSb.Length);
                finalSb.AppendLine(content);
            }

            // PDF Catalog
            AddTempObject("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");

            // Pages List
            string kids = "";
            for (int i = 0; i < pageStreams.Count; i++) kids += (3 + i * 2) + " 0 R ";
            AddTempObject($"2 0 obj << /Type /Pages /Kids [{kids}] /Count {pageStreams.Count} >> endobj");

            // Fonts
            int font1Id = 3 + pageStreams.Count * 2;
            int font2Id = font1Id + 1;

            // Page Objects
            for (int i = 0; i < pageStreams.Count; i++)
            {
                int pageId = 3 + i * 2;
                int contentsId = pageId + 1;
                
                string extGState = "";
                if (opacityStates.Count > 0)
                {
                    extGState = "/ExtGState << ";
                    foreach (var kvp in opacityStates)
                    {
                        // Placeholder, fixed below in extGStateDict
                    }
                    extGState += " >> ";
                }

                // Correct Calculation:
                // obj 1: Catalog
                // obj 2: Pages
                // obj 3 to 2+N*2: Page components (2 per page)
                // obj 2+N*2+1: Font 1
                // obj 2+N*2+2: Font 2
                // obj 2+N*2+3...: ExtGStates
                
                int firstFontId = 3 + pageStreams.Count * 2;
                int secondFontId = firstFontId + 1;
                int firstExtGStateId = secondFontId + 1;

                string extGStateDict = "";
                if (opacityStates.Count > 0)
                {
                    extGStateDict = "/ExtGState << ";
                    int idx = 0;
                    foreach (var kvp in opacityStates)
                    {
                        extGStateDict += $"/{kvp.Value} {firstExtGStateId + idx} 0 R ";
                        idx++;
                    }
                    extGStateDict += " >> ";
                }

                AddTempObject($"{pageId} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Contents {contentsId} 0 R /Resources << /Font << /F1 {firstFontId} 0 R /F2 {secondFontId} 0 R >> {extGStateDict} >> >> endobj");
                AddTempObject($"{contentsId} 0 obj << /Length {pageStreams[i].Length} >> stream\n{pageStreams[i]}\nendstream endobj");
            }

            // Font objects
            AddTempObject($"{font1Id} 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");
            AddTempObject($"{font2Id} 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj");

            // Add ExtGState objects
            if (opacityStates.Count > 0)
            {
                foreach (var kvp in opacityStates)
                {
                    // /ca is non-stroking, /CA is stroking
                    AddTempObject($"{tempOffsets.Count + 1} 0 obj << /Type /ExtGState /ca {kvp.Key:F3} /CA {kvp.Key:F3} >> endobj");
                }
            }

            int xrefPos = finalSb.Length;
            finalSb.AppendLine("xref");
            finalSb.AppendLine($"0 {tempOffsets.Count + 1}");
            finalSb.AppendLine("0000000000 65535 f ");
            foreach (var o in tempOffsets) finalSb.AppendLine(o.ToString("0000000000") + " 00000 n ");

            finalSb.AppendLine("trailer");
            finalSb.AppendLine($"<< /Size {tempOffsets.Count + 1} /Root 1 0 R >>");
            finalSb.AppendLine("startxref");
            finalSb.AppendLine(xrefPos.ToString());
            finalSb.AppendLine("%%EOF");

            return finalSb.ToString();
        }

        public byte[] GetPdfBytes()
        {
            return Encoding.ASCII.GetBytes(GetPdfString());
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, GetPdfBytes());
        }

        private void AddObject(string content)
        {
            offsets.Add(sb.Length);
            sb.AppendLine(content);
        }

        private string Escape(string t)
        {
            return t.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        public float GetCursorY() => cursorY;
        public bool CheckPageOverflow(float spaceNeeded)
        {
            if (cursorY - spaceNeeded < bottomMargin)
            {
                NewPage();
                return true;
            }
            return false;
        }
    }
}
