using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityProductivityTools.Runtime;

namespace UnityProductivityTools.Editor.PdfGenerator
{
    [CustomEditor(typeof(PdfReport))]
    public class PdfReportEditor : UnityEditor.Editor
    {
        private ReorderableList reorderableList;
        private SerializedProperty elementsProp;
        private SerializedProperty fileNameProp;
        private SerializedProperty autoOpenProp;
        private SerializedProperty topMarginProp;
        private SerializedProperty bottomMarginProp;
        //private SerializedProperty leftMarginProp;
        //private SerializedProperty rightMarginProp;

        private void OnEnable()
        {
            elementsProp = serializedObject.FindProperty("elements");
            fileNameProp = serializedObject.FindProperty("fileName");
            autoOpenProp = serializedObject.FindProperty("autoOpenAfterSave");
            topMarginProp = serializedObject.FindProperty("topMargin");
            bottomMarginProp = serializedObject.FindProperty("bottomMargin");
            //leftMarginProp = serializedObject.FindProperty("leftMargin");
            //rightMarginProp = serializedObject.FindProperty("rightMargin");

            reorderableList = new ReorderableList(serializedObject, elementsProp, true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Report Content Elements");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = elementsProp.GetArrayElementAtIndex(index);
                var type = element.FindPropertyRelative("type");
                var text = element.FindPropertyRelative("text");
                var fontSize = element.FindPropertyRelative("fontSize");
                var isBold = element.FindPropertyRelative("isBold");
                var color = element.FindPropertyRelative("color");
                var alignment = element.FindPropertyRelative("alignment");
                var spacing = element.FindPropertyRelative("spacingAfter");
                var spacingBefore = element.FindPropertyRelative("spacingBefore");
                var leftMargin = element.FindPropertyRelative("leftMargin");
                var rightMargin = element.FindPropertyRelative("rightMargin");
                var lineHeight = element.FindPropertyRelative("lineHeight");
                var maxWidth = element.FindPropertyRelative("maxWidth");
                var useCustomPosition = element.FindPropertyRelative("useCustomPosition");
                var customX = element.FindPropertyRelative("customX");
                var customY = element.FindPropertyRelative("customY");
                var dividerThickness = element.FindPropertyRelative("dividerThickness");
                var dividerWidth = element.FindPropertyRelative("dividerWidth");

                float singleLineHeight = EditorGUIUtility.singleLineHeight;
                float padding = 2f;

                // First row: Type and Main Text (hide text field for Divider and VerticalSpace)
                Rect typeRect = new Rect(rect.x, rect.y + padding, 100, singleLineHeight);
                EditorGUI.PropertyField(typeRect, type, GUIContent.none);

                if (type.enumValueIndex != (int)PdfElementType.Divider)
                {
                    Rect textRect = new Rect(rect.x + 105, rect.y + padding, rect.width - 105, singleLineHeight);
                    EditorGUI.PropertyField(textRect, text, new GUIContent(GetLabelForType((PdfElementType)type.enumValueIndex)));
                }

                // Row 2 to 5: Styling, Margins, Spacing, and Advanced (Headers, SubHeaders, BodyText only)
                if (type.enumValueIndex != (int)PdfElementType.Divider && 
                    type.enumValueIndex != (int)PdfElementType.VerticalSpace )
                    //type.enumValueIndex != (int)PdfElementType.LabelValue)
                {
                    // Styling row (Row 2)
                    float yOffset = singleLineHeight + padding * 2;
                    
                    Rect sizeLabelRect = new Rect(rect.x, rect.y + yOffset, 35, singleLineHeight);
                    EditorGUI.LabelField(sizeLabelRect, "Size:");
                    Rect sizeRect = new Rect(rect.x + 35, rect.y + yOffset, 35, singleLineHeight);
                    EditorGUI.PropertyField(sizeRect, fontSize, GUIContent.none);

                    Rect boldRect = new Rect(rect.x + 75, rect.y + yOffset, 50, singleLineHeight);
                    isBold.boolValue = EditorGUI.ToggleLeft(boldRect, "Bold", isBold.boolValue);

                    Rect colorRect = new Rect(rect.x + 130, rect.y + yOffset, 45, singleLineHeight);
                    EditorGUI.PropertyField(colorRect, color, GUIContent.none);

                    Rect alignRect = new Rect(rect.x + 180, rect.y + yOffset, rect.width - 180, singleLineHeight);
                    EditorGUI.PropertyField(alignRect, alignment, GUIContent.none);
                    
                    // Row 3: Margins & Line Height
                    yOffset += singleLineHeight + padding;
                    
                    Rect lMarginLabelRect = new Rect(rect.x, rect.y + yOffset, 75, singleLineHeight);
                    EditorGUI.LabelField(lMarginLabelRect, "Left Margin:");
                    Rect lMarginRect = new Rect(rect.x + 80, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(lMarginRect, leftMargin, GUIContent.none);
                    
                    Rect rMarginLabelRect = new Rect(rect.x + 130, rect.y + yOffset, 80, singleLineHeight);
                    EditorGUI.LabelField(rMarginLabelRect, "Right Margin:");
                    Rect rMarginRect = new Rect(rect.x + 215, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(rMarginRect, rightMargin, GUIContent.none);

                    Rect lhLabelRect = new Rect(rect.x + 265, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.LabelField(lhLabelRect, "Line H:");
                    Rect lhRect = new Rect(rect.x + 310, rect.y + yOffset, 35, singleLineHeight);
                    EditorGUI.PropertyField(lhRect, lineHeight, GUIContent.none);
                    
                    // Row 4: Spacing
                    yOffset += singleLineHeight + padding;

                    Rect spBeforeLabelRect = new Rect(rect.x, rect.y + yOffset, 85, singleLineHeight);
                    EditorGUI.LabelField(spBeforeLabelRect, "Space Before:");
                    Rect spBeforeRect = new Rect(rect.x + 90, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(spBeforeRect, spacingBefore, GUIContent.none);
                    
                    Rect spAfterLabelRect = new Rect(rect.x + 140, rect.y + yOffset, 80, singleLineHeight);
                    EditorGUI.LabelField(spAfterLabelRect, "Space After:");
                    Rect spAfterRect = new Rect(rect.x + 225, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(spAfterRect, spacing, GUIContent.none);

                    // Row 5: Max Width & Custom Position
                    yOffset += singleLineHeight + padding;

                    Rect mwLabelRect = new Rect(rect.x, rect.y + yOffset, 65, singleLineHeight);
                    EditorGUI.LabelField(mwLabelRect, "Max Width:");
                    Rect mwRect = new Rect(rect.x + 70, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(mwRect, maxWidth, GUIContent.none);
                    
                    Rect cpRect = new Rect(rect.x + 120, rect.y + yOffset, 120, singleLineHeight);
                    useCustomPosition.boolValue = EditorGUI.ToggleLeft(cpRect, "Custom Position", useCustomPosition.boolValue);
                    
                    if (useCustomPosition.boolValue)
                    {
                        Rect cxLabelRect = new Rect(rect.x + 245, rect.y + yOffset, 15, singleLineHeight);
                        EditorGUI.LabelField(cxLabelRect, "X");
                        Rect cxRect = new Rect(rect.x + 260, rect.y + yOffset, 40, singleLineHeight);
                        EditorGUI.PropertyField(cxRect, customX, GUIContent.none);
                        
                        Rect cyLabelRect = new Rect(rect.x + 305, rect.y + yOffset, 15, singleLineHeight);
                        EditorGUI.LabelField(cyLabelRect, "Y");
                        Rect cyRect = new Rect(rect.x + 320, rect.y + yOffset, 40, singleLineHeight);
                        EditorGUI.PropertyField(cyRect, customY, GUIContent.none);
                    }
                }
                else if (type.enumValueIndex == (int)PdfElementType.Divider)
                {
                    float yOffset = singleLineHeight + padding * 2;
                    
                    // Divider-specific options
                    Rect thickLabelRect = new Rect(rect.x, rect.y + yOffset, 60, singleLineHeight);
                    EditorGUI.LabelField(thickLabelRect, "Thickness:");
                    Rect thickRect = new Rect(rect.x + 65, rect.y + yOffset, 40, singleLineHeight);
                    EditorGUI.PropertyField(thickRect, dividerThickness, GUIContent.none);
                    
                    Rect widthLabelRect = new Rect(rect.x + 115, rect.y + yOffset, 45, singleLineHeight);
                    EditorGUI.LabelField(widthLabelRect, "Width:");
                    Rect widthRect = new Rect(rect.x + 160, rect.y + yOffset, 60, singleLineHeight);
                    EditorGUI.PropertyField(widthRect, dividerWidth, GUIContent.none);
                }
            };

            reorderableList.elementHeightCallback = (index) => {
                var element = elementsProp.GetArrayElementAtIndex(index);
                var type = element.FindPropertyRelative("type");
                var useCustomPosition = element.FindPropertyRelative("useCustomPosition");
                
                float singleLineHeight = EditorGUIUtility.singleLineHeight;
                float padding = 4f; // Increased padding for better spacing
                
                // Divider: 2 rows (type + divider options)
                if (type.enumValueIndex == (int)PdfElementType.Divider)
                {
                    return (singleLineHeight * 2) + (padding * 6);
                }
                // VerticalSpace: 1 row
                else if (type.enumValueIndex == (int)PdfElementType.VerticalSpace)
                {
                    return singleLineHeight + padding * 4;
                }
                //// LabelValue: 2 rows
                //else if (type.enumValueIndex == (int)PdfElementType.LabelValue)
                //{
                //    return (singleLineHeight * 2) + (padding * 6);
                //}
                // Others (Header, SubHeader, BodyText): 5 rows
                else
                {
                    return (singleLineHeight * 5) + (padding * 12);
                }
            };

            reorderableList.onAddCallback = (list) => {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;
                
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                // Reset to sensible defaults for new elements
                element.FindPropertyRelative("type").enumValueIndex = (int)PdfElementType.Text;
                element.FindPropertyRelative("text").stringValue = "New Element";
                element.FindPropertyRelative("fontSize").intValue = 11;
                element.FindPropertyRelative("isBold").boolValue = false;
                element.FindPropertyRelative("useCustomPosition").boolValue = false;
                element.FindPropertyRelative("spacingAfter").floatValue = 10f;
                element.FindPropertyRelative("lineHeight").floatValue = 1.2f;
                element.FindPropertyRelative("leftMargin").floatValue = 50f;
                element.FindPropertyRelative("rightMargin").floatValue = 50f;
                element.FindPropertyRelative("color").colorValue = Color.black;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            GUILayout.Label("PDF Report Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Build your report layout by adding and reordering elements below. Each element can be fully customized.", MessageType.Info);

            EditorGUILayout.PropertyField(fileNameProp);
            EditorGUILayout.PropertyField(autoOpenProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Page Margins", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(topMarginProp, new GUIContent("Top"));
            EditorGUILayout.PropertyField(bottomMarginProp, new GUIContent("Bottom"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.PropertyField(leftMarginProp, new GUIContent("Left"));
            //EditorGUILayout.PropertyField(rightMarginProp, new GUIContent("Right"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            reorderableList.DoLayoutList();

            EditorGUILayout.Space();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Header", EditorStyles.miniButtonLeft)) AddPreset(PdfElementType.Text, "New Header", 18, true, PdfAlignment.Center);
            if (GUILayout.Button("Add Body Text", EditorStyles.miniButtonMid)) AddPreset(PdfElementType.Text, "Enter description...", 10, false, PdfAlignment.Left);
            if (GUILayout.Button("Add Divider", EditorStyles.miniButtonRight)) AddPreset(PdfElementType.Divider, "", 10, false, PdfAlignment.Left);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("GENERATE REPORT", GUILayout.Height(40)))
            {
                (target as PdfReport).Generate();
            }
            GUI.backgroundColor = Color.white;

            serializedObject.ApplyModifiedProperties();
        }

        private string GetLabelForType(PdfElementType type)
        {
            switch (type)
            {
                //case PdfElementType.LabelValue: return "Label";
                case PdfElementType.VerticalSpace: return "Amount";
                case PdfElementType.Divider: return "---";
                default: return "Text";
            }
        }

        private void AddPreset(PdfElementType type, string text, int fontSize, bool isBold, PdfAlignment alignment)
        {
            elementsProp.arraySize++;
            var element = elementsProp.GetArrayElementAtIndex(elementsProp.arraySize - 1);
            element.FindPropertyRelative("type").enumValueIndex = (int)type;
            element.FindPropertyRelative("text").stringValue = text;
            element.FindPropertyRelative("fontSize").intValue = fontSize;
            element.FindPropertyRelative("isBold").boolValue = isBold;
            element.FindPropertyRelative("alignment").enumValueIndex = (int)alignment;
            element.FindPropertyRelative("spacingAfter").floatValue = (fontSize >= 16) ? 20f : 10f;
            
            //if (type == PdfElementType.LabelValue)
            //    element.FindPropertyRelative("value").stringValue = "Value";

            serializedObject.ApplyModifiedProperties();
        }
    }
}
