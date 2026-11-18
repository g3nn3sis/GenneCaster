// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal static class VrCameraShots
    {
        private const float ThirdPersonDistance = 1.6f;
        private const float ThirdPersonHeight = 0.35f;
        private const float FollowerMinimumDistance = 0.9f;
        private const float FollowerMaximumDistance = 1.8f;
        private const float WallProbeRadius = 0.08f;
        private const float WallMargin = 0.02f;
        private const float MinimumFlatLength = 0.0001f;

        internal static Vector3 FlatForward(Quaternion rotation)
        {
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < MinimumFlatLength) forward = Vector3.ProjectOnPlane(rotation * Vector3.up, Vector3.up);
            return forward.sqrMagnitude < MinimumFlatLength ? Vector3.forward : forward.normalized;
        }

        internal static Pose ThirdPerson(Pose head, float scale)
        {
            Vector3 desired = head.position
                - FlatForward(head.rotation) * (ThirdPersonDistance * scale)
                + Vector3.up * (ThirdPersonHeight * scale);
            Vector3 position = ClearPath(head.position, desired, scale);
            return new Pose(position, LookAt(position, head.position, head.rotation));
        }

        internal static Pose Follower(Vector3 current, Pose head, float scale)
        {
            Vector3 offset = current - head.position;
            float distance = Mathf.Clamp(offset.magnitude, FollowerMinimumDistance * scale, FollowerMaximumDistance * scale);
            Vector3 direction = offset.sqrMagnitude < MinimumFlatLength ? -FlatForward(head.rotation) : offset.normalized;
            Vector3 position = ClearPath(head.position, head.position + direction * distance, scale);
            return new Pose(position, LookAt(position, head.position, head.rotation));
        }

        internal static Quaternion LookAt(Vector3 from, Vector3 to, Quaternion fallback)
        {
            Vector3 direction = to - from;
            return direction.sqrMagnitude < MinimumFlatLength ? fallback : Quaternion.LookRotation(direction, Vector3.up);
        }

        private static Vector3 ClearPath(Vector3 from, Vector3 to, float scale)
        {
            Vector3 path = to - from;
            float length = path.magnitude;
            if (length <= Mathf.Epsilon) return to;
            Vector3 direction = path / length;
            bool blocked = Physics.SphereCast(from, WallProbeRadius * scale, direction, out RaycastHit hit, length, GTPlayer.LocomotionEnabledLayers, QueryTriggerInteraction.Ignore);
            return blocked ? from + direction * Mathf.Max(0f, hit.distance - WallMargin * scale) : to;
        }
    }
}
