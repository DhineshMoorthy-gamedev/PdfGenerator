using UnityEditor;
using UnityEngine;
using UnityProductivityTools.Runtime;
using System.IO;

namespace UnityProductivityTools.PdfExporter
{
    public class PdfExporterWindow : EditorWindow
    {
        private string userName = "John Doe";
        private string reportTitle = "Sample Project Report";

        [MenuItem("Tools/GameDevTools/PDF Generator", false, 308)]
        public static void ShowWindow()
        {
            GetWindow<PdfExporterWindow>("PDF Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("PDF Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool demonstrates the lightweight PdfGenerator utility. It allows you to generate PDF reports directly from Unity without any external dependencies.", MessageType.Info);
            EditorGUILayout.Space();

            userName = EditorGUILayout.TextField("User Name", userName);
            reportTitle = EditorGUILayout.TextField("Report Title", reportTitle);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Sample PDF", GUILayout.Height(30)))
            {
                GeneratePDF();
            }
        }

        private void GeneratePDF()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "SampleReport.pdf");
            PdfGenerator pdf = new PdfGenerator();
            
            pdf.StartDocument();

            // Header
            pdf.DrawCenteredText(reportTitle.ToUpper(), 18, true);
            pdf.AddVerticalSpace(30);
            pdf.DrawCenteredText("GENERATED FROM UNITY TOOLKIT", 12, false);
            pdf.AddVerticalSpace(20);
            pdf.DrawHorizontalRule();
            pdf.AddVerticalSpace(30);

            // User Info
            pdf.DrawText($"User: {userName}", 50, 12, true);
            pdf.AddVerticalSpace(15);
            pdf.DrawText($"Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}", 50, 10, false);
            pdf.AddVerticalSpace(30);

            // Content Section
            pdf.DrawText("Project Summary", 50, 14, true);
            pdf.AddVerticalSpace(20);
            
            string dummyText = "This is a sample PDF generated using the dependency-free PdfGenerator utility integrated into the Unity Productivity Tools package. This utility provides a simple programmatic way to create documents, reports, and logs directly from within the Unity Editor or at Runtime.";
            
            // Very basic word wrap simulation (just for demo)
            pdf.DrawText(dummyText, 50, 10, false); 
            pdf.AddVerticalSpace(40);

            pdf.DrawHorizontalRule();
            pdf.AddVerticalSpace(20);
            pdf.DrawCenteredText("End of Report", 10, false);

            pdf.Save(path);

            EditorUtility.DisplayDialog("PDF Generated", $"Report saved to:\n{path}", "OK");
            
            // Open the file
            // Open the file with cache busting to force browser refresh
            Application.OpenURL("file://" + path + "?t=" + System.DateTime.Now.Ticks);
        }
    }
}
