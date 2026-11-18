// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera
{
    internal static class Smoothing
    {
        internal const float ReferenceFrameRate = 60f;
        private const float MaximumLagSeconds = 0.325f;

        internal static float Step(float smoothing, float deltaTime) =>
            smoothing <= 0f ? 1f : 1f - Mathf.Pow(smoothing, deltaTime * ReferenceFrameRate);

        internal static float LagStep(float setting, float maximumSetting, float deltaTime)
        {
            float lagSeconds = MaximumLagSeconds * Mathf.Clamp01(setting / maximumSetting);
            return lagSeconds <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / lagSeconds);
        }
    }
}
