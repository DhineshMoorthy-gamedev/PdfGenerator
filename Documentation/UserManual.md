# PDF Generator - User Manual

## Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Working with Pages](#working-with-pages)
5. [Text Elements](#text-elements)
6. [Vector Graphics & Shapes](#vector-graphics--shapes)
7. [Tables](#tables)
8. [Advanced Features](#advanced-features)
9. [API Reference](#api-reference)
10. [Troubleshooting](#troubleshooting)

---

## Introduction

The PDF Generator for Unity is a lightweight, dependency-free package that allows you to create professional PDF documents directly from Unity. Whether you need runtime reports, editor tools, or data exports, this package provides a complete solution.

### Key Capabilities
- Generate PDFs at runtime or in the editor
- Multi-page documents with custom layouts
- Vector graphics (shapes, lines, curves)
- Advanced tables with styling
- No external dependencies

---

## Installation

### Method 1: Package Manager (Recommended)

1. Open Unity's **Package Manager** (Window > Package Manager)
2. Click the **+** button in the top-left
3. Select **Add package from git URL...**
4. Enter: `https://github.com/DhineshMoorthy-gamedev/PdfGenerator.git`
5. Click **Add**

### Method 2: Manual Installation

1. Download the repository as a ZIP file
2. Extract to your project's `Packages` folder
3. Unity will automatically detect and import the package

---

## Getting Started

### Creating Your First PDF

#### Using the Inspector

1. Create an empty GameObject in your scene
2. Add the `PdfReport` component
3. In the inspector, add a new **Page**
4. Add elements to the page (Header, Text, etc.)
5. Right-click the component and select **Generate Report**

#### Using Code

```csharp
using UnityEngine;
using UnityProductivityTools.Runtime;

public class SimplePdfExample : MonoBehaviour
{
    void Start()
    {
        // Create a new report
        PdfReport report = gameObject.AddComponent<PdfReport>();
        report.fileName = "MyFirstPDF.pdf";
        
        // Add a page
        PdfPage page = new PdfPage("Introduction");
        
        // Add elements
        page.elements.Add(PdfElement.CreateHeader("Welcome to PDF Generator"));
        page.elements.Add(PdfElement.CreateText("This is my first generated PDF!"));
        page.elements.Add(PdfElement.CreateDivider());
        
        // Add page to report and generate
        report.pages.Add(page);
        report.Generate();
        
        Debug.Log("PDF created at: " + Application.dataPath + "/../" + report.fileName);
    }
}
```

---

## Working with Pages

### Page Structure

Each PDF document consists of one or more pages. Pages organize your content and can have individual margin settings.

```csharp
PdfPage page = new PdfPage("Page Name");
page.useOverrides = true;
page.topMargin = 60f;
page.bottomMargin = 60f;
page.leftMargin = 50f;
page.rightMargin = 50f;
```

### Global vs. Page Margins

- **Global margins** are set on the `PdfReport` component
- **Page margins** override global settings when `useOverrides = true`

---

## Text Elements

### Headers

```csharp
page.elements.Add(PdfElement.CreateHeader("Section Title"));
```

### Regular Text

```csharp
var textElement = new PdfElement {
    type = PdfElementType.Text,
    text = "Your content here",
    fontSize = 12,
    isBold = false,
    alignment = PdfAlignment.Left,
    topMargin = 10,
    bottomMargin = 10
};
page.elements.Add(textElement);
```

### Text Alignment

- `PdfAlignment.Left` - Left-aligned (default)
- `PdfAlignment.Center` - Centered
- `PdfAlignment.Right` - Right-aligned

### Text Wrapping

Text automatically wraps based on the `maxWidth` property. If not set, it uses the page width minus margins.

---

## Vector Graphics & Shapes

### Basic Shapes

#### Rectangle

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Rectangle,
    width = 200f,
    height = 100f,
    useFill = true,
    fillColor = Color.blue,
    useStroke = true,
    borderColor = Color.black,
    borderThickness = 2f
});
```

#### Rounded Rectangle

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.RoundedRectangle,
    width = 200f,
    height = 100f,
    cornerRadius = 15f,
    useFill = true,
    fillColor = Color.green,
    opacity = 0.5f
});
```

#### Circle

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Circle,
    width = 80f,  // Diameter
    height = 80f, // Must match width for perfect circle
    useFill = true,
    fillColor = Color.red
});
```

#### Ellipse

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Ellipse,
    width = 150f,
    height = 80f,
    useFill = true,
    fillColor = Color.yellow
});
```

#### Line

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Line,
    width = 300f,  // Length
    borderThickness = 2f,
    borderColor = Color.black,
    dashPatternArray = new float[] { 5, 2 }  // Dashed line
});
```

### Complex Shapes

#### Polygon

```csharp
var trianglePoints = new List<Vector2> {
    new Vector2(0, 0),
    new Vector2(50, 50),
    new Vector2(100, 0)
};

page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Polygon,
    points = trianglePoints,
    height = 50f,
    useFill = true,
    fillColor = Color.cyan,
    useStroke = true,
    borderColor = Color.black
});
```

#### Curved Paths (Bezier)

```csharp
var pathSegments = new List<PdfPathSegment> {
    new PdfPathSegment { 
        command = PdfPathCommand.MoveTo, 
        p1 = new Vector2(0, 0) 
    },
    new PdfPathSegment { 
        command = PdfPathCommand.CurveTo,
        p1 = new Vector2(25, 50),  // Control point 1
        p2 = new Vector2(75, 50),  // Control point 2
        p3 = new Vector2(100, 0)   // End point
    },
    new PdfPathSegment { 
        command = PdfPathCommand.LineTo, 
        p1 = new Vector2(100, -20) 
    },
    new PdfPathSegment { 
        command = PdfPathCommand.Close 
    }
};

page.elements.Add(new PdfElement {
    type = PdfElementType.Shape,
    shapeType = PdfShapeType.Path,
    pathSegments = pathSegments,
    height = 50f,
    useFill = true,
    fillColor = new Color(0.5f, 0f, 1f),
    opacity = 0.3f
});
```

### Shape Styling Options

- **Fill**: `useFill`, `fillColor`
- **Stroke**: `useStroke`, `borderColor`, `borderThickness`
- **Opacity**: `opacity` (0.0 to 1.0)
- **Dash Pattern**: `dashPatternArray` (e.g., `new float[] { 5, 2 }`)
- **Line Join**: `lineJoin` (Miter, Round, Bevel)
- **Line Cap**: `lineCap` (Butt, Round, Square)

---

## Tables

### Basic Table

```csharp
var tableElement = new PdfElement {
    type = PdfElementType.Table,
    fontSize = 10,
    showTableBorders = true,
    borderColor = Color.black,
    tableHeaderColor = new Color(0.8f, 0.8f, 0.8f)
};

// Add header row
var headerRow = new PdfTableRow();
headerRow.cells.Add(new PdfTableCell { text = "Name" });
headerRow.cells.Add(new PdfTableCell { text = "Age" });
headerRow.cells.Add(new PdfTableCell { text = "City" });
tableElement.tableData.Add(headerRow);

// Add data rows
var dataRow = new PdfTableRow();
dataRow.cells.Add(new PdfTableCell { text = "John Doe" });
dataRow.cells.Add(new PdfTableCell { text = "30" });
dataRow.cells.Add(new PdfTableCell { text = "New York" });
tableElement.tableData.Add(dataRow);

page.elements.Add(tableElement);
```

### Advanced Table Features

#### Column Spanning

```csharp
var cell = new PdfTableCell { 
    text = "Merged Cell", 
    colspan = 2  // Spans 2 columns
};
```

#### Cell Alignment

```csharp
var cell = new PdfTableCell {
    text = "Centered",
    alignment = PdfAlignment.Center,
    verticalAlignment = PdfVerticalAlignment.Middle
};
```

#### Cell Background Color

```csharp
var cell = new PdfTableCell {
    text = "Highlighted",
    backgroundColor = Color.yellow
};
```

#### Fixed Column Widths

```csharp
tableElement.columnWidths = new List<float> { 100f, 50f, 150f };
```

#### Border Styles

```csharp
tableElement.borderStyle = PdfBorderStyle.Dashed;
tableElement.borderThickness = 1.5f;
```

---

## Advanced Features

### Custom Positioning

```csharp
var element = new PdfElement {
    type = PdfElementType.Text,
    text = "Custom Position",
    useCustomPosition = true,
    customX = 100f,
    customY = 500f
};
```

### Vertical Spacing

```csharp
page.elements.Add(new PdfElement {
    type = PdfElementType.VerticalSpace,
    text = "30"  // 30 units of space
});
```

### Dividers

```csharp
page.elements.Add(PdfElement.CreateDivider());

// Custom divider
page.elements.Add(new PdfElement {
    type = PdfElementType.Divider,
    dividerWidth = 400f,
    dividerThickness = 2f
});
```

### Direct PDF Generator API

For maximum control, use `PdfGenerator` directly:

```csharp
var pdf = new PdfGenerator();
pdf.StartDocument();
pdf.SetColor(Color.black);
pdf.DrawText("Direct API", 50, 16, true);
pdf.DrawRect(50, 700, 200, 100, false);
pdf.Save("DirectAPI.pdf");
```

---

## API Reference

### PdfElement Types

- `Text` - Text blocks with wrapping
- `Divider` - Horizontal lines
- `VerticalSpace` - Spacing between elements
- `Table` - Data tables
- `Shape` - Vector graphics

### PdfShapeType

- `Rectangle` - Basic rectangle
- `RoundedRectangle` - Rectangle with rounded corners
- `Circle` - Perfect circle
- `Ellipse` - Oval shape
- `Line` - Straight line
- `Polygon` - Custom vertices
- `Path` - Bezier curves and complex paths

### PdfPathCommand

- `MoveTo` - Move to a point without drawing
- `LineTo` - Draw straight line to point
- `CurveTo` - Draw cubic Bezier curve
- `Close` - Close the path

---

## Troubleshooting

### PDF Not Generating

**Issue**: No PDF file is created  
**Solution**: Check the console for errors. Ensure the output path is writable.

### Shapes Overlapping

**Issue**: Shapes appear on top of each other  
**Solution**: Ensure you're using version 1.3.0 or later. Set appropriate `height` values for shapes.

### Text Not Wrapping

**Issue**: Text extends beyond page boundaries  
**Solution**: Set the `maxWidth` property on text elements.

### Table Cells Empty

**Issue**: Table appears but cells are blank  
**Solution**: Ensure `colspan` is set to 1 (not 0) for all cells.

### Bezier Curves Not Smooth

**Issue**: Curved paths appear jagged  
**Solution**: Adjust control points (p1, p2) to be closer to the curve path. Use more segments for complex curves.

### PDF Won't Open

**Issue**: Generated PDF is corrupted  
**Solution**: Ensure `pdf.Save()` is called. Check that all drawing operations are between `StartDocument()` and `Save()`.

---

## Examples

See the included demo scripts for complete examples:

- **ShapesDemo.cs** - All shape types with styling
- **TableDemo.cs** - Advanced table features

---

## Support

For issues, feature requests, or contributions:
- GitHub: https://github.com/DhineshMoorthy-gamedev/PdfGenerator
- Issues: https://github.com/DhineshMoorthy-gamedev/PdfGenerator/issues

---

*Last Updated: January 31, 2026 - Version 1.3.0*
