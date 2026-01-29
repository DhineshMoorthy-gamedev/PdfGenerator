# PDF Generator for Unity

A lightweight, dependency-free PDF generator utility for Unity. This package allows you to generate PDF reports directly from Unity, both at runtime and in the Editor.

## Features

- **Dependency-Free**: No external libraries required.
- **Runtime Support**: Generate PDFs on any platform supported by Unity.
- **Modular Elements**: Build reports using modular elements like Headers, Text blocks, Dividers, and more.
- **Customizable**: Control margins, font sizes, colors, and alignments.
- **Editor Tooling**: Includes a custom inspector and an Editor Window for quick exporting.

## Installation

### Via Package Manager (Git URL)

1. Open the **Package Manager** in Unity (Window > Package Manager).
2. Click the **+** button and select **Add package from git URL...**.
3. Enter the repository URL: `https://github.com/UnityProductivityTools/PdfGenerator.git`

### Manual Installation

1. Download the repository as a ZIP.
2. Extract the contents into your project's `Packages` folder.

## Quick Start

### Using the PdfReport Component

1. Add the `PdfReport` component to any GameObject.
2. Add elements to the **Elements** list in the Inspector.
3. Call `Generate()` via script or use the **Generate Report** context menu item.

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


## Advanced Tables

The 1.1 update introduces a professional-grade table engine:
- **Rich Styling**: Custom border colors, dashed lines, and per-cell background colors.
- **Complex Layouts**: Support for column spanning (`colspan`) and automatic text wrapping.
- **Precision Control**: Fine-tune cell content with per-cell alignment, vertical alignment, and X/Y offsets.
- **Inspector UI**: A powerful new "Advanced Cells" grid for managing complex table data directly in Unity.

## Documentation

For detailed API information, please refer to the source code or the [Wiki](https://github.com/UnityProductivityTools/PdfGenerator/wiki).

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
