// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.UI
{
    internal static class UiScale
    {
        private const float ReferenceHeight = 1080f;
        private const float MinimumAutoScale = 0.85f;

        internal static float Factor => Mathf.Clamp(Screen.height / ReferenceHeight, MinimumAutoScale, 4f) * CasterConfig.UiScale.Value;
        internal static float Width => Screen.width / Factor;
        internal static float Height => Screen.height / Factor;

        internal static Matrix4x4 Begin()
        {
            Matrix4x4 previous = GUI.matrix;
            float factor = Factor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(factor, factor, 1f));
            return previous;
        }

        internal static Rect ClampToScreen(Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Width - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Height - rect.height));
            return rect;
        }
    }
}
