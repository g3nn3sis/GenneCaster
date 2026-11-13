// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace GenneCaster.GameCamera
{
    internal static class Smoothing
    {
        internal const float ReferenceFrameRate = 60f;

        internal static float Step(float smoothing, float deltaTime) =>
            smoothing <= 0f ? 1f : 1f - Mathf.Pow(smoothing, deltaTime * ReferenceFrameRate);
    }
}
