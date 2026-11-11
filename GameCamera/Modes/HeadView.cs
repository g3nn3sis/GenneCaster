using GorillaLocomotion;
using UnityEngine;

namespace GenneCaster.GameCamera.Modes
{
    internal static class HeadView
    {
        internal static void Follow(CameraWrapper camera, VRRig target)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            if (tagger != null && target == tagger.offlineVRRig && GTPlayer.Instance != null)
            {
                camera.SetPose(GTPlayer.Instance.headCollider.transform.position, target.headMesh.transform.rotation);
                return;
            }
            Transform body = target.transform;
            Vector3 eyes = body.position + target.headBodyOffset + body.up * 0.09f + body.forward * 0.06f;
            camera.SetPose(eyes, target.headMesh.transform.rotation);
        }
    }
}
