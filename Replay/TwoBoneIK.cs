// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace GenneCaster.Replay
{
    internal static class TwoBoneIK
    {
        private const float Epsilon = 0.0001f;

        internal static void Solve(Transform upper, Transform lower, Transform end, Vector3 target)
        {
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float lengthAB = Vector3.Distance(a, b);
            float lengthCB = Vector3.Distance(c, b);
            if (lengthAB < Epsilon || lengthCB < Epsilon) return;
            float lengthAT = Mathf.Clamp(Vector3.Distance(target, a), Epsilon, lengthAB + lengthCB - Epsilon);

            float currentAtA = Angle(c - a, b - a);
            float currentAtB = Angle(a - b, c - b);
            float currentToTarget = Angle(c - a, target - a);
            float desiredAtA = Mathf.Acos(Mathf.Clamp((lengthCB * lengthCB - lengthAB * lengthAB - lengthAT * lengthAT) / (-2f * lengthAB * lengthAT), -1f, 1f));
            float desiredAtB = Mathf.Acos(Mathf.Clamp((lengthAT * lengthAT - lengthAB * lengthAB - lengthCB * lengthCB) / (-2f * lengthAB * lengthCB), -1f, 1f));

            Vector3 bendAxis = Vector3.Cross(c - a, b - a);
            if (bendAxis.sqrMagnitude < Epsilon * Epsilon) return;
            bendAxis.Normalize();
            Vector3 aimAxis = Vector3.Cross(c - a, target - a);

            Quaternion upperGlobal = upper.rotation, lowerGlobal = lower.rotation;
            Quaternion bendUpper = Quaternion.AngleAxis((desiredAtA - currentAtA) * Mathf.Rad2Deg, Quaternion.Inverse(upperGlobal) * bendAxis);
            Quaternion bendLower = Quaternion.AngleAxis((desiredAtB - currentAtB) * Mathf.Rad2Deg, Quaternion.Inverse(lowerGlobal) * bendAxis);
            Quaternion aim = aimAxis.sqrMagnitude < Epsilon * Epsilon
                ? Quaternion.identity
                : Quaternion.AngleAxis(currentToTarget * Mathf.Rad2Deg, Quaternion.Inverse(upperGlobal) * aimAxis.normalized);
            upper.localRotation *= bendUpper * aim;
            lower.localRotation *= bendLower;
        }

        private static float Angle(Vector3 from, Vector3 to) =>
            Mathf.Acos(Mathf.Clamp(Vector3.Dot(from.normalized, to.normalized), -1f, 1f));
    }
}
