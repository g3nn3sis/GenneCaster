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
        internal static CameraMode mode { get => CasterConfig.ActiveCameraMode.Value; set => CasterConfig.ActiveCameraMode.Value = value; }

        private float nextAttempt;
        private CameraMode? runningMode;

        internal void LateUpdate()
        {
            if (mode == CameraMode.Freecam && GorillaController.wasd) GorillaController.wasd = false;
            if (!EnsureCamera()) return;
            camera!.Apply(fieldOfView, nearClipPlane, audioListener);
            SwitchMode(camera);
            switch (runningMode)
            {
                case CameraMode.Default: RunDefault(camera); break;
                case CameraMode.Freecam: RunFreecam(camera); break;
                default: RunSpectator(camera); break;
            }
        }

        private void SwitchMode(CameraWrapper camera)
        {
            CameraMode next = mode;
            if (runningMode == next) return;
            if (runningMode == CameraMode.Freecam) FreecamCamera.Exit();
            if (next == CameraMode.Freecam) FreecamCamera.Enter(camera);
            runningMode = next;
        }

        private static void RunDefault(CameraWrapper camera)
        {
            try { DefaultCamera.Run(camera); }
            catch (Exception e) { Log.ExceptionOnce("default-camera", e, "Moving the default camera failed"); }
        }

        private static void RunSpectator(CameraWrapper camera)
        {
            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return;
            try { SpectatorCamera.Run(camera, target); }
            catch (Exception e) { Log.ExceptionOnce("spectator-camera", e, "Moving the spectator camera failed"); }
        }

        private static void RunFreecam(CameraWrapper camera)
        {
            try { FreecamCamera.Run(camera); }
            catch (Exception e) { Log.ExceptionOnce("freecam-camera", e, "Moving the freecam camera failed"); }
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

        protected override void OnDestroy()
        {
            FreecamCamera.Exit();
            base.OnDestroy();
        }
    }
}
