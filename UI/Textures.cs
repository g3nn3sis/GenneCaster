// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zerori.UI
{
    internal static class Textures
    {
        internal static Texture2D CreateTexture(int width, int height, Color32 fillColor, Color32 borderColor, int borderThickness, int cornerRadius)
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
                    float outerAlpha = Mathf.Clamp01(0.5f - RoundedRectDistance(px, py, width, height, cornerRadius));
                    if (outerAlpha <= 0f) continue;
                    float innerDistance = RoundedRectDistance(px - borderThickness, py - borderThickness, width - borderThickness * 2, height - borderThickness * 2, innerRadius);
                    float fillAmount = Mathf.Clamp01(0.5f - innerDistance);
                    Color color = fillAmount > 0f ? Color.Lerp(border, fill, fillAmount) : border;
                    color.a *= outerAlpha;
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        internal static Texture2D CreateTriangle(int width, int height, Color32 color, bool pointsDown)
        {
            const int samples = 4;
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int covered = 0;
                    for (int sampleY = 0; sampleY < samples; sampleY++)
                    {
                        float v = (y + (sampleY + 0.5f) / samples) / height;
                        float halfWidth = 0.5f * (pointsDown ? v : 1f - v);
                        for (int sampleX = 0; sampleX < samples; sampleX++)
                        {
                            float u = (x + (sampleX + 0.5f) / samples) / width;
                            if (Mathf.Abs(u - 0.5f) <= halfWidth) covered++;
                        }
                    }
                    Color pixel = color;
                    pixel.a *= covered / (float)(samples * samples);
                    pixels[y * width + x] = pixel;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float RoundedRectDistance(float x, float y, float width, float height, float radius)
        {
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float qx = Mathf.Abs(x - centerX) - (centerX - radius);
            float qy = Mathf.Abs(y - centerY) - (centerY - radius);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) + inside - radius;
        }
    }
}
