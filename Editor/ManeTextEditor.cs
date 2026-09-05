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
        private VisualElement _effectsShiftBlock;
        private VisualElement _detailsContainer;
        private VisualElement _emptyFontBox;
        private bool _syncing;

        private const int MinTextRows = 3;
        private const int MaxTextRows = 10;

        /// <summary>Wires font gating, effect toggles, and the text area after UXML clone.</summary>
        protected override void BuildInspector(VisualElement root)
        {
            _detailsContainer = root.Q<VisualElement>("detailsContainer");
            _emptyFontBox = root.Q<VisualElement>("emptyFontBox");
            _effectsShiftBlock = root.Q<VisualElement>("effectsShiftBlock");
            _outlineToggle = SetupEffectBlock(root, "outline", ManeText.TextEffect.Outline);
            _shadowToggle = SetupEffectBlock(root, "shadow", ManeText.TextEffect.Shadow);

            SetupTextArea(root.Q<TextField>("textField"));

            ObjectField fontField = root.Q<ObjectField>("fontField");
            if (fontField != null)
            {
                fontField.objectType = typeof(Font);
                fontField.allowSceneObjects = false;
            }

            SerializedProperty fontProp = serializedObject.FindProperty(ManeText.FontPropertyName);
            SerializedProperty effectProp = serializedObject.FindProperty(ManeText.EffectPropertyName);
            root.TrackPropertyValue(fontProp, _ => UpdateFontGate());
            root.TrackPropertyValue(effectProp, _ => SyncFromSerialized());
            UpdateFontGate();
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
                input.style.overflow = Overflow.Hidden;
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

        private Toggle SetupEffectBlock(VisualElement root, string elementName, ManeText.TextEffect flag)
        {
            VisualElement block = root.Q<VisualElement>(elementName);
            if (block == null)
            {
                Debug.LogError($"VisualElement '{elementName}' not found in root.");
                return null;
            }

            VisualElement contentContainer = block.Q<VisualElement>("contentContainer");
            Toggle isEnableToggle = block.Q<Toggle>("isEnableToggle");
            if (contentContainer == null || isEnableToggle == null)
            {
                Debug.LogError($"Effect block '{elementName}' is missing expected elements.");
                return null;
            }

            UpdateContentVisibility();
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
                UpdateContentVisibility();
                UpdateEffectsShiftVisibility();
            });

            return isEnableToggle;

            void UpdateContentVisibility()
            {
                contentContainer.style.display = isEnableToggle.value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateFontGate()
        {
            serializedObject.UpdateIfRequiredOrScript();
            bool hasFont = serializedObject.FindProperty(ManeText.FontPropertyName).objectReferenceValue != null;

            if (_emptyFontBox != null)
                _emptyFontBox.style.display = hasFont ? DisplayStyle.None : DisplayStyle.Flex;

            if (_detailsContainer != null)
                _detailsContainer.style.display = hasFont ? DisplayStyle.Flex : DisplayStyle.None;
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

            UpdateEffectsShiftVisibility();
        }

        private void UpdateEffectsShiftVisibility()
        {
            if (_effectsShiftBlock == null)
                return;

            bool anyEffect = _outlineToggle is { value: true } ||
                             _shadowToggle is { value: true };
            _effectsShiftBlock.style.display = anyEffect ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle == null)
                return;

            toggle.SetValueWithoutNotify(value);
            VisualElement content = toggle.parent?.Q<VisualElement>("contentContainer");
            if (content != null)
                content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
