// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.Replay;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Zetori.Player
{
    internal sealed class GorillaController : ObjectComponent
    {
        internal static bool wasd { get => CasterConfig.Wasd.Value; set => CasterConfig.Wasd.Value = value; }
        internal static bool mouseClick { get => CasterConfig.MouseClick.Value; set => CasterConfig.MouseClick.Value = value; }
        internal static float MovementSpeed { get => CasterConfig.MovementSpeed.Value; set => CasterConfig.MovementSpeed.Value = value; }
        internal static float MouseSensitivity { get => CasterConfig.MouseSpeed.Value; set => CasterConfig.MouseSpeed.Value = value; }

        private const float MaxPitch = 80f;
        private const float ClickRange = 500f;
        private const float GravityCompensation = 1.12f;

        private float yaw, pitch;
        private bool flying;
        private bool noClipActive;
        private readonly List<MeshCollider> disabledColliders = new List<MeshCollider>();

        internal void FixedUpdate()
        {
            GTPlayer? player = GTPlayer.Instance;
            if (player == null || player.playerRigidBody == null) return;
            if (!XRSettings.isDeviceActive) Float(player);
            Mouse? mouse = Mouse.current;
            if (mouseClick && !flying && mouse != null && GorillaTagger.Instance != null)
            {
                try { Click(GorillaTagger.Instance, mouse); }
                catch (Exception e) { Log.ExceptionOnce("mouse-click", e, "Mouse click failed"); }
            }
        }

        private static void Float(GTPlayer player)
        {
            Rigidbody body = player.playerRigidBody;
            body.AddForce(-Physics.gravity * body.mass * (player.scale * GravityCompensation), ForceMode.Force);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        internal void Update()
        {
            GTPlayer? player = GTPlayer.Instance;
            GorillaTagger? tagger = GorillaTagger.Instance;
            Mouse? mouse = Mouse.current;
            Keyboard? keyboard = Keyboard.current;
            if (player == null || tagger == null || tagger.offlineVRRig == null || mouse == null || keyboard == null) return;
            try
            {
                bool fly = wasd && !ReplaySession.IsReplaying && mouse.rightButton.isPressed;
                if (fly != flying) SetFlying(fly, tagger);
                if (fly) Fly(player, tagger, mouse);
            }
            catch (Exception e) { Log.ExceptionOnce("wasd", e, "WASD flight update failed"); }
        }

        private void SetFlying(bool fly, GorillaTagger tagger)
        {
            flying = fly;
            Cursor.lockState = fly ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !fly;
            if (fly)
            {
                Vector3 euler = tagger.headCollider.transform.rotation.eulerAngles;
                yaw = euler.y;
                pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -MaxPitch, MaxPitch);
            }
            else SetNoClip(false);
        }

        private void Fly(GTPlayer player, GorillaTagger tagger, Mouse mouse)
        {
            Transform rig = tagger.offlineVRRig.transform;
            Vector3 direction = Vector3.zero;
            if (CasterConfig.KeyHeld(CasterConfig.FlyForwardKey)) direction += rig.forward;
            if (CasterConfig.KeyHeld(CasterConfig.FlyBackKey)) direction -= rig.forward;
            if (CasterConfig.KeyHeld(CasterConfig.FlyRightKey)) direction += rig.right;
            if (CasterConfig.KeyHeld(CasterConfig.FlyLeftKey)) direction -= rig.right;
            if (CasterConfig.KeyHeld(CasterConfig.FlyUpKey)) direction += rig.up;
            if (CasterConfig.KeyHeld(CasterConfig.FlyDownKey)) direction -= rig.up;
            player.transform.position += direction * (MovementSpeed * Time.deltaTime);

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * MouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * MouseSensitivity, -MaxPitch, MaxPitch);
            tagger.headCollider.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            SetNoClip(CasterConfig.KeyHeld(CasterConfig.NoClipKey));
        }

        private void SetNoClip(bool noClip)
        {
            if (noClip == noClipActive) return;
            noClipActive = noClip;
            if (noClip)
            {
                foreach (MeshCollider collider in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!collider.enabled) continue;
                    collider.enabled = false;
                    disabledColliders.Add(collider);
                }
                return;
            }
            foreach (MeshCollider collider in disabledColliders)
                if (collider != null) collider.enabled = true;
            disabledColliders.Clear();
        }

        private static void Click(GorillaTagger tagger, Mouse mouse)
        {
            if (!mouse.leftButton.isPressed) return;
            Camera? view = CameraManager.camera?.Camera ?? Camera.main;
            if (view == null) return;
            Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, ClickRange, GTPlayer.LocomotionEnabledLayers)) return;
            Transform hand = tagger.rightHandTriggerCollider.transform;
            TransformFollow follow = hand.GetComponent<TransformFollow>();
            follow.enabled = false;
            hand.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            hand.position = hit.point;
            follow.enabled = true;
        }

        protected override void OnDestroy()
        {
            SetNoClip(false);
            base.OnDestroy();
        }
    }
}
