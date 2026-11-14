// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;

namespace GenneCaster.GameCamera.Modes
{
    internal static class HeadView
    {
        internal static void Follow(CameraWrapper camera, VRRig target) => Follow(camera, Eyes(target));

        internal static void Follow(CameraWrapper camera, Pose eyes) => camera.SetPose(eyes.position, eyes.rotation);

        internal static Pose Eyes(VRRig target)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            if (tagger != null && target == tagger.offlineVRRig && GTPlayer.Instance != null)
                return new Pose(GTPlayer.Instance.headCollider.transform.position, target.headMesh.transform.rotation);
            Transform body = target.transform;
            Vector3 eyes = body.position + target.headBodyOffset + body.up * 0.09f + body.forward * 0.06f;
            return new Pose(eyes, target.headMesh.transform.rotation);
        }
    }
}
