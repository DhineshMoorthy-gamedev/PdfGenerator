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
        public List<PdfElement> elements = new List<PdfElement>();

        /// <summary>
        /// Generates the PDF report based on the current modular elements.
        /// </summary>
        [ContextMenu("Generate Report")]
        public void Generate()
        {
            if (elements == null || elements.Count == 0)
            {
                Debug.LogWarning("[PdfReport] No elements to generate.");
                return;
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

            foreach (var element in elements)
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
            }
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
        public void AddHeader(string text) => elements.Add(PdfElement.CreateHeader(text));
        public void AddDivider() => elements.Add(PdfElement.CreateDivider());
        //public void AddLabelValue(string label, string value) => elements.Add(PdfElement.CreateLabelValue(label, value));
        public void AddElement(PdfElement element) => elements.Add(element);
        public void Clear() => elements.Clear();
    }
}
