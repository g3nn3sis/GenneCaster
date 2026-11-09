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
