using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityProductivityTools.Runtime;

namespace UnityProductivityTools.Editor.PdfGenerator
{
    [CustomEditor(typeof(PdfReport))]
    public class PdfReportEditor : UnityEditor.Editor
    {
        private SerializedProperty pagesProp;
        private SerializedProperty fileNameProp;
        private SerializedProperty autoOpenProp;
        private SerializedProperty topMarginProp;
        private SerializedProperty bottomMarginProp;

        private System.Collections.Generic.Dictionary<string, ReorderableList> elementLists = new System.Collections.Generic.Dictionary<string, ReorderableList>();

        private void OnEnable()
        {
            pagesProp = serializedObject.FindProperty("pages");
            fileNameProp = serializedObject.FindProperty("fileName");
            autoOpenProp = serializedObject.FindProperty("autoOpenAfterSave");
            topMarginProp = serializedObject.FindProperty("topMargin");
            bottomMarginProp = serializedObject.FindProperty("bottomMargin");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            GUILayout.Label("PDF Report Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This architecture now supports Pages. You can group your content into different pages.", MessageType.Info);

            EditorGUILayout.PropertyField(fileNameProp);
            EditorGUILayout.PropertyField(autoOpenProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Default Margins", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(topMarginProp, new GUIContent("Top"));
            EditorGUILayout.PropertyField(bottomMarginProp, new GUIContent("Bottom"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawPagesList();

            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("GENERATE REPORT", GUILayout.Height(40)))
            {
                (target as PdfReport).Generate();
            }
            GUI.backgroundColor = Color.white;

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPagesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pages", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Page", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                pagesProp.arraySize++;
                var newPage = pagesProp.GetArrayElementAtIndex(pagesProp.arraySize - 1);
                newPage.FindPropertyRelative("pageName").stringValue = "New Page " + pagesProp.arraySize;
                newPage.FindPropertyRelative("elements").ClearArray();
                newPage.FindPropertyRelative("useOverrides").boolValue = false;
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < pagesProp.arraySize; i++)
            {
                var pageProp = pagesProp.GetArrayElementAtIndex(i);
                var pageName = pageProp.FindPropertyRelative("pageName");
                var useOverrides = pageProp.FindPropertyRelative("useOverrides");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                // Foldout state persistence (simple index-based)
                bool folded = EditorPrefs.GetBool($"PdfPage_{target.GetInstanceID()}_{i}", true);
                bool newFolded = EditorGUILayout.Foldout(folded, pageName.stringValue, true, EditorStyles.foldoutHeader);
                if (newFolded != folded) EditorPrefs.SetBool($"PdfPage_{target.GetInstanceID()}_{i}", newFolded);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Page", "Are you sure you want to delete this page and all its content?", "Yes", "No"))
                    {
                        pagesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (newFolded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(pageName);
                    EditorGUILayout.PropertyField(useOverrides);
                    if (useOverrides.boolValue)
                    {
                        var top = pageProp.FindPropertyRelative("topMargin");
                        var bottom = pageProp.FindPropertyRelative("bottomMargin");
                        var left = pageProp.FindPropertyRelative("leftMargin");
                        var right = pageProp.FindPropertyRelative("rightMargin");

                        float h = EditorGUIUtility.singleLineHeight;
                        float pad = 2f;
                        Rect r = EditorGUILayout.GetControlRect(true, h * 2 + pad);
                        
                        float half = r.width / 2f;
                        float labelW = 50;

                        EditorGUI.LabelField(new Rect(r.x, r.y, labelW, h), "Top");
                        EditorGUI.PropertyField(new Rect(r.x + labelW, r.y, half - labelW - 5, h), top, GUIContent.none);
                        
                        EditorGUI.LabelField(new Rect(r.x + half, r.y, labelW, h), "Bottom");
                        EditorGUI.PropertyField(new Rect(r.x + half + labelW, r.y, half - labelW - 5, h), bottom, GUIContent.none);
                        
                        EditorGUI.LabelField(new Rect(r.x, r.y + h + pad, labelW, h), "Left");
                        EditorGUI.PropertyField(new Rect(r.x + labelW, r.y + h + pad, half - labelW - 5, h), left, GUIContent.none);
                        
                        EditorGUI.LabelField(new Rect(r.x + half, r.y + h + pad, labelW, h), "Right");
                        EditorGUI.PropertyField(new Rect(r.x + half + labelW, r.y + h + pad, half - labelW - 5, h), right, GUIContent.none);
                    }

                    EditorGUILayout.Space();
                    DrawElementsList(pageProp.FindPropertyRelative("elements"), i);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawElementsList(SerializedProperty elementsProp, int pageIndex)
        {
            string listKey = $"{elementsProp.propertyPath}_{pageIndex}";
            if (!elementLists.TryGetValue(listKey, out var list) || list.serializedProperty.serializedObject != serializedObject)
            {
                list = new ReorderableList(serializedObject, elementsProp, true, true, true, true);
                
                list.drawHeaderCallback = (Rect rect) => {
                    EditorGUI.LabelField(rect, "Elements");
                };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                    DrawElementDetails(rect, elementsProp.GetArrayElementAtIndex(index));
                };

                list.elementHeightCallback = (index) => {
                    return GetElementHeight(elementsProp.GetArrayElementAtIndex(index));
                };

                list.onAddCallback = (l) => {
                    int index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    l.index = index;
                    var element = l.serializedProperty.GetArrayElementAtIndex(index);
                    ResetElementToDefaults(element);
                };

                elementLists[listKey] = list;
            }

            list.DoLayoutList();
            
            // Preset buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Header", EditorStyles.miniButtonLeft)) AddPreset(elementsProp, PdfElementType.Text, "Header", 18, true, PdfAlignment.Center);
            if (GUILayout.Button("+ Text", EditorStyles.miniButtonMid)) AddPreset(elementsProp, PdfElementType.Text, "Body Text", 11, false, PdfAlignment.Left);
            if (GUILayout.Button("+ Table", EditorStyles.miniButtonMid)) AddTablePreset(elementsProp);
            if (GUILayout.Button("+ Shape", EditorStyles.miniButtonMid)) AddShapePreset(elementsProp);
            if (GUILayout.Button("+ Divider", EditorStyles.miniButtonRight)) AddPreset(elementsProp, PdfElementType.Divider, "", 11, false, PdfAlignment.Left);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawElementDetails(Rect rect, SerializedProperty element)
        {
            var type = element.FindPropertyRelative("type");
            var text = element.FindPropertyRelative("text");
            var fontSize = element.FindPropertyRelative("fontSize");
            var isBold = element.FindPropertyRelative("isBold");
            var color = element.FindPropertyRelative("color");
            var alignment = element.FindPropertyRelative("alignment");
            var bottomMargin = element.FindPropertyRelative("bottomMargin");
            var topMargin = element.FindPropertyRelative("topMargin");
            var leftMargin = element.FindPropertyRelative("leftMargin");
            var rightMargin = element.FindPropertyRelative("rightMargin");
            var lineHeight = element.FindPropertyRelative("lineHeight");
            var maxWidth = element.FindPropertyRelative("maxWidth");
            var useCustomPosition = element.FindPropertyRelative("useCustomPosition");
            var customX = element.FindPropertyRelative("customX");
            var customY = element.FindPropertyRelative("customY");
            var dividerThickness = element.FindPropertyRelative("dividerThickness");
            var dividerWidth = element.FindPropertyRelative("dividerWidth");
            var width = element.FindPropertyRelative("width");
            var height = element.FindPropertyRelative("height");

            float sh = EditorGUIUtility.singleLineHeight;
            float p = 2f;

            Rect typeRect = new Rect(rect.x, rect.y + p, 100, sh);
            EditorGUI.PropertyField(typeRect, type, GUIContent.none);

            PdfElementType elementType = (PdfElementType)type.enumValueIndex;

            if (elementType == PdfElementType.Text)
            {
                Rect textRect = new Rect(rect.x + 105, rect.y + p, rect.width - 105, sh);
                EditorGUI.PropertyField(textRect, text, GUIContent.none);

                float y = sh + p * 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 35, sh), "Size:");
                EditorGUI.PropertyField(new Rect(rect.x + 35, rect.y + y, 35, sh), fontSize, GUIContent.none);
                isBold.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 75, rect.y + y, 50, sh), "Bold", isBold.boolValue);
                EditorGUI.PropertyField(new Rect(rect.x + 130, rect.y + y, 45, sh), color, GUIContent.none);
                EditorGUI.PropertyField(new Rect(rect.x + 180, rect.y + y, rect.width - 180, sh), alignment, GUIContent.none);

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 75, sh), "L-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 80, rect.y + y, 40, sh), leftMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 130, rect.y + y, 80, sh), "R-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 215, rect.y + y, 40, sh), rightMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 265, rect.y + y, 40, sh), "LH:");
                EditorGUI.PropertyField(new Rect(rect.x + 310, rect.y + y, 35, sh), lineHeight, GUIContent.none);

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 85, sh), "Top-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 90, rect.y + y, 40, sh), topMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 140, rect.y + y, 80, sh), "Bot-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 225, rect.y + y, 40, sh), bottomMargin, GUIContent.none);

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 65, sh), "MaxW:");
                EditorGUI.PropertyField(new Rect(rect.x + 70, rect.y + y, 40, sh), maxWidth, GUIContent.none);
                useCustomPosition.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 120, rect.y + y, 120, sh), "Custom Pos", useCustomPosition.boolValue);
                if (useCustomPosition.boolValue)
                {
                    EditorGUI.LabelField(new Rect(rect.x + 245, rect.y + y, 15, sh), "X");
                    EditorGUI.PropertyField(new Rect(rect.x + 260, rect.y + y, 40, sh), customX, GUIContent.none);
                    EditorGUI.LabelField(new Rect(rect.x + 305, rect.y + y, 15, sh), "Y");
                    EditorGUI.PropertyField(new Rect(rect.x + 320, rect.y + y, 40, sh), customY, GUIContent.none);
                }
            }
            else if (elementType == PdfElementType.Divider)
            {
                float y = sh + p * 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 60, sh), "Thick:");
                EditorGUI.PropertyField(new Rect(rect.x + 65, rect.y + y, 40, sh), dividerThickness, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 115, rect.y + y, 45, sh), "Width:");
                EditorGUI.PropertyField(new Rect(rect.x + 160, rect.y + y, 60, sh), dividerWidth, GUIContent.none);

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 85, sh), "Top-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 90, rect.y + y, 40, sh), topMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 140, rect.y + y, 80, sh), "Bot-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 225, rect.y + y, 40, sh), bottomMargin, GUIContent.none);
            }
            else if (elementType == PdfElementType.VerticalSpace)
            {
                EditorGUI.PropertyField(new Rect(rect.x + 105, rect.y + p, rect.width - 105, sh), text, GUIContent.none);
            }
            else if (elementType == PdfElementType.Shape)
            {
                var shapeType = element.FindPropertyRelative("shapeType");
                var cornerRadius = element.FindPropertyRelative("cornerRadius");
                var useFill = element.FindPropertyRelative("useFill");
                var useStroke = element.FindPropertyRelative("useStroke");
                var fillColor = element.FindPropertyRelative("fillColor");
                var borderColor = element.FindPropertyRelative("borderColor");
                var borderThickness = element.FindPropertyRelative("borderThickness");
                var opacity = element.FindPropertyRelative("opacity");
                var lineJoin = element.FindPropertyRelative("lineJoin");
                var lineCap = element.FindPropertyRelative("lineCap");

                float y = sh + p * 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + y, 120, sh), shapeType, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 125, rect.y + y, 35, sh), "Opac:");
                EditorGUI.PropertyField(new Rect(rect.x + 160, rect.y + y, 35, sh), opacity, GUIContent.none);
                
                PdfShapeType st = (PdfShapeType)shapeType.enumValueIndex;
                
                if (st != PdfShapeType.Line)
                {
                    useFill.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 200, rect.y + y, 40, sh), "Fill", useFill.boolValue);
                    EditorGUI.PropertyField(new Rect(rect.x + 245, rect.y + y, 40, sh), fillColor, GUIContent.none);
                }
                
                useStroke.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 290, rect.y + y, 55, sh), "Stroke", useStroke.boolValue);
                EditorGUI.PropertyField(new Rect(rect.x + 350, rect.y + y, 40, sh), borderColor, GUIContent.none);

                y += sh + p;
                if (st == PdfShapeType.RoundedRectangle)
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 50, sh), "Radius:");
                    EditorGUI.PropertyField(new Rect(rect.x + 55, rect.y + y, 40, sh), cornerRadius, GUIContent.none);
                    
                    EditorGUI.LabelField(new Rect(rect.x + 100, rect.y + y, 40, sh), "Thick:");
                    EditorGUI.PropertyField(new Rect(rect.x + 145, rect.y + y, 35, sh), borderThickness, GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x + 185, rect.y + y, 60, sh), lineJoin, GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x + 250, rect.y + y, 60, sh), lineCap, GUIContent.none);
                }
                else
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 40, sh), "Thick:");
                    EditorGUI.PropertyField(new Rect(rect.x + 45, rect.y + y, 35, sh), borderThickness, GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x + 85, rect.y + y, 60, sh), lineJoin, GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x + 150, rect.y + y, 60, sh), lineCap, GUIContent.none);
                }

                // Points / Segments
                if (st == PdfShapeType.Polygon)
                {
                    y += sh + p;
                    var points = element.FindPropertyRelative("points");
                    float pointsH = EditorGUI.GetPropertyHeight(points, true);
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + y, rect.width, pointsH), points, true); 
                    y += pointsH - sh;
                }
                else if (st == PdfShapeType.Path)
                {
                    y += sh + p;
                    var segments = element.FindPropertyRelative("pathSegments");
                    float segmentsH = EditorGUI.GetPropertyHeight(segments, true);
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + y, rect.width, segmentsH), segments, true); 
                    y += segmentsH - sh;
                }

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 75, sh), "L-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 80, rect.y + y, 40, sh), leftMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 130, rect.y + y, 80, sh), "R-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 215, rect.y + y, 40, sh), rightMargin, GUIContent.none);
                
                y += sh + p;
                if (st == PdfShapeType.Line)
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 55, sh), "Length:");
                    EditorGUI.PropertyField(new Rect(rect.x + 55, rect.y + y, 60, sh), width, GUIContent.none);
                }
                else if (st == PdfShapeType.Circle)
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 60, sh), "Diameter:");
                    EditorGUI.PropertyField(new Rect(rect.x + 65, rect.y + y, 60, sh), width, GUIContent.none);
                    height.floatValue = width.floatValue;
                }
                else if (st == PdfShapeType.Polygon || st == PdfShapeType.Path)
                {
                     EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 95, sh), "Content Height:");
                     EditorGUI.PropertyField(new Rect(rect.x + 100, rect.y + y, 60, sh), height, GUIContent.none);
                }
                else 
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 55, sh), "Width:");
                    EditorGUI.PropertyField(new Rect(rect.x + 55, rect.y + y, 60, sh), width, GUIContent.none);
                    EditorGUI.LabelField(new Rect(rect.x + 125, rect.y + y, 55, sh), "Height:");
                    EditorGUI.PropertyField(new Rect(rect.x + 180, rect.y + y, 60, sh), height, GUIContent.none);
                }

                y += sh + p;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 85, sh), "Top-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 90, rect.y + y, 40, sh), topMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 140, rect.y + y, 80, sh), "Bot-Margin:");
                EditorGUI.PropertyField(new Rect(rect.x + 225, rect.y + y, 40, sh), bottomMargin, GUIContent.none);
            }
            else if (elementType == PdfElementType.Table)
            {
                var tableDataProp = element.FindPropertyRelative("tableData");
                var showBordersProp = element.FindPropertyRelative("showTableBorders");
                var hasHeaderProp = element.FindPropertyRelative("hasTableHeader");
                var borderThicknessProp = element.FindPropertyRelative("borderThickness");
                var borderColorProp = element.FindPropertyRelative("borderColor");
                var borderStyleProp = element.FindPropertyRelative("borderStyle");
                var cellPaddingProp = element.FindPropertyRelative("cellPadding");
                var headerColorProp = element.FindPropertyRelative("tableHeaderColor");

                float y = sh + p * 2;
                
                // Row 1: Style
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 35, sh), "Size:");
                EditorGUI.PropertyField(new Rect(rect.x + 35, rect.y + y, 30, sh), fontSize, GUIContent.none);
                isBold.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 70, rect.y + y, 50, sh), "Bold", isBold.boolValue);
                EditorGUI.PropertyField(new Rect(rect.x + 125, rect.y + y, 40, sh), color, GUIContent.none);
                showBordersProp.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 170, rect.y + y, 65, sh), "Borders", showBordersProp.boolValue);
                hasHeaderProp.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x + 240, rect.y + y, 65, sh), "Header", hasHeaderProp.boolValue);

                y += sh + p;

                // Row 2: Borders Advanced
                if (showBordersProp.boolValue)
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 40, sh), "Thick:");
                    EditorGUI.PropertyField(new Rect(rect.x + 40, rect.y + y, 30, sh), borderThicknessProp, GUIContent.none);
                    EditorGUI.LabelField(new Rect(rect.x + 75, rect.y + y, 40, sh), "Style:");
                    EditorGUI.PropertyField(new Rect(rect.x + 115, rect.y + y, 60, sh), borderStyleProp, GUIContent.none);
                    EditorGUI.LabelField(new Rect(rect.x + 180, rect.y + y, 40, sh), "Color:");
                    EditorGUI.PropertyField(new Rect(rect.x + 225, rect.y + y, 40, sh), borderColorProp, GUIContent.none);
                    y += sh + p;
                }

                // Row 3: Padding & Header Color
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 55, sh), "Padding:");
                EditorGUI.PropertyField(new Rect(rect.x + 55, rect.y + y, 30, sh), cellPaddingProp, GUIContent.none);
                if (hasHeaderProp.boolValue)
                {
                    EditorGUI.LabelField(new Rect(rect.x + 95, rect.y + y, 80, sh), "Header Color:");
                    EditorGUI.PropertyField(new Rect(rect.x + 180, rect.y + y, 40, sh), headerColorProp, GUIContent.none);
                }
                
                y += sh + p;

                // Row 3.5: Column Widths
                var colWidthsProp = element.FindPropertyRelative("columnWidths");
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 65, sh), "Col Widths:");
                if (GUI.Button(new Rect(rect.x + 70, rect.y + y, 20, sh), "+")) colWidthsProp.arraySize++;
                if (GUI.Button(new Rect(rect.x + 90, rect.y + y, 20, sh), "-") && colWidthsProp.arraySize > 0) colWidthsProp.arraySize--;
                
                float cw_x = rect.x + 115;
                for (int i = 0; i < colWidthsProp.arraySize; i++)
                {
                    float w = 30;
                    if (cw_x + w > rect.x + rect.width) break;
                    EditorGUI.PropertyField(new Rect(cw_x, rect.y + y, w, sh), colWidthsProp.GetArrayElementAtIndex(i), GUIContent.none);
                    cw_x += w + 2;
                }
                
                y += sh + p;

                // Row 3.7: Spacing
                EditorGUI.LabelField(new Rect(rect.x, rect.y + y, 70, sh), "Top-Mg:");
                EditorGUI.PropertyField(new Rect(rect.x + 75, rect.y + y, 30, sh), topMargin, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + 115, rect.y + y, 75, sh), "Bot-Mg:");
                EditorGUI.PropertyField(new Rect(rect.x + 195, rect.y + y, 30, sh), bottomMargin, GUIContent.none);
                
                y += sh + p;
                
                // Row 4: Actions
                Rect btnRect = new Rect(rect.x, rect.y + y, 60, sh);
                if (GUI.Button(btnRect, "+ Row", EditorStyles.miniButtonLeft))
                {
                    int colCount = (tableDataProp.arraySize > 0) ? 
                        tableDataProp.GetArrayElementAtIndex(0).FindPropertyRelative("cells").arraySize : 2;
                    
                    tableDataProp.arraySize++;
                    var newRow = tableDataProp.GetArrayElementAtIndex(tableDataProp.arraySize - 1);
                    var newRowCells = newRow.FindPropertyRelative("cells");
                    newRowCells.arraySize = colCount;
                    GUI.changed = true;
                }
                btnRect.x += 60;
                if (GUI.Button(btnRect, "- Row", EditorStyles.miniButtonMid) && tableDataProp.arraySize > 0) 
                {
                    tableDataProp.arraySize--;
                    GUI.changed = true;
                }
                btnRect.x += 60;
                if (GUI.Button(btnRect, "+ Col", EditorStyles.miniButtonMid))
                {
                    if (tableDataProp.arraySize == 0) tableDataProp.arraySize = 1;
                    for (int i = 0; i < tableDataProp.arraySize; i++)
                        tableDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("cells").arraySize++;
                    GUI.changed = true;
                }
                btnRect.x += 60;
                if (GUI.Button(btnRect, "- Col", EditorStyles.miniButtonMid))
                {
                    for (int i = 0; i < tableDataProp.arraySize; i++)
                    {
                        var c = tableDataProp.GetArrayElementAtIndex(i).FindPropertyRelative("cells");
                        if (c.arraySize > 0) c.arraySize--;
                    }
                    GUI.changed = true;
                }
                btnRect.x += 60;
                if (GUI.Button(btnRect, "Clear", EditorStyles.miniButtonRight)) 
                {
                    tableDataProp.arraySize = 0;
                    GUI.changed = true;
                }

                y += sh + p;

                bool showCellDetails = EditorPrefs.GetBool("PdfTable_ShowDetails", false);
                showCellDetails = GUI.Toggle(new Rect(rect.x + rect.width - 100, rect.y + y - sh - p, 100, sh), showCellDetails, "Cell Details", "Button");
                if (showCellDetails != EditorPrefs.GetBool("PdfTable_ShowDetails", false)) EditorPrefs.SetBool("PdfTable_ShowDetails", showCellDetails);

                // Data Grid
                int rows = tableDataProp.arraySize;
                if (rows > 0)
                {
                    int cols = tableDataProp.GetArrayElementAtIndex(0).FindPropertyRelative("cells").arraySize;
                    if (cols > 0)
                    {
                        float colWidth = rect.width / cols;
                        for (int r = 0; r < rows; r++)
                        {
                            var rowCells = tableDataProp.GetArrayElementAtIndex(r).FindPropertyRelative("cells");
                            for (int c = 0; c < cols; c++)
                            {
                                Rect cellRect = new Rect(rect.x + (c * colWidth), rect.y + y, colWidth - 2, sh);
                                var cellProp = rowCells.GetArrayElementAtIndex(c);
                                
                                if (!showCellDetails)
                                {
                                    EditorGUI.PropertyField(cellRect, cellProp.FindPropertyRelative("text"), GUIContent.none);
                                }
                                else
                                {
                                    // Complex cell view: Alignment, V-Align, Colspan
                                    float miniW = (colWidth - 4) / 4f;
                                    EditorGUI.PropertyField(new Rect(cellRect.x, cellRect.y, miniW * 1.5f, sh), cellProp.FindPropertyRelative("alignment"), GUIContent.none);
                                    EditorGUI.PropertyField(new Rect(cellRect.x + miniW * 1.5f + 1, cellRect.y, miniW * 1.5f, sh), cellProp.FindPropertyRelative("verticalAlignment"), GUIContent.none);
                                    EditorGUI.PropertyField(new Rect(cellRect.x + miniW * 3 + 2, cellRect.y, miniW - 2, sh), cellProp.FindPropertyRelative("colspan"), GUIContent.none);
                                }
                            }
                            y += sh + 2;
                        }
                    }
                }
            }
        }

        private float GetElementHeight(SerializedProperty element)
        {
            var type = (PdfElementType)element.FindPropertyRelative("type").enumValueIndex;
            float sh = EditorGUIUtility.singleLineHeight;
            float p = 4f;
            switch (type)
            {
                case PdfElementType.Text: return sh * 5 + p * 10;
                case PdfElementType.Divider: return sh * 3 + p * 6;
                case PdfElementType.Table: 
                    int rows = element.FindPropertyRelative("tableData").arraySize;
                    bool borders = element.FindPropertyRelative("showTableBorders").boolValue;
                    float baseH = sh * 6 + p * 12; // Extra rows for Column Widths and Spacing
                    if (borders) baseH += sh + p;
                    return baseH + (rows * (sh + 2)) + p;
                case PdfElementType.Shape:
                    var shapeType = (PdfShapeType)element.FindPropertyRelative("shapeType").enumValueIndex;
                    float shapeH = sh * 6 + p * 12; 
                    
                    if (shapeType == PdfShapeType.Polygon)
                    {
                         var points = element.FindPropertyRelative("points");
                         shapeH += EditorGUI.GetPropertyHeight(points, true) + p;
                    }
                    else if (shapeType == PdfShapeType.Path)
                    {
                         var segments = element.FindPropertyRelative("pathSegments");
                         shapeH += EditorGUI.GetPropertyHeight(segments, true) + p;
                    }
                    return shapeH;
                default: return sh + p * 4;
            }
        }

        private void ResetElementToDefaults(SerializedProperty element)
        {
            element.FindPropertyRelative("type").enumValueIndex = 0;
            element.FindPropertyRelative("text").stringValue = "";
            element.FindPropertyRelative("fontSize").intValue = 11;
            element.FindPropertyRelative("isBold").boolValue = false;
            element.FindPropertyRelative("color").colorValue = Color.black;
            element.FindPropertyRelative("color").colorValue = Color.black;
            element.FindPropertyRelative("bottomMargin").floatValue = 10f;
            element.FindPropertyRelative("lineHeight").floatValue = 1.2f;
            element.FindPropertyRelative("leftMargin").floatValue = 50f;
            element.FindPropertyRelative("rightMargin").floatValue = 50f;
        }

        private void AddPreset(SerializedProperty elementsProp, PdfElementType type, string text, int fontSize, bool isBold, PdfAlignment alignment)
        {
            elementsProp.arraySize++;
            var el = elementsProp.GetArrayElementAtIndex(elementsProp.arraySize - 1);
            ResetElementToDefaults(el);
            el.FindPropertyRelative("type").enumValueIndex = (int)type;
            el.FindPropertyRelative("text").stringValue = text;
            el.FindPropertyRelative("fontSize").intValue = fontSize;
            el.FindPropertyRelative("isBold").boolValue = isBold;
            el.FindPropertyRelative("alignment").enumValueIndex = (int)alignment;
            serializedObject.ApplyModifiedProperties();
        }

        private void AddTablePreset(SerializedProperty elementsProp)
        {
            elementsProp.arraySize++;
            var element = elementsProp.GetArrayElementAtIndex(elementsProp.arraySize - 1);
            ResetElementToDefaults(element);
            element.FindPropertyRelative("type").enumValueIndex = (int)PdfElementType.Table;
            
            var tableDataProp = element.FindPropertyRelative("tableData");
            tableDataProp.arraySize = 2;
            for (int r = 0; r < 2; r++)
            {
                var cells = tableDataProp.GetArrayElementAtIndex(r).FindPropertyRelative("cells");
                cells.arraySize = 2;
                for (int c = 0; c < 2; c++)
                {
                    cells.GetArrayElementAtIndex(c).FindPropertyRelative("text").stringValue = $"R{r}C{c}";
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddShapePreset(SerializedProperty elementsProp)
        {
            elementsProp.arraySize++;
            var el = elementsProp.GetArrayElementAtIndex(elementsProp.arraySize - 1);
            ResetElementToDefaults(el);
            el.FindPropertyRelative("type").enumValueIndex = (int)PdfElementType.Shape;
            el.FindPropertyRelative("shapeType").enumValueIndex = (int)PdfShapeType.RoundedRectangle;
            el.FindPropertyRelative("cornerRadius").floatValue = 10f;
            el.FindPropertyRelative("useFill").boolValue = true;
            el.FindPropertyRelative("fillColor").colorValue = new Color(0.8f, 0.9f, 1f);
            el.FindPropertyRelative("borderColor").colorValue = new Color(0.2f, 0.4f, 0.6f);
            el.FindPropertyRelative("opacity").floatValue = 0.5f;
            el.FindPropertyRelative("height").floatValue = 40f; // Default height
            el.FindPropertyRelative("width").floatValue = 0f; // Auto width
            serializedObject.ApplyModifiedProperties();
        }
    }
}
