// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraCosmetics
    {
        private const int LocalCosmeticLayers = (int)UnityLayerMask.MirrorOnly;

        private Camera? camera;
        private int originalCullingMask;

        internal void Apply(Camera target, bool firstPerson, bool visible)
        {
            if (!firstPerson)
            {
                Restore();
                return;
            }
            if (camera != target)
            {
                Restore();
                camera = target;
                originalCullingMask = target.cullingMask;
            }
            target.cullingMask = visible
                ? originalCullingMask | LocalCosmeticLayers
                : originalCullingMask & ~LocalCosmeticLayers;
        }

        internal void Restore()
        {
            if (camera != null) camera.cullingMask = originalCullingMask;
            camera = null;
        }
    }
}
