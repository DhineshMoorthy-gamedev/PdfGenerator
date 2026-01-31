using UnityEngine;
using UnityProductivityTools.Runtime;
using System.Collections.Generic;

namespace UnityProductivityTools.Runtime.Examples
{
    public class ShapesDemo : MonoBehaviour
    {
        [ContextMenu("Generate Shapes Demo")]
        public void Generate()
        {
            PdfReport report = gameObject.GetComponent<PdfReport>();
            if (report == null) report = gameObject.AddComponent<PdfReport>();
            
            report.pages.Clear();
            report.fileName = "Shapes_Demo.pdf";
            
            PdfPage page = new PdfPage("Vector Graphics Showcase");
            
            // 1. Header
            page.elements.Add(PdfElement.CreateHeader("Graphics & Drawing Primitives"));
            page.elements.Add(PdfElement.CreateDivider());

            // 2. Rectangles & Rounded Rectangles
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "1. Rectangles & Rounded Rectangles", isBold = true, fontSize = 14 });
            
            // Solid Rectangle
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Rectangle, 
                height = 50f, // Height
                useFill = true,
                fillColor = new Color(0.2f, 0.6f, 1.0f),
                useStroke = true,
                borderColor = Color.black,
                borderThickness = 2f,
                bottomMargin = 20
            });

            // Rounded Rectangle with transparency
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.RoundedRectangle, 
                height = 60f,
                cornerRadius = 15f,
                useFill = true,
                fillColor = Color.green,
                opacity = 0.5f,
                useStroke = true,
                borderColor = Color.black,
                borderThickness = 1.5f,
                bottomMargin = 20
            });

            // 3. Circles & Ellipses
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "2. Circles & Ellipses", isBold = true, fontSize = 14, topMargin = 10 });
            
            // Circle (using equal margins/height)
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Circle, 
                height = 80f, // Acts as diameter
                width = 80f,
                alignment = PdfAlignment.Center,
                useFill = true,
                fillColor = Color.red,
                useStroke = true,
                borderColor = Color.black,
                bottomMargin = 20
            });

            // Ellipse
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Ellipse, 
                height = 40f,
                width = 150f,
                alignment = PdfAlignment.Center,
                useFill = true,
                fillColor = Color.yellow,
                useStroke = true,
                borderColor = Color.blue,
                borderThickness = 3f,
                lineJoin = PdfLineJoin.Round,
                bottomMargin = 20
            });

            // 4. Styling (Line Joins/Caps)
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "3. Line Styling", isBold = true, fontSize = 14, topMargin = 10 });
            
            // Dashed Line
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Line, 
                borderThickness = 2f,
                borderColor = Color.black,
                dashPatternArray = new float[] { 5, 2 },
                bottomMargin = 10
            });

            // Thick line with Round Caps
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Line, 
                borderThickness = 10f,
                borderColor = Color.gray,
                lineCap = PdfLineCap.Round,
                bottomMargin = 20
            });

            // 5. Polygons
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "4. Polygons", isBold = true, fontSize = 14, topMargin = 10 });
            
            var trianglePoints = new List<Vector2> {
                new Vector2(0, 0),
                new Vector2(50, 50),
                new Vector2(100, 0)
            };

            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Polygon, 
                points = trianglePoints,
                height = 50f, // Height for spacing
                useFill = true,
                fillColor = new Color(1f, 0.5f, 0f),
                useStroke = true,
                borderColor = Color.black,
                lineJoin = PdfLineJoin.Bevel,
                bottomMargin = 60 // Leave space for the polygon which hangs below Y
            });

            // 6. Curved Paths
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "5. Curved Paths (Bezier)", isBold = true, fontSize = 14, topMargin = 70 });
            
            var pathSegments = new List<PdfPathSegment> {
                new PdfPathSegment { command = PdfPathCommand.MoveTo, p1 = new Vector2(0, 0) },
                new PdfPathSegment { 
                    command = PdfPathCommand.CurveTo, 
                    p1 = new Vector2(25, 50), p2 = new Vector2(75, 50), p3 = new Vector2(100, 0)
                },
                new PdfPathSegment { command = PdfPathCommand.LineTo, p1 = new Vector2(100, -20) },
                new PdfPathSegment { command = PdfPathCommand.Close }
            };

            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, 
                shapeType = PdfShapeType.Path, 
                pathSegments = pathSegments,
                height = 50f,
                useFill = true,
                fillColor = new Color(0.5f, 0f, 1f),
                opacity = 0.3f,
                useStroke = true,
                borderColor = Color.magenta,
                borderThickness = 2f,
                bottomMargin = 40
            });

            // 7. Heart Shape
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "6. Complex Path: Heart", isBold = true, fontSize = 14, topMargin = 20 });
            var heartSegments = new List<PdfPathSegment> {
                new PdfPathSegment { command = PdfPathCommand.MoveTo, p1 = new Vector2(50, 0) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(50, 20), p2 = new Vector2(100, 20), p3 = new Vector2(100, -10) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(100, -40), p2 = new Vector2(50, -60), p3 = new Vector2(50, -80) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(50, -60), p2 = new Vector2(0, -40), p3 = new Vector2(0, -10) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(0, 20), p2 = new Vector2(50, 20), p3 = new Vector2(50, 0) },
                new PdfPathSegment { command = PdfPathCommand.Close }
            };

            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, shapeType = PdfShapeType.Path, pathSegments = heartSegments,
                height = 100f, useFill = true, fillColor = Color.red, useStroke = true, borderColor = new Color(0.5f, 0, 0),
                borderThickness = 2f, bottomMargin = 20
            });

            // 8. Wave
            page.elements.Add(new PdfElement { type = PdfElementType.Text, text = "7. Open Path: Wave", isBold = true, fontSize = 14, topMargin = 20 });
            var waveSegments = new List<PdfPathSegment> {
                new PdfPathSegment { command = PdfPathCommand.MoveTo, p1 = new Vector2(0, 0) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(50, 100), p2 = new Vector2(100, -100), p3 = new Vector2(150, 0) },
                new PdfPathSegment { command = PdfPathCommand.CurveTo, p1 = new Vector2(200, 100), p2 = new Vector2(250, -100), p3 = new Vector2(300, 0) }
            };
            page.elements.Add(new PdfElement { 
                type = PdfElementType.Shape, shapeType = PdfShapeType.Path, pathSegments = waveSegments,
                height = 50f, useStroke = true, borderColor = Color.blue, borderThickness = 3f, bottomMargin = 20
            });

            report.pages.Add(page);
            report.Generate();
            
            Debug.Log("[ShapesDemo] PDF generated successfully!");
        }
    }
}

