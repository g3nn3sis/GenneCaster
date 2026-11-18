// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraMeshPart
    {
        internal Vector3 Pivot { get; }
        internal Mesh Mesh { get; }

        internal VrCameraMeshPart(Vector3 pivot, Mesh mesh)
        {
            Pivot = pivot;
            Mesh = mesh;
        }
    }
}
