// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera.Modes;
using Zetori.GameCamera.Vr;
using Zetori.Player;
using Zetori.Replay;
using UnityEngine;

namespace Zetori.GameCamera
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
        private VrCameraRig? vrRig;

        internal void LateUpdate()
        {
            if (FliesCamera && GorillaController.wasd) GorillaController.wasd = false;
            if (!EnsureCamera()) return;
            if (VrCameraRig.HeadsetActive)
            {
                RunVr(camera!);
                return;
            }
            vrRig?.Hide();
            camera!.Apply(fieldOfView, nearClipPlane, audioListener);
            SwitchMode(camera);
            switch (runningMode)
            {
                case CameraMode.Default: RunDefault(camera); break;
                case CameraMode.Freecam: RunFreecam(camera); break;
                case CameraMode.Web: RunWeb(camera); break;
                default: RunSpectator(camera); break;
            }
        }

        private static bool FliesCamera => mode == CameraMode.Freecam || (mode == CameraMode.Web && WebCamera.Editing);

        internal static CameraMode ActiveMode => ReplaySession.Player is ReplayPlayer replay ? replay.Camera : mode;

        internal static void UseWeb()
        {
            if (ReplaySession.Player is ReplayPlayer replay) replay.Camera = CameraMode.Web;
            else mode = CameraMode.Web;
        }

        private void RunVr(CameraWrapper camera)
        {
            LeaveDesktopMode();
            vrRig ??= new VrCameraRig();
            camera.Apply(fieldOfView, nearClipPlane, audioListener && !vrRig.HeadsetHearsGame);
            try { vrRig.Tick(camera); }
            catch (Exception e) { Log.ExceptionOnce("vr-camera", e, "Updating the VR camera failed"); }
        }

        private void LeaveDesktopMode()
        {
            if (runningMode == CameraMode.Freecam) FreecamCamera.Exit();
            if (runningMode == CameraMode.Web) WebCamera.Exit();
            runningMode = null;
        }

        private void SwitchMode(CameraWrapper camera)
        {
            CameraMode next = ActiveMode;
            if (runningMode == next) return;
            if (runningMode == CameraMode.Freecam) FreecamCamera.Exit();
            if (runningMode == CameraMode.Web) WebCamera.Exit();
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
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                RunReplaySpectator(camera, replay);
                return;
            }
            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return;
            try { SpectatorCamera.Run(camera, target); }
            catch (Exception e) { Log.ExceptionOnce("spectator-camera", e, "Moving the spectator camera failed"); }
        }

        private static void RunReplaySpectator(CameraWrapper camera, ReplayPlayer replay)
        {
            ReplayPuppet? target = replay.Spectated;
            if (target == null) return;
            try { SpectatorCamera.Run(camera, target.Body, target.Eyes); }
            catch (Exception e) { Log.ExceptionOnce("replay-camera", e, "Moving the replay camera failed"); }
        }

        private static void RunFreecam(CameraWrapper camera)
        {
            try { FreecamCamera.Run(camera); }
            catch (Exception e) { Log.ExceptionOnce("freecam-camera", e, "Moving the freecam camera failed"); }
        }

        private static void RunWeb(CameraWrapper camera)
        {
            try { WebCamera.Run(camera); }
            catch (Exception e) { Log.ExceptionOnce("web-camera", e, "Moving the web camera failed"); }
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
            WebCamera.Exit();
            vrRig?.Dispose();
            vrRig = null;
            base.OnDestroy();
        }
    }
}
