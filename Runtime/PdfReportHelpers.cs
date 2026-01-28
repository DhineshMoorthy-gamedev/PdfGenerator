using UnityEngine;
using UnityProductivityTools.Runtime;

namespace UnityProductivityTools.Runtime
{
    /// <summary>
    /// Helper methods for PdfReport to handle custom drawing operations
    /// </summary>
    public static class PdfReportHelpers
    {
        /// <summary>
        /// Draws a custom divider line with specified thickness and width
        /// </summary>
        public static void DrawDivider(PdfGenerator pdf, float xStart, float width, float thickness)
        {
            float yPos = pdf.cursorY;
            
            // Draw line using PDF line drawing commands
            pdf.currentContent.AppendLine("ET"); // End text
            pdf.currentContent.AppendLine($"{thickness} w"); // Set line width
            pdf.currentContent.AppendLine($"{xStart} {yPos} m"); // Move to start
            pdf.currentContent.AppendLine($"{xStart + width} {yPos} l"); // Line to end
            pdf.currentContent.AppendLine("S"); // Stroke
            pdf.currentContent.AppendLine("BT"); // Begin text again
        }
    }
}
