// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraBodyTint
    {
        private readonly Texture2D texture;
        private readonly Color32[] bakedPixels;
        private readonly Color32[] tintedPixels;

        internal VrCameraBodyTint(Texture2D texture)
        {
            this.texture = texture;
            bakedPixels = texture.GetPixels32();
            tintedPixels = new Color32[bakedPixels.Length];
        }

        internal void Apply(Color32 accent)
        {
            for (int i = 0; i < bakedPixels.Length; i++) tintedPixels[i] = Recolor(bakedPixels[i], accent);
            texture.SetPixels32(tintedPixels);
            texture.Apply(true);
        }

        private static Color32 Recolor(Color32 baked, Color32 accent)
        {
            int redExcess = baked.r - Mathf.Max(baked.g, baked.b);
            if (redExcess <= 0) return baked;
            float accentWeight = (float)redExcess / baked.r;
            float brightness = baked.r / 255f;
            return new Color32(
                Blend(baked.r, accent.r * brightness, accentWeight),
                Blend(baked.g, accent.g * brightness, accentWeight),
                Blend(baked.b, accent.b * brightness, accentWeight),
                baked.a);
        }

        private static byte Blend(byte baked, float accent, float weight) =>
            (byte)Mathf.RoundToInt(Mathf.Lerp(baked, accent, weight));
    }
}
