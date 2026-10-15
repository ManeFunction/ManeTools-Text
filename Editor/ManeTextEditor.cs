using Mane.Unity.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Mane.Unity.Text.Editor
{
    /// <summary>
    /// UI Toolkit inspector for <see cref="ManeText"/>. Effect toggles write the
    /// serialized flags mask; the rest of the fields use property binding.
    /// </summary>
    [CustomEditor(typeof(ManeText))]
    public class ManeTextEditor : ManeEditor
    {
        private Toggle _outlineToggle;
        private Toggle _shadowToggle;
        private bool _syncing;

        private const int MinTextRows = 3;
        private const int MaxTextRows = 10;

        /// <summary>Wires font gating, effect toggles, and the text area after UXML clone.</summary>
        protected override void BuildInspector(VisualElement root)
        {
            SerializedProperty fontProp = serializedObject.FindProperty(ManeText.FontPropertyName);
            SerializedProperty effectProp = serializedObject.FindProperty(ManeText.EffectPropertyName);

            VisualElement emptyFontBox = InfoBoxDrawer.Create("At first, assign a font.", InfoBoxType.Warning);
            emptyFontBox.name = "emptyFontBox";
            root.Insert(0, emptyFontBox);
            emptyFontBox.ShowWhen(() => fontProp.objectReferenceValue == null, fontProp);
            root.Require<VisualElement>("detailsContainer")
                ?.ShowWhen(() => fontProp.objectReferenceValue != null, fontProp);

            const int anyEffect = (int)(ManeText.TextEffect.Outline | ManeText.TextEffect.Shadow);
            root.Require<VisualElement>("effectsShiftBlock")
                ?.ShowWhen(() => (effectProp.intValue & anyEffect) != 0, effectProp);

            _outlineToggle = SetupEffectToggle(root, "outline", ManeText.TextEffect.Outline);
            _shadowToggle = SetupEffectToggle(root, "shadow", ManeText.TextEffect.Shadow);

            SetupTextArea(root.Require<TextField>("textField"));

            ObjectField fontField = root.Require<ObjectField>("fontField");
            if (fontField != null)
            {
                fontField.objectType = typeof(Font);
                fontField.allowSceneObjects = false;
            }

            root.TrackPropertyValue(effectProp, _ => SyncFromSerialized());
            SyncFromSerialized();
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

        private Toggle SetupEffectToggle(VisualElement root, string blockName, ManeText.TextEffect flag)
        {
            Toggle isEnableToggle = root.Require<VisualElement>(blockName)?.Require<Toggle>("isEnableToggle");
            if (isEnableToggle == null)
                return null;

            isEnableToggle.RegisterValueChangedCallback(evt =>
            {
                if (_syncing)
                    return;

                serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty effect = serializedObject.FindProperty(ManeText.EffectPropertyName);
                int value = effect.intValue;
                if (evt.newValue)
                    value |= (int)flag;
                else
                    value &= ~(int)flag;

                effect.intValue = value;
                serializedObject.ApplyModifiedProperties();
            });

            return isEnableToggle;
        }

        private void SyncFromSerialized()
        {
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty effect = serializedObject.FindProperty(ManeText.EffectPropertyName);
            int value = effect.intValue;

            _syncing = true;
            SetToggle(_outlineToggle, (value & (int)ManeText.TextEffect.Outline) != 0);
            SetToggle(_shadowToggle, (value & (int)ManeText.TextEffect.Shadow) != 0);
            _syncing = false;
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle == null)
                return;

            toggle.SetValueWithoutNotify(value);
        }
    }
}
