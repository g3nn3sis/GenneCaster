// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraButton
    {
        private const float HalfWidth = 0.03f;
        private const float HalfHeight = 0.02f;
        private const float HalfDepth = 0.025f;
        private const float DepthCenter = -0.005f;
        private const float ReleaseProximity = 1.4f;
        private const float TravelDepth = 0.003f;
        private const float TravelSeconds = 0.12f;

        private readonly Transform cap;
        private readonly Vector3 restPosition;
        private bool armed = true;
        private float depressedUntil;

        internal Renderer Renderer { get; }

        internal VrCameraButton(Transform cap, Renderer renderer)
        {
            this.cap = cap;
            Renderer = renderer;
            restPosition = cap.localPosition;
        }

        internal float Proximity(Vector3 finger)
        {
            Vector3 local = cap.InverseTransformPoint(finger);
            float across = Mathf.Abs(local.x) / HalfWidth;
            float along = Mathf.Abs(local.y) / HalfHeight;
            float depth = Mathf.Abs(local.z - DepthCenter) / HalfDepth;
            return Mathf.Max(across, Mathf.Max(along, depth));
        }

        internal bool Update(float proximity, float now)
        {
            bool pressed = armed && proximity <= 1f;
            if (pressed)
            {
                armed = false;
                depressedUntil = now + TravelSeconds;
            }
            else if (proximity >= ReleaseProximity) armed = true;
            cap.localPosition = restPosition + Vector3.forward * (now < depressedUntil ? TravelDepth : 0f);
            return pressed;
        }
    }
}
