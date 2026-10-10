using UnityEngine;

namespace GenneCaster.Classes.UI
{
    internal static class Textures
    {
        public static Texture2D CreateTexture(
            int width,
            int height,
            Color32 fillColor,
            Color32 borderColor,
            int borderThickness,
            int cornerRadius)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            Color fill = fillColor;
            Color border = borderColor;
            float innerRadius = Mathf.Max(0, cornerRadius - borderThickness);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float distanceToEdge = GetRoundedRectDistance(
                        px,
                        py,
                        width,
                        height,
                        cornerRadius
                    );
                    float outerAlpha = Mathf.Clamp01(
                        0.5f - distanceToEdge
                    );

                    if (outerAlpha <= 0f) continue;

                    float innerDistance = GetRoundedRectDistance(
                        px,
                        py,
                        width - borderThickness * 2,
                        height - borderThickness * 2,
                        innerRadius,
                        borderThickness,
                        borderThickness
                    );

                    float fillAmount = Mathf.Clamp01(
                        0.5f - innerDistance
                    );

                    Color color;

                    if (fillAmount > 0f)
                    {
                        color = Color.Lerp(border, fill, fillAmount);
                    }
                    else
                    {
                        color = border;
                    }

                    color.a *= outerAlpha;

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
        }

        private static float GetRoundedRectDistance(
            float x,
            float y,
            float width,
            float height,
            float radius,
            float offsetX = 0f,
            float offsetY = 0f)
        {
            x -= offsetX;
            y -= offsetY;

            float centerX = width * 0.5f;
            float centerY = height * 0.5f;

            float qx = Mathf.Abs(x - centerX) - (centerX - radius);
            float qy = Mathf.Abs(y - centerY) - (centerY - radius);

            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);

            float inside = Mathf.Min(
                Mathf.Max(qx, qy),
                0f
            );

            return Mathf.Sqrt(
                outsideX * outsideX +
                outsideY * outsideY
            ) + inside - radius;
        }
    }
}
