using GenneCaster.Base;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
using UnityEngine;

namespace GenneCaster.GameCamera
{
    internal sealed class CameraManager : ObjectComponent
    {
        private const float RetryInterval = 1f;

        internal static CameraWrapper? camera;
        internal static bool audioListener { get => CasterConfig.AudioListener.Value; set => CasterConfig.AudioListener.Value = value; }
        internal static float fieldOfView { get => CasterConfig.FieldOfView.Value; set => CasterConfig.FieldOfView.Value = value; }
        internal static float nearClipPlane { get => CasterConfig.NearClipPlane.Value; set => CasterConfig.NearClipPlane.Value = value; }

        private float nextAttempt;

        internal void LateUpdate()
        {
            if (!EnsureCamera()) return;
            camera!.Apply(fieldOfView, nearClipPlane, audioListener);
            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return;
            try { SpectatorCamera.Run(camera, target); }
            catch (Exception e) { Log.ExceptionOnce("spectator-camera", e, "Moving the spectator camera failed"); }
        }

        private bool EnsureCamera()
        {
            if (camera != null && camera.IsAlive) return true;
            if (Time.unscaledTime < nextAttempt) return false;
            nextAttempt = Time.unscaledTime + RetryInterval;
            camera = CameraWrapper.TryCreate();
            if (camera == null)
            {
                Log.WarningOnce("camera-missing", "The game's shoulder camera was not found; retrying.");
                return false;
            }
            Log.Forget("camera-missing");
            Log.Info("Spectator camera ready.");
            return true;
        }
    }
}
