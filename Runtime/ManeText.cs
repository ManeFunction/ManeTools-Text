using System;
using UnityEngine;

namespace Mane.Unity.Text
{
    /// <summary>
    /// Mesh-based 3D text renderer. Glyphs are built from a Unity <see cref="UnityEngine.Font"/>
    /// atlas; outline and shadow are extra quads, not shader effects.
    /// Labels that share font, opaque color, and style reuse one material.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Mane Tools/Components/Mane Text")]
    public class ManeText : MonoBehaviour
    {
        private static readonly int AlphaColor = Shader.PropertyToID("_AlphaColor");

        /// <summary>Horizontal origin of each line relative to this transform.</summary>
        public enum HorizontalAlignment
        {
            Left = 1,
            Center = 0,
            Right = 2,
        }

        /// <summary>Vertical origin of the block relative to this transform.</summary>
        public enum VerticalAlignment
        {
            Top = 1,
            Center = 0,
            Bottom = 2,
        }

        /// <summary>Geometry effects that can be combined with a flags mask.</summary>
        [Flags]
        public enum TextEffect
        {
            None = 0,
            Outline = 1,
            Shadow = 2,
        }

        [Flags]
        private enum DirtyFlags
        {
            None = 0,
            Mesh = 1,
            Material = 2,
        }

        private static readonly DirtyFlags AllDirty = DirtyFlags.Mesh | DirtyFlags.Material;


        [SerializeField, TextArea] private string _text = string.Empty;

        [Space]
        [SerializeField] private Font _font;
        [SerializeField] private int _fontSize = 100;
        [SerializeField] private float _characterSize = .1f;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private HorizontalAlignment _horizontal = HorizontalAlignment.Center;
        [SerializeField] private VerticalAlignment _vertical = VerticalAlignment.Center;
        [SerializeField] private int _spacingX;
        [SerializeField] private int _spacingY;
        [SerializeField] private int _maxWidth;
        [SerializeField] private int _maxHeight;
        [SerializeField] private bool _breakDigits = true;

        [Space]
        [SerializeField] private TextEffect _effect = TextEffect.None;
        [SerializeField] private float _outlineSize;
        [SerializeField] private Color _outlineColor = Color.red;
        [SerializeField] private Vector2 _shadowOffset = new(3, -3);
        [SerializeField] private Color _shadowColor = new(0f, 0f, 0f, .3f);
        [SerializeField] private float _effectsShiftZ = .1f;

        private Mesh _mesh;
        private MeshRenderer _rendererInternal;
        private int _sortingOrder;
        private Vector2 _size = Vector2.zero;
        private DirtyFlags _dirty = AllDirty;

        /// <summary>True when the mesh or material still needs a rebuild.</summary>
        public bool Dirty => _dirty != DirtyFlags.None;

        /// <summary>Font atlas used to generate glyphs. Changing it rebuilds mesh and material.</summary>
        public Font Font
        {
            get => _font;
            set
            {
                if (_font == value) return;

                _font = value;
                SetAllDirty();
            }
        }

        /// <summary>Source string. Changing it rebuilds the mesh on the next update.</summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;

                _text = value;
                SetLayoutDirty();
            }
        }

        /// <summary>Requested point size used when sampling glyph metrics from the font.</summary>
        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                SetLayoutDirty();
            }
        }

        /// <summary>World-space scale applied to glyph quads. Does not affect <see cref="Size"/> or wrap limits.</summary>
        public float CharacterSize
        {
            get => _characterSize;
            set
            {
                if (Math.Abs(_characterSize - value) < float.Epsilon)
                    return;

                _characterSize = value;
                _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Horizontal origin of each line.</summary>
        public HorizontalAlignment Horizontal
        {
            get => _horizontal;
            set
            {
                if (_horizontal == value)
                    return;

                _horizontal = value;
                _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Vertical origin of the text block.</summary>
        public VerticalAlignment Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical == value)
                    return;

                _vertical = value;
                _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Extra advance added between glyphs, in font units.</summary>
        public int SpacingX
        {
            get => _spacingX;
            set
            {
                if (_spacingX == value)
                    return;

                _spacingX = value;
                SetLayoutDirty();
            }
        }

        /// <summary>Extra offset added to line height, in font units.</summary>
        public int SpacingY
        {
            get => _spacingY;
            set
            {
                if (_spacingY == value)
                    return;

                _spacingY = value;
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Maximum line width in font units. 0 means unlimited; negative values produce no mesh.
        /// </summary>
        public int MaxWidth
        {
            get => _maxWidth;
            set
            {
                if (_maxWidth == value)
                    return;

                _maxWidth = value;
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Maximum block height in font units, used to clip wrapped lines.
        /// 0 means unlimited; negative values produce no mesh.
        /// </summary>
        public int MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (_maxHeight == value)
                    return;

                _maxHeight = value;
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// When false, every space is a word boundary.
        /// When true, a space after a digit does not end the current word.
        /// </summary>
        public bool BreakDigits
        {
            get => _breakDigits;
            set
            {
                if (_breakDigits == value)
                    return;

                _breakDigits = value;
                SetLayoutDirty();
            }
        }

        /// <summary>Enabled geometry effects. Outline and shadow can be combined.</summary>
        public TextEffect Effect
        {
            get => _effect;
            set
            {
                if (_effect == value)
                    return;

                _effect = value;
                _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Outline quad offset in font units. Ignored unless outline is enabled.</summary>
        public float OutlineSize
        {
            get => _outlineSize;
            set
            {
                if (Math.Abs(_outlineSize - value) < float.Epsilon) return;

                _outlineSize = value;
                if (IsOutlineActive)
                    _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Vertex color of outline quads. Ignored unless outline is enabled.</summary>
        public Color OutlineColor
        {
            get => _outlineColor;
            set
            {
                if (_outlineColor == value)
                    return;

                _outlineColor = value;
                if (IsOutlineActive)
                    _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Shadow quad offset in font units. Ignored unless shadow is enabled.</summary>
        public Vector2 ShadowOffset
        {
            get => _shadowOffset;
            set
            {
                if (_shadowOffset == value)
                    return;

                _shadowOffset = value;
                if (IsShadowActive)
                    _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>Vertex color of shadow quads. Ignored unless shadow is enabled.</summary>
        public Color ShadowColor
        {
            get => _shadowColor;
            set
            {
                if (_shadowColor == value)
                    return;

                _shadowColor = value;
                if (IsShadowActive)
                    _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>
        /// Z offset for outline quads; shadow uses twice this value.
        /// Ignored when no effects are enabled.
        /// </summary>
        public float EffectsShiftZ
        {
            get => _effectsShiftZ;
            set
            {
                if (Math.Abs(_effectsShiftZ - value) < float.Epsilon)
                    return;

                _effectsShiftZ = value;
                if (!IsNoEffectsActive)
                    _dirty |= DirtyFlags.Mesh;
            }
        }

        /// <summary>
        /// Glyph vertex color. RGB rebuilds the mesh; alpha switches between a shared
        /// opaque material and a per-instance transparent one.
        /// </summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (Math.Abs(_color.a - value.a) > float.Epsilon)
                    _dirty |= DirtyFlags.Material;

                if (Math.Abs(_color.r - value.r) > float.Epsilon ||
                    Math.Abs(_color.g - value.g) > float.Epsilon ||
                    Math.Abs(_color.b - value.b) > float.Epsilon)
                    _dirty |= DirtyFlags.Mesh;

                _color = value;
            }
        }

        private Color SolidColor
        {
            get
            {
                Color c = Color;
                c.a = 1;

                return c;
            }
        }

        /// <summary>Renderer sorting order. Not serialized; the mesh renderer keeps the last applied value.</summary>
        public int SortingOrder
        {
            get => _sortingOrder;
            set
            {
                _sortingOrder = value;
                if (Renderer != null)
                    Renderer.sortingOrder = value;
            }
        }

        /// <summary>
        /// Laid-out size in font units (the same space as <see cref="MaxWidth"/> / <see cref="MaxHeight"/>).
        /// Multiply by <see cref="CharacterSize"/> to get the world-space mesh size.
        /// </summary>
        public Vector2 Size
        {
            get
            {
                if (_size == Vector2.zero)
                {
                    ManeTextInfo info = GetWrappedText();
                    if (info != null)
                        _size = new Vector2(info.MaxLength,
                            CalculateBaseLine() * (info.String.Count - 1) + CalculateLineHeight());
                }

                return _size;
            }
        }


        private MeshRenderer Renderer
        {
            get
            {
                if (_rendererInternal == null)
                    _rendererInternal = GetComponent<MeshRenderer>();

                return _rendererInternal;
            }
        }

        /// <summary>True when outline quads are included in the mesh.</summary>
        public bool IsOutlineActive => (_effect & TextEffect.Outline) != 0;

        /// <summary>True when a shadow quad is included in the mesh.</summary>
        public bool IsShadowActive => (_effect & TextEffect.Shadow) != 0;

        /// <summary>True when neither outline nor shadow is enabled.</summary>
        public bool IsNoEffectsActive => _effect == TextEffect.None;

#if UNITY_EDITOR
        /// <summary>Serialized field name for the font, used by the custom inspector.</summary>
        public const string FontPropertyName = nameof(_font);

        /// <summary>Serialized field name for effects, used by the custom inspector.</summary>
        public const string EffectPropertyName = nameof(_effect);
#endif

        
        private void Awake() => Font.textureRebuilt += OnFontTextureRebuilt;

        private void Update()
        {
            if (_dirty == DirtyFlags.None)
                return;

            UpdateView(true);
        }

#if UNITY_EDITOR
        private void OnValidate() => SetAllDirty();
#endif

        private void OnDestroy()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;

            if (_mesh != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
#else
                Destroy(_mesh);
#endif
            }

            if (!ManeTextMaterialsCache.Contains(Renderer.sharedMaterial))
                ManeTextMaterialsCache.Destroy(Renderer.sharedMaterial, Renderer);
        }

        private void OnFontTextureRebuilt(Font changedFont)
        {
            if (changedFont != _font) return;

            _dirty |= DirtyFlags.Mesh;
            UpdateView(false);
        }

        private void SetLayoutDirty()
        {
            _dirty |= DirtyFlags.Mesh;
            _size = Vector2.zero;
        }

        private void SetAllDirty()
        {
            _dirty = AllDirty;
            _size = Vector2.zero;
        }

        private float CalculateBaseLine()
        {
            int fontSize = _font.fontSize;
            if (fontSize == 0)
                return _spacingY;

            return (float)_font.lineHeight / fontSize * _fontSize + _spacingY;
        }

        private float CalculateLineHeight()
        {
            int fontSize = _font.fontSize;
            if (fontSize == 0)
                return 0f;

            return (float)_font.ascent / fontSize * _fontSize;
        }

        private void UpdateView(bool requestCharacters)
        {
            if (_font == null) return;

            if (requestCharacters)
                _font.RequestCharactersInTexture(_text, _fontSize);

            if ((_dirty & DirtyFlags.Material) != 0)
                UpdateMaterial();

            if ((_dirty & DirtyFlags.Mesh) != 0)
                RebuildMesh();

            _dirty = DirtyFlags.None;
        }

        private void UpdateMaterial()
        {
            Material m = Renderer.sharedMaterial;
            bool cached = ManeTextMaterialsCache.Contains(m);

            // Transparent to opaque || <Null> to opaque
            if (Math.Abs(_color.a - 1f) < float.Epsilon && (m == null || !cached))
            {
                if (m != null && !cached)
                    ManeTextMaterialsCache.Destroy(m, Renderer);

                m = ManeTextMaterialsCache.Find(_font);
                if (m == null)
                    m = ManeTextMaterialsCache.Create(_font, true);

                Renderer.sharedMaterial = m;

                return;
            }

            // Update opaque
            if (Math.Abs(_color.a - 1f) < float.Epsilon && cached)
                return; // do nothing

            // Opaque to transparent || <Null> to transparent
            if (_color.a < 1f && (m == null || cached))
                Renderer.sharedMaterial = ManeTextMaterialsCache.Create(_font, false);

            // Update transparent
            if (Renderer.sharedMaterial != null)
                Renderer.sharedMaterial.SetFloat(AlphaColor, _color.a);
        }

        private void RebuildMesh()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.MarkDynamic();
                _mesh.hideFlags = HideFlags.DontSave;
                GetComponent<MeshFilter>().mesh = _mesh;
            }

            UpdateMaterial();

            ManeTextInfo info = GetWrappedText();

            Vector3[] vertices;
            int[] triangles;
            Color[] colors;
            Vector2[] uv;

            float du = 0f;
            float dv = 0f;
            if (info != null)
            {
                int vertexMultiplier = 1;
                if (IsShadowActive)
                    vertexMultiplier += 1;
                if (IsOutlineActive)
                    vertexMultiplier += 8;

                int length = info.TotalCount;
                vertices = new Vector3[length * 4 * vertexMultiplier];
                triangles = new int[length * 6 * vertexMultiplier];
                colors = new Color[vertices.Length];
                uv = new Vector2[vertices.Length];

                Texture texture = Renderer.sharedMaterial != null ? Renderer.sharedMaterial.mainTexture : null;
                if (texture != null)
                {
                    du = .5f / texture.width;
                    dv = .5f / texture.height;
                }

                float baseLine = CalculateBaseLine();
                float lineHeight = CalculateLineHeight();

                Vector3 pos = Vector3.zero;
                switch (_vertical)
                {
                    case VerticalAlignment.Top:
                        pos = new Vector3(0, (info.String.Count - 1) * baseLine, 0);
                        break;

                    case VerticalAlignment.Center:
                        pos = new Vector3(0, -lineHeight * .5f + (info.String.Count - 1) * baseLine * .5f, 0);
                        break;

                    case VerticalAlignment.Bottom:
                        pos = new Vector3(0, -lineHeight, 0);
                        break;
                }

                for (int s = 0, i = 0; s < info.String.Count; s++)
                {
                    float offset = 0;
                    if (_horizontal == HorizontalAlignment.Right)
                        offset = -info.Length[s];
                    else if (_horizontal == HorizontalAlignment.Center)
                        offset = info.Length[s] * -.5f;

                    for (int ch = 0; ch < info.String[s].Length; ch++)
                    {
                        _font.GetCharacterInfo(info.String[s][ch], out CharacterInfo chi, _fontSize);

                        if (info.String[s][ch] != ' ')
                        {
                            if (IsOutlineActive)
                            {
                                int o = Effect == TextEffect.Outline ? 0 : 1;
                                CreateRect(chi, length * o++ + i, pos + new Vector3(_outlineSize, 0, _effectsShiftZ),
                                    offset, _outlineColor);
                                CreateRect(chi, length * o++ + i, pos + new Vector3(0, _outlineSize, _effectsShiftZ),
                                    offset, _outlineColor);
                                CreateRect(chi, length * o++ + i, pos + new Vector3(-_outlineSize, 0, _effectsShiftZ),
                                    offset, _outlineColor);
                                CreateRect(chi, length * o++ + i, pos + new Vector3(0, -_outlineSize, _effectsShiftZ),
                                    offset, _outlineColor);
                                CreateRect(chi, length * o++ + i,
                                    pos + new Vector3(_outlineSize, _outlineSize, _effectsShiftZ), offset,
                                    _outlineColor);
                                CreateRect(chi, length * o++ + i,
                                    pos + new Vector3(_outlineSize, -_outlineSize, _effectsShiftZ), offset,
                                    _outlineColor);
                                CreateRect(chi, length * o++ + i,
                                    pos + new Vector3(-_outlineSize, _outlineSize, _effectsShiftZ), offset,
                                    _outlineColor);
                                CreateRect(chi, length * o + i,
                                    pos + new Vector3(-_outlineSize, -_outlineSize, _effectsShiftZ), offset,
                                    _outlineColor);
                            }

                            if (IsShadowActive)
                                CreateRect(chi, i,
                                    pos + new Vector3(_shadowOffset.x, _shadowOffset.y, _effectsShiftZ * 2f), offset,
                                    _shadowColor);

                            CreateRect(chi, length * (vertexMultiplier - 1) + i++, pos, offset, SolidColor);
                        }

                        pos += new Vector3(chi.advance + _spacingX, 0, 0);
                    }

                    pos = new Vector3(0, pos.y - baseLine, 0);
                }

                _size = new Vector2(info.MaxLength, baseLine * (info.String.Count - 1) + lineHeight);

                _mesh.Clear();
                _mesh.vertices = vertices;
                _mesh.triangles = triangles;
                _mesh.colors = colors;
                _mesh.uv = uv;
            }
            else
            {
                _size = Vector2.zero;

                _mesh.Clear();
            }


            void CreateRect(CharacterInfo chi, int i, Vector3 glyphPos, float offset, Color color)
            {
                glyphPos *= _characterSize;

                vertices[4 * i + 0] = glyphPos + new Vector3(chi.minX + offset - .5f, chi.maxY + .5f, 0) * _characterSize;
                vertices[4 * i + 1] = glyphPos + new Vector3(chi.maxX + offset + .5f, chi.maxY + .5f, 0) * _characterSize;
                vertices[4 * i + 2] = glyphPos + new Vector3(chi.maxX + offset + .5f, chi.minY - .5f, 0) * _characterSize;
                vertices[4 * i + 3] = glyphPos + new Vector3(chi.minX + offset - .5f, chi.minY - .5f, 0) * _characterSize;

                colors[4 * i + 0] = color;
                colors[4 * i + 1] = color;
                colors[4 * i + 2] = color;
                colors[4 * i + 3] = color;

                if (chi.uvTopLeft.x > chi.uvBottomRight.x || chi.uvTopLeft.y > chi.uvBottomRight.y)
                {
                    uv[4 * i + 0] = chi.uvTopLeft + new Vector2(du, dv);
                    uv[4 * i + 2] = chi.uvBottomRight + new Vector2(-du, -dv);
                }
                else
                {
                    uv[4 * i + 0] = chi.uvTopLeft + new Vector2(-du, -dv);
                    uv[4 * i + 2] = chi.uvBottomRight + new Vector2(du, dv);
                }

                uv[4 * i + 1] = chi.uvTopRight + new Vector2(du, -dv);
                uv[4 * i + 3] = chi.uvBottomLeft + new Vector2(-du, dv);

                triangles[6 * i + 0] = 4 * i + 0;
                triangles[6 * i + 1] = 4 * i + 1;
                triangles[6 * i + 2] = 4 * i + 2;

                triangles[6 * i + 3] = 4 * i + 0;
                triangles[6 * i + 4] = 4 * i + 2;
                triangles[6 * i + 5] = 4 * i + 3;
            }
        }

        private ManeTextInfo GetWrappedText()
        {
            if (_font == null || _maxWidth < 0 || _maxHeight < 0 || string.IsNullOrEmpty(_text))
                return null;

            _font.RequestCharactersInTexture(_text, _fontSize);

            int maxLines = int.MaxValue;
            if (_maxHeight > 0)
            {
                int line = (int)CalculateBaseLine();
                if (line > 0)
                    maxLines = _maxHeight / line;
            }

            return ManeTextLayout.Wrap(_text, _maxWidth, maxLines, _breakDigits, GetGlyphWidth);
        }

        private float GetGlyphWidth(char ch)
        {
            _font.GetCharacterInfo(ch, out CharacterInfo info, _fontSize);
            return _spacingX + info.advance;
        }
    }
}
