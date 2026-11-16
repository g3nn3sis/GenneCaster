// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.GameCamera.Modes
{
    internal static class DefaultCamera
    {
        internal static void Run(CameraWrapper camera)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            if (tagger == null || tagger.offlineVRRig == null) return;
            HeadView.Follow(camera, tagger.offlineVRRig);
        }
    }
}
