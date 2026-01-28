using UnityEngine;

namespace UnityProductivityTools.Runtime
{
    public enum PdfElementType
    {
        Text,
        Divider,
        VerticalSpace
    }

    public enum PdfAlignment
    {
        Left,
        Center,
        Right
    }

    [System.Serializable]
    public class PdfElement
    {
        public PdfElementType type = PdfElementType.Text;
        public string text;
        
        public int fontSize = 11;
        public bool isBold = false;
        public Color color = Color.black;
        public PdfAlignment alignment = PdfAlignment.Left;
        
        // Spacing & Layout
        public float spacingBefore = 0f;
        public float spacingAfter = 10f;
        public float leftMargin = 50f;
        public float rightMargin = 50f;
        public float lineHeight = 1.2f; // Multiplier for line spacing in wrapped text
        
        // Advanced Positioning
        public bool useCustomPosition = false;
        public float customX = 0f;
        public float customY = 0f;
        public float maxWidth = 0f; // 0 = auto-calculate based on margins
        
        // Element-Specific Options
        public float dividerThickness = 1f; // For Divider type
        public float dividerWidth = 495f; // For Divider type

        public PdfElement() { }

        public static PdfElement CreateHeader(string text) => new PdfElement { 
            type = PdfElementType.Text, 
            text = text, 
            fontSize = 18, 
            isBold = true, 
            alignment = PdfAlignment.Center, 
            spacingAfter = 20,
            lineHeight = 1.3f
        };
        
        public static PdfElement CreateDivider() => new PdfElement { 
            type = PdfElementType.Divider, 
            spacingAfter = 20 
        };
    }
}
