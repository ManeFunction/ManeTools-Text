using System.Collections.Generic;
using UnityEngine;

namespace Mane.Unity.Text
{
    internal static class ManeTextMaterialsCache
    {
        private const string ShaderName = "Mane Tools/Mane Text - Alpha";

        private static readonly List<Material> Materials = new();
        private static Shader _shader;

        public static Material Find(Font font)
        {
            if (font == null) return null;

            int i = 0;
            while (i < Materials.Count)
            {
                if (Materials[i] == null)
                {
                    Materials.RemoveAt(i);
                    continue;
                }

                if (Materials[i].mainTexture == font.material.mainTexture)
                    return Materials[i];

                i++;
            }

            return null;
        }

        public static Material Create(Font font, bool addToCache)
        {
            if (font == null) return null;

            Shader shader = GetShader();
            if (shader == null)
            {
                Debug.LogError($"Shader '{ShaderName}' was not found. Make sure it is included in the build.");
                return null;
            }

            Material m = new Material(font.material)
            {
                shader = shader,
                hideFlags = HideFlags.DontSave
            };

            if (addToCache)
            {
                Materials.Add(m);
                m.name = font.name;
            }
            else
                m.name = string.Empty;

            return m;
        }

        // Cached materials are named after the font; per-instance ones are not.
        public static bool Contains(Material material) =>
            material != null && !string.IsNullOrEmpty(material.name);

        public static void Destroy(Material material, MeshRenderer renderer)
        {
            if (material == null)
                return;

            if (renderer != null && renderer.sharedMaterial == material)
                renderer.sharedMaterial = null;

#if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);
#else
            Object.Destroy(material);
#endif
        }

        private static Shader GetShader()
        {
            if (_shader == null)
                _shader = Shader.Find(ShaderName);

            return _shader;
        }
    }
}
