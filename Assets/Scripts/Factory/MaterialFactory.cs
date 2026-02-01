using UnityEngine;

namespace TheTear.Factory
{
    public static class MaterialFactory
    {
        private static bool warned;

        public static Material GetSafeUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                if (!warned)
                {
                    Debug.LogWarning("Unlit/Color shader not found. Falling back to Sprites/Default.");
                    warned = true;
                }
            }

            if (shader == null)
            {
                if (!warned)
                {
                    Debug.LogWarning("No safe unlit shader found. Using default renderer materials.");
                    warned = true;
                }
                return null;
            }

            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
