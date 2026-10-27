using Mane.Unity.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mane.Unity.Text.Editor
{
    /// <summary>
    /// UI Toolkit inspector for <see cref="ManeText"/>. The outline and shadow blocks are built from the
    /// <see cref="FeatureToggleAttribute"/> fields; the rest of the fields use property binding.
    /// </summary>
    [CustomEditor(typeof(ManeText))]
    public class ManeTextEditor : ManeEditor
    {
        private const int MinTextRows = 3;
        private const int MaxTextRows = 10;

        /// <summary>Wires font gating, effect blocks, and the text area after UXML clone.</summary>
        protected override void BuildInspector(VisualElement root)
        {
            SerializedProperty fontProp = serializedObject.FindProperty(ManeText.FontPropertyName);

            VisualElement emptyFontBox = InfoBoxDrawer.Create("At first, assign a font.", InfoBoxType.Warning);
            emptyFontBox.name = "emptyFontBox";
            root.Insert(0, emptyFontBox);
            emptyFontBox.ShowWhen(() => fontProp.objectReferenceValue == null, fontProp);
            root.Require<VisualElement>("detailsContainer")
                ?.ShowWhen(() => fontProp.objectReferenceValue != null, fontProp);

            SetupEffects(root.Require<VisualElement>("effects"));

            SetupTextArea(root.Require<TextField>("textField"));

            ObjectField fontField = root.Require<ObjectField>("fontField");
            if (fontField != null)
            {
                fontField.objectType = typeof(Font);
                fontField.allowSceneObjects = false;
            }

        }

        // Outline, shadow, and the shift they share, from the first effect switch to the end of the component.
        private void SetupEffects(VisualElement effects)
        {
            if (effects == null)
                return;

            ManeInspectorLayout.Fill(effects, serializedObject, ManeText.OutlinePropertyName);

            SerializedProperty outline = serializedObject.FindProperty(ManeText.OutlinePropertyName);
            SerializedProperty shadow = serializedObject.FindProperty(ManeText.ShadowPropertyName);
            effects.Q(ManeInspectorLayout.FieldName(ManeText.EffectsShiftZPropertyName))?.parent
                ?.ShowWhen(() => outline.boolValue || shadow.boolValue, outline, shadow);
        }

        private static void SetupTextArea(TextField textField)
        {
            if (textField == null)
                return;

            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;
            textField.RegisterCallback<AttachToPanelEvent>(_ => ApplyTextAreaHeight(textField));
            textField.RegisterCallback<GeometryChangedEvent>(_ => ApplyTextAreaHeight(textField));
        }

        private static void ApplyTextAreaHeight(TextField textField)
        {
            if (textField.panel == null)
                return;

            VisualElement input = textField.Q(className: "unity-base-text-field__input")
                                  ?? textField.Q(className: "unity-base-field__input");
            if (input == null)
                return;

            float lineHeight = GetTextLineHeight(textField);
            float extra = input.resolvedStyle.paddingTop + input.resolvedStyle.paddingBottom
                          + input.resolvedStyle.borderTopWidth + input.resolvedStyle.borderBottomWidth;
            float minHeight = lineHeight * MinTextRows + extra;
            float maxHeight = lineHeight * MaxTextRows + extra;

            if (input.style.maxHeight.keyword != StyleKeyword.Undefined
                || !Mathf.Approximately(input.style.maxHeight.value.value, maxHeight))
            {
                input.style.minHeight = minHeight;
                input.style.maxHeight = maxHeight;
            }

            ScrollView scrollView = textField.Q<ScrollView>();
            if (scrollView == null)
                return;

            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        private static float GetTextLineHeight(TextField textField)
        {
            if (textField.panel == null)
                return EditorGUIUtility.singleLineHeight;

            Vector2 size = textField.MeasureTextSize("Ag", 0, VisualElement.MeasureMode.Undefined, 0,
                VisualElement.MeasureMode.Undefined);
            if (size.y > 1f)
                return size.y;

            TextElement textElement = textField.Q<TextElement>();
            if (textElement != null)
            {
                float fontSize = textElement.resolvedStyle.fontSize;
                if (fontSize > 1f)
                    return fontSize;
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
