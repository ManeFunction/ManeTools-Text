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
    public class ManeTextEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset xml;

        private Toggle _outlineToggle;
        private Toggle _shadowToggle;
        private VisualElement _effectsShiftBlock;
        private VisualElement _detailsContainer;
        private VisualElement _emptyFontBox;
        private bool _syncing;

        /// <summary>Builds the inspector from the assigned UXML tree.</summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            if (xml == null)
            {
                Debug.LogError("ManeTextEditor UXML is not assigned. Set it on the editor script's xml field.");
                return root;
            }

            xml.CloneTree(root);

            _detailsContainer = root.Q<VisualElement>("detailsContainer");
            _emptyFontBox = root.Q<VisualElement>("emptyFontBox");
            _effectsShiftBlock = root.Q<VisualElement>("effectsShiftBlock");
            _outlineToggle = SetupEffectBlock(root, "outline", ManeText.TextEffect.Outline);
            _shadowToggle = SetupEffectBlock(root, "shadow", ManeText.TextEffect.Shadow);

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

            return root;
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
