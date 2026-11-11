using GenneCaster.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.GameCamera.Modes
{
    internal static class SpectatorCamera
    {
        private const float OrbitDistanceScale = 3.5f;
        private const float MaxOrbitPitch = 89f;

        internal static float OrbitYaw;
        internal static float OrbitPitch;

        internal static float HeadDistance => CasterConfig.HeadDistance.Value;
        internal static bool IsFirstPerson => HeadDistance <= 0f;

        internal static void ResetOrbit()
        {
            OrbitYaw = 0f;
            OrbitPitch = 0f;
        }

        internal static void Run(CameraWrapper camera, VRRig target)
        {
            float deltaTime = Time.deltaTime;
            ReadOrbitInput(deltaTime);
            if (IsFirstPerson)
            {
                HeadView.Follow(camera, target);
                return;
            }
            Vector3 pivot = target.transform.position;
            Quaternion orbit = Quaternion.Euler(OrbitPitch, target.transform.eulerAngles.y + OrbitYaw, 0f);
            Vector3 desired = pivot + orbit * Vector3.back * (HeadDistance * OrbitDistanceScale);
            Quaternion look = Quaternion.LookRotation(pivot - desired, Vector3.up);
            float positionStep = Smoothing.Step(CasterConfig.PositionSmoothing.Value, deltaTime);
            float rotationStep = Smoothing.Step(0.5f + CasterConfig.RotationSmoothing.Value * 0.5f, deltaTime);
            camera.SetPose(
                Vector3.Lerp(camera.Transform.position, desired, positionStep),
                Quaternion.Slerp(camera.Transform.rotation, look, rotationStep));
        }

        private const float AutoRotateScale = 0.2f;

        private static void ReadOrbitInput(float deltaTime)
        {
            if (CasterConfig.OrbitAutoRotate.Value)
                OrbitYaw -= CasterConfig.OrbitSpeed.Value * AutoRotateScale * deltaTime;
            Keyboard? keyboard = Keyboard.current;
            if (keyboard == null)
            {
                OrbitYaw = Mathf.Repeat(OrbitYaw + 180f, 360f) - 180f;
                return;
            }
            float step = CasterConfig.OrbitSpeed.Value * deltaTime;
            if (keyboard.rightArrowKey.isPressed) OrbitYaw -= step;
            if (keyboard.leftArrowKey.isPressed) OrbitYaw += step;
            if (keyboard.upArrowKey.isPressed) OrbitPitch += step;
            if (keyboard.downArrowKey.isPressed) OrbitPitch -= step;
            OrbitYaw = Mathf.Repeat(OrbitYaw + 180f, 360f) - 180f;
            OrbitPitch = Mathf.Clamp(OrbitPitch, -MaxOrbitPitch, MaxOrbitPitch);
        }
    }
}
