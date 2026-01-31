# PDF Generator for Unity

A lightweight, dependency-free PDF generator utility for Unity. This package allows you to generate PDF reports directly from Unity, both at runtime and in the Editor.

## Features

- **Dependency-Free**: No external libraries required.
- **Runtime Support**: Generate PDFs on any platform supported by Unity.
- **Multi-Page Support**: Organize reports into multiple pages with explicit control.
- **Custom Margins**: Set global margins or override them per page for ultimate layout control.
- **Vector Graphics & Shapes**: Draw rectangles, circles, ellipses, polygons, and curved paths with Bezier support.
- **Advanced Tables**: Professional table engine with wrapping, colspan, and rich styling.
- **Modular Elements**: Build reports using modular elements like Headers, Text blocks, Dividers, Shapes, and more.
- **Editor Tooling**: Powerful custom inspector with drag-and-drop reordering and interactive table grid.

## Installation

### Via Package Manager (Git URL)

1. Open the **Package Manager** in Unity (Window > Package Manager).
2. Click the **+** button and select **Add package from git URL...**.
3. Enter the repository URL: `https://github.com/DhineshMoorthy-gamedev/PdfGenerator.git`

### Manual Installation

1. Download the repository as a ZIP.
2. Extract the contents into your project's `Packages` folder.

## Quick Start

### Using the PdfReport Component

1. Add the `PdfReport` component to any GameObject.
2. Add a **Page** to the **Pages** list.
3. Within the page, add elements to the **Elements** list.
4. Call `Generate()` via script or use the **Generate Report** context menu item.

```csharp
using UnityProductivityTools.Runtime;

public class MyReportGenerator : MonoBehaviour {
    public PdfReport report;

    void Start() {
        report.AddHeader("Dynamic Report");
        report.AddElement(PdfElement.CreateText("This was added at runtime."));
        report.Generate();
    }
}
```

### Direct PDF Generation

For more control, use the `PdfGenerator` class directly:

```csharp
using UnityProductivityTools.Runtime;

var pdf = new PdfGenerator();
pdf.StartDocument();
pdf.DrawCenteredText("My Custom PDF", 24, true);
pdf.Save("CustomReport.pdf");
```

## Vector Graphics & Shapes

The package now includes comprehensive vector drawing capabilities:

- **Basic Shapes**: Rectangles, Rounded Rectangles, Circles, Ellipses, Lines
- **Complex Shapes**: Polygons (custom vertices), Paths (with Bezier curves)
- **Styling**: Fill colors, stroke colors, opacity, line thickness, dash patterns
- **Line Styling**: Configurable line joins (Miter, Round, Bevel) and caps (Butt, Round, Square)

```csharp
// Create a filled circle
page.elements.Add(new PdfElement { 
    type = PdfElementType.Shape, 
    shapeType = PdfShapeType.Circle, 
    width = 80f,
    height = 80f,
    useFill = true,
    fillColor = Color.red,
    useStroke = true,
    borderColor = Color.black
});

// Create a curved path with Bezier
var pathSegments = new List<PdfPathSegment> {
    new PdfPathSegment { command = PdfPathCommand.MoveTo, p1 = new Vector2(0, 0) },
    new PdfPathSegment { 
        command = PdfPathCommand.CurveTo, 
        p1 = new Vector2(25, 50), p2 = new Vector2(75, 50), p3 = new Vector2(100, 0)
    },
    new PdfPathSegment { command = PdfPathCommand.Close }
};
```

## Advanced Tables

The 1.2 update introduces a professional-grade multi-page engine:
- **Flexible Pages**: Add as many pages as needed, each with its own name and margin overrides.
- **Nested Controls**: Manage elements within pages using a clean, hierarchical ReorderableList.
- **Table Power**: The table editor now features a "Cell Details" mode for alignment and merging, plus fixed column width overrides.
- **Auto-Migration**: Old single-list reports from version 1.0/1.1 are automatically migrated to page 1.

## Documentation

- **[User Manual](Documentation/UserManual.md)** - Comprehensive guide with examples
- **[Sequence Diagram (Architecture)](Documentation/SequenceDiagram.md)** - System architecture overview

For detailed API information, please refer to the source code or the [Wiki](https://github.com/DhineshMoorthy-gamedev/PdfGenerator/wiki).

## Examples

Check out the included demo scripts:
- `ShapesDemo.cs` - Demonstrates all available shape types and styling options
- `TableDemo.cs` - Shows advanced table features

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
