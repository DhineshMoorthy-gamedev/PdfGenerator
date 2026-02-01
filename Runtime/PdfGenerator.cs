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
        
        // Image support
        private Dictionary<string, ImageData> embeddedImages = new Dictionary<string, ImageData>();
        private int imageCount = 0;
        
        private class ImageData
        {
            public string name;
            public byte[] data;
            public int width;
            public int height;
            public int objectId;
        }

        public PdfGenerator()
        {
            sb = new StringBuilder();
            offsets = new List<int>();
            pageStreams = new List<StringBuilder>();
            embeddedImages = new Dictionary<string, ImageData>();
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

        /// <summary>
        /// Embeds an image from file path and returns the image name for use in DrawImage.
        /// Returns null if the image cannot be loaded.
        /// </summary>
        public string EmbedImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogWarning("[PdfGenerator] Image path is null or empty.");
                return null;
            }

            if (!File.Exists(imagePath))
            {
                Debug.LogWarning($"[PdfGenerator] Image file not found at path: {imagePath}");
                return null;
            }

            // Check if already embedded
            if (embeddedImages.ContainsKey(imagePath))
            {
                return embeddedImages[imagePath].name;
            }

            try
            {
                Debug.Log($"[PdfGenerator] Attempting to embed image: {imagePath}");
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                Debug.Log($"[PdfGenerator] Read {imageBytes.Length} bytes from file.");
                
                // Get image dimensions
                int width, height;
                if (!GetJpegDimensions(imageBytes, out width, out height))
                {
                    Debug.LogWarning($"[PdfGenerator] Could not read JPEG dimensions: {imagePath}. Ensure it is a valid JPEG/JPG file.");
                    return null;
                }

                Debug.Log($"[PdfGenerator] Detected image dimensions: {width}x{height}");

                imageCount++;
                string imageName = $"Im{imageCount}";

                var imageData = new ImageData
                {
                    name = imageName,
                    data = imageBytes,
                    width = width,
                    height = height,
                    objectId = -1 // Will be set during PDF generation
                };

                embeddedImages[imagePath] = imageData;
                return imageName;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PdfGenerator] Error embedding image {imagePath}: {e.Message}");
                return null;
            }
        }

        public void DrawHorizontalRule()
        {
            DrawLine(leftMargin, pageWidth - rightMargin);
        }

        /// <summary>
        /// Draws an embedded image at the specified position with given dimensions.
        /// </summary>
        public void DrawImage(string imageName, float x, float y, float width, float height)
        {
            if (string.IsNullOrEmpty(imageName)) return;

            if (currentContent != null && !currentContent.ToString().EndsWith("ET\n"))
            {
                currentContent.AppendLine("ET");
            }
            
            currentContent.AppendLine("q"); // Save graphics state
            
            // Transform matrix: width 0 0 height x y cm
            currentContent.AppendLine($"{width:F3} 0 0 {height:F3} {x:F3} {y:F3} cm");
            currentContent.AppendLine($"/{imageName} Do");
            
            currentContent.AppendLine("Q"); // Restore graphics state
            currentContent.AppendLine("BT");
        }

        /// <summary>
        /// Gets the dimensions of an embedded image.
        /// </summary>
        public bool GetImageDimensions(string imagePath, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (embeddedImages.ContainsKey(imagePath))
            {
                width = embeddedImages[imagePath].width;
                height = embeddedImages[imagePath].height;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads JPEG dimensions from byte array.
        /// </summary>
        private bool GetJpegDimensions(byte[] data, out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                // Check for JPEG magic number (FF D8)
                if (data.Length < 2 || data[0] != 0xFF || data[1] != 0xD8)
                {
                    Debug.LogWarning($"[PdfGenerator] Invalid JPEG header: {data[0]:X2} {data[1]:X2}");
                    return false;
                }

                int i = 2;
                while (i < data.Length - 1)
                {
                    // Search for next marker (starts with FF)
                    if (data[i] != 0xFF)
                    {
                        i++;
                        continue;
                    }
                    
                    byte marker = data[i + 1];
                    i += 2;

                    // Skip padding FF bytes
                    while (marker == 0xFF && i < data.Length)
                    {
                        marker = data[i];
                        i++;
                    }

                    // SOF markers (Start of Frame)
                    // C0-C3, C5-C7, C9-CB, CD-CF are SOF markers
                    if ((marker >= 0xC0 && marker <= 0xC3) || 
                        (marker >= 0xC5 && marker <= 0xC7) ||
                        (marker >= 0xC9 && marker <= 0xCB) ||
                        (marker >= 0xCD && marker <= 0xCF))
                    {
                        if (i + 7 >= data.Length) return false;
                        
                        // Skip length (2 bytes) and precision (1 byte)
                        i += 3;
                        
                        // Read height (2 bytes, big-endian)
                        height = (data[i] << 8) | data[i + 1];
                        i += 2;
                        
                        // Read width (2 bytes, big-endian)
                        width = (data[i] << 8) | data[i + 1];
                        
                        return true;
                    }
                    else if (marker == 0xD9 || marker == 0xDA) // EOI or SOS - stop searching
                    {
                        break;
                    }
                    else
                    {
                        // Skip this segment
                        if (i + 1 >= data.Length) return false;
                        int segmentLength = (data[i] << 8) | data[i + 1];
                        i += segmentLength;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PdfGenerator] Error parsing JPEG markers: {e.Message}");
                return false;
            }

            return false;
        }

        public byte[] GetPdfBytes()
        {
            if (currentContent != null && !currentContent.ToString().EndsWith("ET\n"))
            {
                currentContent.AppendLine("ET");
            }

            // Create a temporary copy of sb to append the structural elements
            // without modifying the internal state permanently in case user calls it multiple times
            StringBuilder finalSb = new StringBuilder(sb.ToString());
            List<int> tempOffsets = new List<int>();

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
            int firstExtGStateId = font2Id + 1;
            int firstImageId = firstExtGStateId + opacityStates.Count;

            // Assign object IDs to images
            int imageIdx = 0;
            foreach (var kvp in embeddedImages)
            {
                kvp.Value.objectId = firstImageId + imageIdx;
                imageIdx++;
            }

            // Page Objects
            for (int i = 0; i < pageStreams.Count; i++)
            {
                int pageId = 3 + i * 2;
                int contentsId = pageId + 1;

                // Build ExtGState dictionary
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

                // Build XObject dictionary for images
                string xObjectDict = "";
                if (embeddedImages.Count > 0)
                {
                    xObjectDict = "/XObject << ";
                    foreach (var kvp in embeddedImages)
                    {
                        xObjectDict += $"/{kvp.Value.name} {kvp.Value.objectId} 0 R ";
                    }
                    xObjectDict += " >> ";
                }

                AddTempObject($"{pageId} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Contents {contentsId} 0 R /Resources << /Font << /F1 {font1Id} 0 R /F2 {font2Id} 0 R >> {extGStateDict}{xObjectDict}>> >> endobj");
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
                    tempOffsets.Add(finalSb.Length);
                    finalSb.AppendLine($"{tempOffsets.Count} 0 obj << /Type /ExtGState /ca {kvp.Key:F3} /CA {kvp.Key:F3} >> endobj");
                }
            }

            // Construct final bytes using ISO-8859-1 for text part
            byte[] textBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(finalSb.ToString());
            List<byte> pdfBytes = new List<byte>(textBytes);

            // Add Image XObjects
            if (embeddedImages.Count > 0)
            {
                foreach (var kvp in embeddedImages)
                {
                    var img = kvp.Value;
                    int objId = img.objectId;
                    
                    // Record offset for xref
                    tempOffsets.Add(pdfBytes.Count);
                    
                    // Image XObject header
                    string header = $"{objId} 0 obj << /Type /XObject /Subtype /Image /Width {img.width} /Height {img.height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {img.data.Length} >> stream\n";
                    pdfBytes.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(header));
                    
                    // Add binary image data
                    pdfBytes.AddRange(img.data);
                    
                    // Close stream
                    string footer = "\nendstream endobj\n";
                    pdfBytes.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(footer));
                }
            }

            // Build xref and trailer
            int xrefPos = pdfBytes.Count;
            StringBuilder xrefSb = new StringBuilder();
            xrefSb.Append("xref\r\n");
            xrefSb.Append($"0 {tempOffsets.Count + 1}\r\n");
            xrefSb.Append("0000000000 65535 f \r\n");
            
            foreach (var o in tempOffsets)
            {
                xrefSb.Append(o.ToString("0000000000"));
                xrefSb.Append(" 00000 n \r\n");
            }

            xrefSb.Append("trailer\r\n");
            xrefSb.Append("<< /Size ").Append(tempOffsets.Count + 1).Append(" /Root 1 0 R >>\r\n");
            xrefSb.Append("startxref\r\n");
            xrefSb.Append(xrefPos).Append("\r\n");
            xrefSb.Append("%%EOF\r\n");

            pdfBytes.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(xrefSb.ToString()));

            return pdfBytes.ToArray();
        }

        public string GetPdfString()
        {
            // Use ISO-8859-1 (Latin-1) for string representation to preserve binary data 1:1
            try {
                return Encoding.GetEncoding("ISO-8859-1").GetString(GetPdfBytes());
            } catch {
                return Encoding.UTF8.GetString(GetPdfBytes()); // Fallback
            }
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
