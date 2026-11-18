// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraView
    {
        private const float TeleportDistance = 8f;

        private bool placed;
        private Transform? anchor;
        private Vector3 anchoredPosition;
        private Quaternion anchoredRotation = Quaternion.identity;

        internal Vector3 Position { get; private set; }
        internal Quaternion Rotation { get; private set; } = Quaternion.identity;

        internal void Invalidate()
        {
            placed = false;
            anchor = null;
        }

        internal void Step(Pose target, float positionRate, float rotationRate, float scale, float deltaTime, Transform? movingSpace = null)
        {
            if (anchor != null && anchor == movingSpace)
            {
                Position = anchor.TransformPoint(anchoredPosition);
                Rotation = anchor.rotation * anchoredRotation;
            }
            if (!placed || (target.position - Position).sqrMagnitude > Square(TeleportDistance * scale))
            {
                Position = target.position;
                Rotation = target.rotation;
                placed = true;
            }
            else
            {
                Position = Vector3.Lerp(Position, target.position, Blend(positionRate, deltaTime));
                Rotation = Quaternion.Slerp(Rotation, target.rotation, Blend(rotationRate, deltaTime));
            }
            anchor = movingSpace;
            if (anchor == null) return;
            anchoredPosition = anchor.InverseTransformPoint(Position);
            anchoredRotation = Quaternion.Inverse(anchor.rotation) * Rotation;
        }

        internal static float Blend(float rate, float deltaTime) => rate <= 0f ? 1f : 1f - Mathf.Exp(-rate * deltaTime);

        private static float Square(float value) => value * value;
    }
}
