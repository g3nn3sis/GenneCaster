// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.GameCamera.Modes
{
    internal static class FreecamCamera
    {
        private const float MaxPitch = 80f;

        private static Vector3 position;
        private static float yaw, pitch;
        private static bool looking;

        internal static void Enter(CameraWrapper camera)
        {
            position = camera.Transform.position;
            Vector3 euler = camera.Transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -MaxPitch, MaxPitch);
        }

        internal static void Exit() => SetLooking(false);

        internal static void Run(CameraWrapper camera)
        {
            Mouse? mouse = Mouse.current;
            Keyboard? keyboard = Keyboard.current;
            bool look = mouse != null && keyboard != null && mouse.rightButton.isPressed;
            SetLooking(look);
            if (look) Fly(mouse!, keyboard!);
            camera.SetPose(position, Quaternion.Euler(pitch, yaw, 0f));
        }

        private static void Fly(Mouse mouse, Keyboard keyboard)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float sensitivity = CasterConfig.MouseSpeed.Value;
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -MaxPitch, MaxPitch);
            position += MoveDirection(keyboard) * (CasterConfig.MovementSpeed.Value * Time.deltaTime);
        }

        private static Vector3 MoveDirection(Keyboard keyboard)
        {
            Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
            Vector3 forward = heading * Vector3.forward;
            Vector3 right = heading * Vector3.right;
            Vector3 direction = Vector3.zero;
            if (keyboard.wKey.isPressed) direction += forward;
            if (keyboard.sKey.isPressed) direction -= forward;
            if (keyboard.dKey.isPressed) direction += right;
            if (keyboard.aKey.isPressed) direction -= right;
            if (keyboard.spaceKey.isPressed) direction += Vector3.up;
            if (keyboard.leftShiftKey.isPressed) direction -= Vector3.up;
            return direction;
        }

        private static void SetLooking(bool look)
        {
            if (look == looking) return;
            looking = look;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;
        }
    }
}
