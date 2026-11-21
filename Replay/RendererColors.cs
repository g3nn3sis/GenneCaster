// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal static class RendererColors
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private static MaterialPropertyBlock? block;

        private static MaterialPropertyBlock Block => block ??= new MaterialPropertyBlock();

        internal static Color Main(Renderer renderer) => Read(renderer, BaseColor, LegacyColor, Color.white);

        internal static Color Emission(Renderer renderer) => Read(renderer, EmissionColor, EmissionColor, Color.black);

        internal static Color MaterialMain(Material? material) =>
            material == null ? Color.white
            : material.HasProperty(BaseColor) ? material.GetColor(BaseColor)
            : material.HasProperty(LegacyColor) ? material.GetColor(LegacyColor)
            : Color.white;

        internal static void Tint(Material material, Color color)
        {
            if (material.HasProperty(BaseColor)) material.SetColor(BaseColor, color);
            if (material.HasProperty(LegacyColor)) material.SetColor(LegacyColor, color);
        }

        internal static void Apply(Renderer renderer, Color main, Color emission)
        {
            MaterialPropertyBlock shared = Block;
            shared.Clear();
            shared.SetColor(BaseColor, main);
            shared.SetColor(LegacyColor, main);
            shared.SetColor(EmissionColor, emission);
            renderer.SetPropertyBlock(shared);
        }

        internal static void Clear(Renderer renderer) => renderer.SetPropertyBlock(null);

        private static Color Read(Renderer renderer, int primary, int secondary, Color fallback)
        {
            if (renderer.HasPropertyBlock())
            {
                MaterialPropertyBlock shared = Block;
                renderer.GetPropertyBlock(shared);
                if (TryRead(shared, primary, secondary, out Color whole)) return whole;
                renderer.GetPropertyBlock(shared, 0);
                if (TryRead(shared, primary, secondary, out Color first)) return first;
            }
            Material? material = renderer.sharedMaterial;
            if (material == null) return fallback;
            if (material.HasProperty(primary)) return material.GetColor(primary);
            if (material.HasProperty(secondary)) return material.GetColor(secondary);
            return fallback;
        }

        private static bool TryRead(MaterialPropertyBlock source, int primary, int secondary, out Color color)
        {
            if (source.HasColor(primary)) color = source.GetColor(primary);
            else if (source.HasColor(secondary)) color = source.GetColor(secondary);
            else
            {
                color = default;
                return false;
            }
            return true;
        }
    }
}
