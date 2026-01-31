using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityProductivityTools.Runtime
{
    /// <summary>
    /// A high-level API wrapper for PdfGenerator. 
    /// Attach this to a GameObject to generate reports at runtime.
    /// </summary>
    public class PdfReport : MonoBehaviour
    {
        public string fileName = "ProjectReport.pdf";
        public bool autoOpenAfterSave = true;

        public float topMargin = 50f;
        public float bottomMargin = 50f;
        public float leftMargin = 50f;
        public float rightMargin = 50f;

        [Tooltip("The ordered list of content elements to include in the PDF.")]
        [Header("Page Setup")]
        public List<PdfPage> pages = new List<PdfPage>();

        [HideInInspector, System.Obsolete("Use 'pages' instead. This will be migrated automatically.")]
        public List<PdfElement> elements = new List<PdfElement>();

        /// <summary>
        /// Generates the PDF report based on the current modular elements.
        /// </summary>
        [ContextMenu("Generate Report")]
        public void Generate()
        {
            if ((pages == null || pages.Count == 0) && (elements == null || elements.Count == 0))
            {
                Debug.LogWarning("[PdfReport] No pages or elements to generate.");
                return;
            }

            // Migration check
            if (pages.Count == 0 && elements.Count > 0)
            {
                var migrationPage = new PdfPage("Migrated Content");
                migrationPage.elements.AddRange(elements);
                pages.Add(migrationPage);
                // elements.Clear(); // Optional: Clear to avoid double migration, but HideInInspector is safer for now
            }

            PdfGenerator pdf = CreatePdf();
            
            // Determine save path
            string folder = Application.isEditor ? Directory.GetCurrentDirectory() : Application.persistentDataPath;
            string path = Path.Combine(folder, fileName);
            
            pdf.Save(path);
            Debug.Log($"[PdfReport] Modular report generated at: {path}");

            if (autoOpenAfterSave)
            {
                // Open with cache busting to force browser refresh
                Application.OpenURL("file://" + path + "?t=" + System.DateTime.Now.Ticks);
            }
        }

        private PdfGenerator CreatePdf()
        {
            PdfGenerator pdf = new PdfGenerator();
            pdf.topMargin = topMargin;
            pdf.bottomMargin = bottomMargin;
            pdf.leftMargin = leftMargin;
            pdf.rightMargin = rightMargin;
            
            pdf.StartDocument();

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                // Apply page-specific overrides if enabled
                float currentTop = page.useOverrides ? page.topMargin : topMargin;
                float currentBottom = page.useOverrides ? page.bottomMargin : bottomMargin;
                float currentLeft = page.useOverrides ? page.leftMargin : leftMargin;
                float currentRight = page.useOverrides ? page.rightMargin : rightMargin;

                pdf.topMargin = currentTop;
                pdf.bottomMargin = currentBottom;
                pdf.leftMargin = currentLeft;
                pdf.rightMargin = currentRight;
                
                if (i > 0) 
                    pdf.NewPage();
                else
                    pdf.cursorY = 842 - currentTop - 30;

                foreach (var element in page.elements)
                {
                    // Apply spacing before
                    if (element.spacingBefore > 0)
                    {
                        pdf.AddVerticalSpace(element.spacingBefore);
                    }

                    // Check for page overflow before drawing
                    float spaceNeeded = element.spacingAfter + (element.fontSize * element.lineHeight);
                    pdf.CheckPageOverflow(spaceNeeded);

                    DrawElement(pdf, element);
                    pdf.AddVerticalSpace(element.spacingAfter);
                }
            }

            return pdf;
        }

        private void DrawElement(PdfGenerator pdf, PdfElement el)
        {
            // Add dynamic top padding for large fonts to prevent clipping
            if (el.fontSize > 50)
            {
                float extraTopPadding = (el.fontSize - 50) * 0.3f;
                pdf.AddVerticalSpace(extraTopPadding);
            }

            pdf.SetColor(el.color);

            // Handle custom positioning
            if (el.useCustomPosition)
            {
                // Save current cursor position
                float savedY = pdf.cursorY;
                
                // Temporarily set custom position
                pdf.cursorY = el.customY;
                
                // Draw at custom position
                DrawElementAtPosition(pdf, el, el.customX);
                
                // Restore cursor (or keep custom Y if user wants)
                pdf.cursorY = savedY;
            }
            else
            {
                DrawElementAtPosition(pdf, el, el.leftMargin);
            }
        }
        
        private void DrawElementAtPosition(PdfGenerator pdf, PdfElement el, float xPosition)
        {
            // Use page-level margins as defaults if element margins are at default value (50)
            float effectiveLeftMargin = (el.leftMargin == 50f) ? pdf.leftMargin : el.leftMargin;
            float effectiveRightMargin = (el.rightMargin == 50f) ? pdf.rightMargin : el.rightMargin;
            
            switch (el.type)
            {
                case PdfElementType.Text:
                    float maxWidth = el.maxWidth > 0 ? el.maxWidth : (595f - effectiveLeftMargin - effectiveRightMargin);
                    DrawWrappedText(pdf, el.text, el.fontSize, el.isBold, el.alignment, effectiveLeftMargin, effectiveRightMargin, el.lineHeight, maxWidth);
                    break;

                case PdfElementType.Divider:
                    DrawDivider(pdf, effectiveLeftMargin, el.dividerWidth, el.dividerThickness);
                    pdf.AddVerticalSpace(el.dividerThickness + 10f); // Add some space after a divider
                    break;

                case PdfElementType.VerticalSpace:
                    if (float.TryParse(el.text, out float extra))
                    {
                        pdf.AddVerticalSpace(extra);
                    }
                    else
                    {
                        pdf.AddVerticalSpace(20f); // Default space if parsing fails or text empty
                    }
                    break;

                case PdfElementType.Table:
                    DrawTable(pdf, el);
                    break;
            }
        }

        private void DrawTable(PdfGenerator pdf, PdfElement el)
        {
            if (el.tableData == null || el.tableData.Count == 0) 
            {
                Debug.LogWarning("[PdfReport] Table has no data.");
                return;
            }

            int rowCount = el.tableData.Count;
            // Determine max column count across all rows for width distribution
            int maxCols = 0;
            foreach (var row in el.tableData)
            {
                if (row.cells != null && row.cells.Count > maxCols) maxCols = row.cells.Count;
            }
            
            if (maxCols == 0) 
            {
                Debug.LogWarning("[PdfReport] Table rows have no cells.");
                return;
            }

            float effectiveLeftMargin = (el.leftMargin == 50f) ? pdf.leftMargin : el.leftMargin;
            float effectiveRightMargin = (el.rightMargin == 50f) ? pdf.rightMargin : el.rightMargin;

            float tableWidth = (el.dividerWidth > 0) ? el.dividerWidth : (595f - effectiveLeftMargin - effectiveRightMargin);
            float[] colWidths = new float[maxCols];

            // Distribute width
            float totalFixedWidth = 0;
            int autoCols = 0;
            for (int i = 0; i < maxCols; i++)
            {
                if (el.columnWidths != null && i < el.columnWidths.Count && el.columnWidths[i] > 0)
                    totalFixedWidth += el.columnWidths[i];
                else
                    autoCols++;
            }

            float autoColWidth = Mathf.Max(10f, (tableWidth - totalFixedWidth) / Mathf.Max(1, autoCols));
            for (int i = 0; i < maxCols; i++)
            {
                if (el.columnWidths != null && i < el.columnWidths.Count && el.columnWidths[i] > 0)
                    colWidths[i] = el.columnWidths[i];
                else
                    colWidths[i] = autoColWidth;
            }

            float startX = effectiveLeftMargin;
            if (el.alignment == PdfAlignment.Center) startX = (595f - tableWidth) / 2f;
            else if (el.alignment == PdfAlignment.Right) startX = 595f - effectiveRightMargin - tableWidth;

            // Debug info to help troubleshoot mismatches
            // Debug.Log($"[PdfReport] Drawing Table: {rowCount} rows, {maxCols} columns, total width: {tableWidth}");

            for (int r = 0; r < rowCount; r++)
            {
                bool isHeader = (r == 0 && el.hasTableHeader);
                var row = el.tableData[r];
                if (row.cells == null) continue;
                
                int currentRowCols = row.cells.Count;
                
                // --- PHASE 1: Calculate Row Height (supports wrapping) ---
                float actualRowHeight = el.fontSize + (el.cellPadding * 2f);
                List<List<string>> wrappedRows = new List<List<string>>();

                for (int c = 0; c < currentRowCols; c++)
                {
                    PdfTableCell cell = row.cells[c];
                    string rawText = cell.text ?? "";
                    
                    if (cell.wrapText)
                    {
                        float cW = (c < maxCols) ? colWidths[c] : autoColWidth;
                        float maxWidth = cW - (el.cellPadding * 2);
                        int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(maxWidth / (el.fontSize * 0.53f)));
                        var lines = SplitTextIntoLines(rawText, charsPerLine);
                        wrappedRows.Add(lines);
                        float cellHeight = (lines.Count * el.fontSize) + (el.cellPadding * 2f);
                        if (cellHeight > actualRowHeight) actualRowHeight = cellHeight;
                    }
                    else
                    {
                        wrappedRows.Add(new List<string> { rawText });
                    }
                }

                pdf.CheckPageOverflow(actualRowHeight);

                float currentX = startX;

                // --- PHASE 2: Draw Backgrounds, Borders, and Content ---
                for (int c = 0; c < currentRowCols; c++)
                {
                    if (c >= maxCols) break; // Safety against out of bounds

                    PdfTableCell cell = row.cells[c];
                    int safeColspan = Mathf.Max(1, cell.colspan);
                    
                    float effectiveColWidth = 0;
                    for(int i = 0; i < safeColspan && (c + i) < maxCols; i++) 
                        effectiveColWidth += colWidths[c + i];

                    // 1. Draw Cell Background
                    Color cellBg = cell.backgroundColor;
                    if (isHeader && cellBg.a < 0.01f) cellBg = el.tableHeaderColor;
                    if (cellBg.a > 0.05f) 
                    {
                        pdf.SetColor(cellBg);
                        pdf.DrawRect(currentX, pdf.cursorY - actualRowHeight, effectiveColWidth, actualRowHeight, true);
                    }

                    // 2. Draw Borders (using individual lines to fix dash alignment bug)
                    if (el.showTableBorders)
                    {
                        pdf.SetLineWidth(el.borderThickness);
                        pdf.SetColor(el.borderColor);
                        if (el.borderStyle == PdfBorderStyle.Dashed) pdf.SetDashPattern(new float[] { 3, 3 }, 0);
                        else pdf.SetDashPattern(null, 0);
                        
                        float yBottom = pdf.cursorY - actualRowHeight;
                        float yTop = pdf.cursorY;
                        float xLeft = currentX;
                        float xRight = currentX + effectiveColWidth;

                        // Draw 4 lines with consistent direction to ensure dash alignment
                        pdf.DrawLine(xLeft, yTop, xRight, yTop);       // Top
                        pdf.DrawLine(xLeft, yBottom, xRight, yBottom); // Bottom
                        pdf.DrawLine(xLeft, yTop, xLeft, yBottom);     // Left
                        pdf.DrawLine(xRight, yTop, xRight, yBottom);   // Right
                    }

                    // 3. Draw Content
                    pdf.SetColor(el.color);
                    if (c < wrappedRows.Count)
                    {
                        var linesToDraw = wrappedRows[c];
                        float totalLinesHeight = linesToDraw.Count * el.fontSize;
                        for (int l = 0; l < linesToDraw.Count; l++)
                        {
                            string line = linesToDraw[l];
                            float textWidth = line.Length * el.fontSize * 0.5f; // Approx
                            float textX = currentX + el.cellPadding;
                            if (cell.alignment == PdfAlignment.Center) textX = currentX + (effectiveColWidth / 2f) - (textWidth / 2f);
                            else if (cell.alignment == PdfAlignment.Right) textX = currentX + effectiveColWidth - textWidth - el.cellPadding;
                            textX += cell.offsetX;

                            float startY = pdf.cursorY - el.cellPadding - (el.fontSize * 0.8f);
                            if (cell.verticalAlignment == PdfVerticalAlignment.Middle)
                                startY = pdf.cursorY - (actualRowHeight / 2f) - (totalLinesHeight / 2f) + (el.fontSize * 0.2f);
                            else if (cell.verticalAlignment == PdfVerticalAlignment.Bottom)
                                startY = pdf.cursorY - actualRowHeight + el.cellPadding + ((linesToDraw.Count - 1 - l) * el.fontSize);
                            
                            float lineY = startY - (l * el.fontSize) + cell.offsetY;
                            float savedY = pdf.cursorY;
                            pdf.cursorY = lineY;
                            pdf.DrawText(line, textX, el.fontSize, isHeader || el.isBold);
                            pdf.cursorY = savedY;
                        }
                    }

                    currentX += effectiveColWidth;
                    if (safeColspan > 1) c += (safeColspan - 1);
                }

                if (el.showTableBorders) { pdf.SetLineWidth(1f); pdf.SetDashPattern(null, 0); }
                pdf.AddVerticalSpace(actualRowHeight);
            }
        }

        private List<string> SplitTextIntoLines(string text, int maxChars)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return lines;
            }

            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word)) continue;

                if ((currentLine + word).Length <= maxChars)
                {
                    currentLine += (currentLine == "" ? "" : " ") + word;
                }
                else
                {
                    if (currentLine != "") lines.Add(currentLine);
                    currentLine = word;
                    // Handle words longer than maxChars
                    while (currentLine.Length > maxChars)
                    {
                        lines.Add(currentLine.Substring(0, maxChars));
                        currentLine = currentLine.Substring(maxChars);
                    }
                }
            }
            if (currentLine != "") lines.Add(currentLine);
            if (lines.Count == 0) lines.Add("");
            return lines;
        }

        private void DrawWrappedText(PdfGenerator pdf, string text, int fontSize, bool isBold, PdfAlignment alignment, float leftMargin, float rightMargin, float lineHeight, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Calculate effective width
            float charWidth = fontSize * 0.6f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(maxWidth / charWidth));

            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = currentLine + word;
                
                // If this word alone is too long, break it into chunks
                if (word.Length > charsPerLine)
                {
                    // Flush current line first if it has content
                    if (!string.IsNullOrEmpty(currentLine.Trim()))
                    {
                        DrawLine(pdf, currentLine.Trim(), fontSize, isBold, alignment, leftMargin, rightMargin);
                        pdf.AddVerticalSpace(fontSize * lineHeight);
                        pdf.CheckPageOverflow(fontSize * lineHeight);
                        currentLine = "";
                    }
                    
                    // Break the long word into chunks
                    for (int i = 0; i < word.Length; i += charsPerLine)
                    {
                        string chunk = word.Substring(i, Mathf.Min(charsPerLine, word.Length - i));
                        DrawLine(pdf, chunk, fontSize, isBold, alignment, leftMargin, rightMargin);
                        if (i + charsPerLine < word.Length)
                        {
                            pdf.AddVerticalSpace(fontSize * lineHeight);
                            pdf.CheckPageOverflow(fontSize * lineHeight);
                        }
                    }
                    currentLine = " ";
                }
                else if (testLine.Length > charsPerLine)
                {
                    DrawLine(pdf, currentLine.Trim(), fontSize, isBold, alignment, leftMargin, rightMargin);
                    currentLine = word + " ";
                    pdf.AddVerticalSpace(fontSize * lineHeight);
                    pdf.CheckPageOverflow(fontSize * lineHeight);
                }
                else
                {
                    currentLine += word + " ";
                }
            }

            if (!string.IsNullOrEmpty(currentLine.Trim()))
            {
                DrawLine(pdf, currentLine.Trim(), fontSize, isBold, alignment, leftMargin, rightMargin);
                pdf.AddVerticalSpace(fontSize * lineHeight);
            }
        }

        private void DrawLine(PdfGenerator pdf, string text, int fontSize, bool isBold, PdfAlignment alignment, float leftMargin, float rightMargin)
        {
            switch (alignment)
            {
                case PdfAlignment.Center:
                    pdf.DrawCenteredText(text, fontSize, isBold);
                    break;
                case PdfAlignment.Right:
                    float approxWidth = text.Length * (fontSize * 0.6f);
                    // Use page width minus right margin for right alignment
                    pdf.DrawText(text, 595f - rightMargin - approxWidth, fontSize, isBold);
                    break;
                default:
                    pdf.DrawText(text, leftMargin, fontSize, isBold);
                    break;
            }
        }
        
        private void DrawDivider(PdfGenerator pdf, float xStart, float width, float thickness)
        {
            PdfReportHelpers.DrawDivider(pdf, xStart, width, thickness);
        }

        // API Methods
        public void AddHeader(string text)
        {
            if (pages.Count == 0) pages.Add(new PdfPage("Main Page"));
            pages[pages.Count - 1].elements.Add(PdfElement.CreateHeader(text));
        }

        public void AddDivider()
        {
            if (pages.Count == 0) pages.Add(new PdfPage("Main Page"));
            pages[pages.Count - 1].elements.Add(PdfElement.CreateDivider());
        }

        public void AddElement(PdfElement element)
        {
            if (pages.Count == 0) pages.Add(new PdfPage("Main Page"));
            pages[pages.Count - 1].elements.Add(element);
        }

        public void AddNewPage(string name = "New Page") => pages.Add(new PdfPage(name));

        public void Clear()
        {
            pages.Clear();
            elements.Clear();
        }
    }
}
