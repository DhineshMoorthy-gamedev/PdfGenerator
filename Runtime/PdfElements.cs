using System.Collections.Generic;
using UnityEngine;

namespace UnityProductivityTools.Runtime
{
    public enum PdfElementType
    {
        Text,
        Divider,
        VerticalSpace,
        Table
    }

    public enum PdfAlignment
    {
        Left,
        Center,
        Right
    }

    public enum PdfVerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    public enum PdfBorderStyle
    {
        Solid,
        Dashed
    }

    [System.Serializable]
    public class PdfTableCell
    {
        public string text;
        public PdfAlignment alignment = PdfAlignment.Left;
        public PdfVerticalAlignment verticalAlignment = PdfVerticalAlignment.Middle;
        public float offsetX = 0f;
        public float offsetY = 0f;
        
        public Color backgroundColor = Color.clear;
        public int colspan = 1;
        public string imagePath;
        public bool wrapText = true;
        
        public PdfTableCell() 
        { 
            colspan = 1;
            wrapText = true;
            alignment = PdfAlignment.Left;
            verticalAlignment = PdfVerticalAlignment.Middle;
            backgroundColor = Color.clear;
        }
        
        public PdfTableCell(string text, PdfAlignment alignment = PdfAlignment.Left) : this()
        {
            this.text = text;
            this.alignment = alignment;
        }
    }

    [System.Serializable]
    public class PdfTableRow
    {
        public List<PdfTableCell> cells = new List<PdfTableCell>();
        public PdfTableRow() { }
        public PdfTableRow(int columnCount)
        {
            for (int i = 0; i < columnCount; i++) cells.Add(new PdfTableCell());
        }
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
        public float dividerWidth = 0f; // 0 = auto-calculate based on margins

        // Table Options
        public List<PdfTableRow> tableData = new List<PdfTableRow>();
        public bool showTableBorders = true;
        public float borderThickness = 1f;
        public Color borderColor = Color.black;
        public PdfBorderStyle borderStyle = PdfBorderStyle.Solid;
        public float cellPadding = 5f;
        public bool hasTableHeader = true;
        public Color tableHeaderColor = new Color(0.9f, 0.9f, 0.9f);
        public List<float> columnWidths = new List<float>(); // Optional overrides

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
    [System.Serializable]
    public class PdfPage
    {
        public string pageName = "New Page";
        public List<PdfElement> elements = new List<PdfElement>();
        
        public bool useOverrides = false;
        public float topMargin = 50f;
        public float bottomMargin = 50f;
        public float leftMargin = 50f;
        public float rightMargin = 50f;

        public PdfPage() { }
        public PdfPage(string name) => pageName = name;
    }
}
