// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;
using Zetori.Base;
using Zetori.GameCamera.Modes;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraRig : IDisposable
    {
        private const float SummonWindow = 0.4f;
        private const float SummonDistance = 0.55f;
        private const float SummonDrop = 0.08f;
        private const float RiseSeconds = 0.65f;
        private const float FadeInSeconds = 0.4f;
        private const float FadeOutSeconds = 0.3f;
        private const float GroundProbeDistance = 3f;
        private const float BuriedDepth = 0.16f;
        private const float RiseWithoutGround = 1.2f;
        private const float FieldOfViewStep = 5f;
        private const float MinimumFieldOfView = 30f;
        private const float MaximumFieldOfView = 130f;
        private const float HapticStrength = 0.2f;
        private const float HapticSeconds = 0.04f;
        private const float FollowerMoveRate = 4f;
        private const float FollowerTurnRate = 8f;
        private const float HeadTurnRate = 14f;
        private const float ThirdPersonRate = 8f;
        private const float HandheldMoveRate = 18f;
        private const float HandheldTurnRate = 10f;

        private readonly DoublePressDetector summonPress = new DoublePressDetector(SummonWindow);
        private readonly VrCameraHand leftHand = new VrCameraHand(true);
        private readonly VrCameraHand rightHand = new VrCameraHand(false);
        private readonly VrCameraView view = new VrCameraView();
        private VrCameraModel? model;
        private VrCameraHand? holder;
        private bool deployed;
        private float fade;
        private float riseProgress = 1f;
        private Vector3 riseStart;
        private Vector3 riseEnd;
        private Vector3 holdPosition;
        private Quaternion holdRotation = Quaternion.identity;
        private AudioListener? headsetListener;
        private bool listenerSearched;

        internal static bool HeadsetActive => XRSettings.isDeviceActive;

        private static VrCameraMode Mode
        {
            get => CasterConfig.VrMode.Value;
            set => CasterConfig.VrMode.Value = value;
        }

        private bool TabletVisible => deployed && Mode != VrCameraMode.Follower;
        private bool TabletRisen => riseProgress >= 1f;
        private bool TabletInteractive => TabletVisible && TabletRisen && fade >= 1f;

        internal bool HeadsetHearsGame
        {
            get
            {
                GorillaTagger? tagger = GorillaTagger.Instance;
                if (!listenerSearched && tagger != null && tagger.mainCamera != null)
                {
                    headsetListener = tagger.mainCamera.GetComponentInChildren<AudioListener>();
                    listenerSearched = true;
                }
                return headsetListener != null && headsetListener.enabled;
            }
        }

        internal void Tick(CameraWrapper output)
        {
            GorillaTagger? tagger = GorillaTagger.Instance;
            GTPlayer? player = GTPlayer.Instance;
            ControllerInputPoller? input = ControllerInputPoller.instance;
            if (tagger == null || player == null || input == null || tagger.offlineVRRig == null) return;

            float scale = player.scale;
            float now = Time.unscaledTime;
            float deltaTime = Time.unscaledDeltaTime;
            leftHand.Sync(tagger.leftHandTransform, tagger.leftHandTriggerCollider.transform, input.leftGrab);
            rightHand.Sync(tagger.rightHandTransform, tagger.rightHandTriggerCollider.transform, input.rightGrab);
            Pose head = HeadView.Eyes(tagger.offlineVRRig);

            if (summonPress.Update(input.rightControllerPrimaryButton, now)) ToggleTablet(tagger, output, head, scale);
            if (model != null) UpdateTablet(tagger, model, head, scale, now, deltaTime);

            AimView(head, player.headCollider.transform.parent, scale, deltaTime);
            output.SetPose(view.Position, view.Rotation);
            if (model != null && fade > 0f)
            {
                float viewerDistance = Vector3.Distance(head.position, model.Root.position);
                model.UpdatePreview(output.Camera, view.Position, view.Rotation, CameraManager.fieldOfView, viewerDistance, scale);
            }
        }

        internal void Hide()
        {
            model?.Hide();
            holder = null;
            fade = 0f;
            FinishRise();
            view.Invalidate();
        }

        private void FinishRise()
        {
            if (model != null && !TabletRisen) model.Root.position = riseEnd;
            riseProgress = 1f;
        }

        private void ToggleTablet(GorillaTagger tagger, CameraWrapper output, Pose head, float scale)
        {
            if (TabletVisible) Dismiss(tagger);
            else Summon(tagger, output, head, scale);
        }

        private void Dismiss(GorillaTagger tagger)
        {
            deployed = false;
            holder = null;
            view.Invalidate();
            tagger.StartVibration(false, HapticStrength, HapticSeconds);
        }

        private void UpdateTablet(GorillaTagger tagger, VrCameraModel camera, Pose head, float scale, float now, float deltaTime)
        {
            camera.SetScale(scale);
            camera.SetMode(Mode);
            if (TabletInteractive)
            {
                UpdateGrab(tagger, camera);
                UpdateButtons(tagger, camera, now);
                camera.SetMode(Mode);
            }
            bool visible = TabletVisible;
            if (!visible) holder = null;
            fade = Mathf.MoveTowards(fade, visible ? 1f : 0f, deltaTime / (visible ? FadeInSeconds : FadeOutSeconds));
            if (visible) AdvanceRise(camera, deltaTime);
            MoveFollower(camera, head, scale, deltaTime);
            camera.SetFade(fade);
            if (fade > 0f) camera.Show();
            else camera.Hide();
        }

        private void AdvanceRise(VrCameraModel camera, float deltaTime)
        {
            if (TabletRisen) return;
            riseProgress = Mathf.Min(1f, riseProgress + deltaTime / RiseSeconds);
            float remaining = 1f - riseProgress;
            camera.Root.position = Vector3.Lerp(riseStart, riseEnd, 1f - remaining * remaining * remaining);
        }

        private static Vector3 RiseOrigin(Vector3 target, float scale)
        {
            bool grounded = Physics.Raycast(target, Vector3.down, out RaycastHit hit, GroundProbeDistance * scale, GTPlayer.LocomotionEnabledLayers, QueryTriggerInteraction.Ignore);
            return grounded
                ? hit.point + Vector3.down * (BuriedDepth * scale)
                : target + Vector3.down * (RiseWithoutGround * scale);
        }

        private void Summon(GorillaTagger tagger, CameraWrapper output, Pose head, float scale)
        {
            model ??= VrCameraModel.Create(output.Camera);
            deployed = true;
            holder = null;
            if (Mode == VrCameraMode.Follower) Mode = VrCameraMode.Handheld;
            Vector3 position = head.position
                + VrCameraShots.FlatForward(head.rotation) * (SummonDistance * scale)
                + Vector3.down * (SummonDrop * scale);
            riseEnd = position;
            riseStart = RiseOrigin(position, scale);
            riseProgress = 0f;
            fade = 0f;
            model.Root.SetPositionAndRotation(riseStart, VrCameraShots.LookAt(head.position, position, head.rotation));
            view.Invalidate();
            tagger.StartVibration(false, HapticStrength, HapticSeconds);
        }

        private void UpdateGrab(GorillaTagger tagger, VrCameraModel camera)
        {
            if (holder != null && !holder.Gripping)
            {
                VrCameraHand otherHand = holder == leftHand ? rightHand : leftHand;
                holder = null;
                if (otherHand.Gripping) TryHold(tagger, camera, otherHand);
            }
            if (rightHand.GripStarted) TryHold(tagger, camera, rightHand);
            if (leftHand.GripStarted) TryHold(tagger, camera, leftHand);
            if (holder == null) return;
            Transform palm = holder.Palm;
            camera.Root.SetPositionAndRotation(palm.position + palm.rotation * holdPosition, palm.rotation * holdRotation);
        }

        private void TryHold(GorillaTagger tagger, VrCameraModel camera, VrCameraHand hand)
        {
            if (holder != null) return;
            Transform palm = hand.Palm;
            if (!camera.WithinGrabReach(palm.position)) return;
            Quaternion inversePalm = Quaternion.Inverse(palm.rotation);
            holdPosition = inversePalm * (camera.Root.position - palm.position);
            holdRotation = inversePalm * camera.Root.rotation;
            holder = hand;
            tagger.StartVibration(hand.IsLeft, HapticStrength, HapticSeconds);
        }

        private void UpdateButtons(GorillaTagger tagger, VrCameraModel camera, float now)
        {
            if (Pressed(tagger, camera.FieldOfViewDown, now)) ChangeFieldOfView(-FieldOfViewStep);
            if (Pressed(tagger, camera.FieldOfViewUp, now)) ChangeFieldOfView(FieldOfViewStep);
            if (Pressed(tagger, camera.FlipButton, now) && Mode == VrCameraMode.Handheld)
            {
                camera.FlipLens();
                view.Invalidate();
            }
            VrCameraButton[] modeButtons = camera.ModeButtons;
            for (int i = 0; i < modeButtons.Length; i++)
                if (Pressed(tagger, modeButtons[i], now)) SelectMode((VrCameraMode)i);
        }

        private void SelectMode(VrCameraMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            view.Invalidate();
        }

        private bool Pressed(GorillaTagger tagger, VrCameraButton button, float now)
        {
            VrCameraHand? nearest = null;
            float proximity = float.PositiveInfinity;
            ConsiderFinger(leftHand, button, ref nearest, ref proximity);
            ConsiderFinger(rightHand, button, ref nearest, ref proximity);
            bool pressed = button.Update(proximity, now);
            if (pressed && nearest != null) tagger.StartVibration(nearest.IsLeft, HapticStrength, HapticSeconds);
            return pressed;
        }

        private void ConsiderFinger(VrCameraHand hand, VrCameraButton button, ref VrCameraHand? nearest, ref float proximity)
        {
            if (hand == holder) return;
            float candidate = button.Proximity(hand.Finger.position);
            if (candidate >= proximity) return;
            proximity = candidate;
            nearest = hand;
        }

        private static void ChangeFieldOfView(float change) =>
            CameraManager.fieldOfView = Mathf.Clamp(CameraManager.fieldOfView + change, MinimumFieldOfView, MaximumFieldOfView);

        private void MoveFollower(VrCameraModel camera, Pose head, float scale, float deltaTime)
        {
            if (Mode != VrCameraMode.Follower || !deployed) return;
            Transform root = camera.Root;
            Pose target = VrCameraShots.Follower(root.position, head, scale);
            root.SetPositionAndRotation(
                Vector3.Lerp(root.position, target.position, VrCameraView.Blend(FollowerMoveRate, deltaTime)),
                Quaternion.Slerp(root.rotation, target.rotation, VrCameraView.Blend(FollowerTurnRate, deltaTime)));
        }

        private void AimView(Pose head, Transform? playSpace, float scale, float deltaTime)
        {
            VrCameraMode mode = Mode;
            if (mode == VrCameraMode.ThirdPerson)
            {
                view.Step(VrCameraShots.ThirdPerson(head, scale), ThirdPersonRate, ThirdPersonRate, scale, deltaTime);
                return;
            }
            if (mode == VrCameraMode.FirstPerson || model == null || !deployed || !TabletRisen)
            {
                view.Step(head, 0f, HeadTurnRate, scale, deltaTime);
                return;
            }
            Transform lens = model.Lens;
            var lensPose = new Pose(lens.position, lens.rotation);
            if (mode == VrCameraMode.Follower) view.Step(lensPose, 0f, 0f, scale, deltaTime);
            else view.Step(lensPose, HandheldMoveRate, HandheldTurnRate, scale, deltaTime, holder != null ? playSpace : null);
        }

        public void Dispose()
        {
            model?.Dispose();
            model = null;
        }
    }
}
