// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using UnityEngine;

namespace Zetori.UI
{
    internal static class UiScale
    {
        private const float ReferenceHeight = 1080f;
        private const float MinimumAutoScale = 0.85f;
        private const float MultiplierStep = 0.05f;

        internal static float Multiplier => CasterConfig.UiScale.Value;
        internal static bool CanDecrease => Multiplier > CasterConfig.MinimumUiScale && !Mathf.Approximately(Multiplier, CasterConfig.MinimumUiScale);
        internal static bool CanIncrease => Multiplier < CasterConfig.MaximumUiScale && !Mathf.Approximately(Multiplier, CasterConfig.MaximumUiScale);
        internal static float Factor => Mathf.Clamp(Screen.height / ReferenceHeight, MinimumAutoScale, 4f) * Multiplier;
        internal static float Width => Screen.width / Factor;
        internal static float Height => Screen.height / Factor;

        internal static void Decrease() => StepMultiplier(-1);

        internal static void Increase() => StepMultiplier(1);

        private static void StepMultiplier(int direction)
        {
            float stepped = Mathf.Round((Multiplier + direction * MultiplierStep) / MultiplierStep) * MultiplierStep;
            CasterConfig.UiScale.Value = Mathf.Clamp(stepped, CasterConfig.MinimumUiScale, CasterConfig.MaximumUiScale);
        }

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
