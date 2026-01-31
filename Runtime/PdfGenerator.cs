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
            currentContent.AppendLine($"{x} {y} {width} {height} re");
            if (fill) currentContent.AppendLine("f");
            else currentContent.AppendLine("S");
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
                AddTempObject($"{pageId} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Contents {contentsId} 0 R /Resources << /Font << /F1 {font1Id} 0 R /F2 {font2Id} 0 R >> >> >> endobj");
                AddTempObject($"{contentsId} 0 obj << /Length {pageStreams[i].Length} >> stream\n{pageStreams[i]}\nendstream endobj");
            }

            AddTempObject($"{font1Id} 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");
            AddTempObject($"{font2Id} 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj");

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
