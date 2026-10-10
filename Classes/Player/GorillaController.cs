using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.Player
{
    public sealed class GorillaController : ObjectComponent
    {
        public static bool freeCam, mouseClick;
        public static float _movementSpeed = 10f, _mouseSpeed = 0.3f;
        public static float _yaw, _pitch;
        public void FixedUpdate()
        {
            if (!GorillaTagger.Instance.offlineVRRig) return;
            try
            {
                Rigidbody rb = GTPlayer.Instance.playerRigidBody;
                rb.AddForce(-Physics.gravity * rb.mass * (GTPlayer.Instance.scale * 1.15f), ForceMode.Force);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                switch (Mouse.current.rightButton.isPressed)
                {
                    case true:
                        if (!freeCam) return;
                        if (Keyboard.current.wKey.isPressed) GTPlayer.Instance.transform.position += (GorillaTagger.Instance.offlineVRRig.transform.forward * (_movementSpeed * Time.deltaTime));
                        if (Keyboard.current.sKey.isPressed) GTPlayer.Instance.transform.position += -(GorillaTagger.Instance.offlineVRRig.transform.forward * (_movementSpeed * Time.deltaTime));
                        if (Keyboard.current.dKey.isPressed) GTPlayer.Instance.transform.position += (GorillaTagger.Instance.offlineVRRig.transform.right * (_movementSpeed * Time.deltaTime));
                        if (Keyboard.current.aKey.isPressed) GTPlayer.Instance.transform.position += -(GorillaTagger.Instance.offlineVRRig.transform.right * (_movementSpeed * Time.deltaTime));

                        if (Keyboard.current.spaceKey.isPressed) GTPlayer.Instance.transform.position += (GorillaTagger.Instance.offlineVRRig.transform.up * (_movementSpeed * Time.deltaTime));
                        if (Keyboard.current.leftShiftKey.isPressed) GTPlayer.Instance.transform.position += -(GorillaTagger.Instance.offlineVRRig.transform.up * (_movementSpeed * Time.deltaTime));

                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                        _yaw += mouseDelta.x * _mouseSpeed;
                        _pitch -= mouseDelta.y * _mouseSpeed;
                        _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                        GorillaTagger.Instance.headCollider.transform.rotation =
                            Quaternion.Euler(_pitch, _yaw, 0f);

                        MeshCollider[] colliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
                        foreach (MeshCollider collider in colliders)
                        {
                            collider.enabled = !Keyboard.current.eKey.isPressed;
                        }
                        break;
                    case false:
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        if (!mouseClick) return;
                        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                        if (Physics.Raycast(ray, out RaycastHit hit, 500f, GTPlayer.LocomotionEnabledLayers))
                        {
                            if (Mouse.current.leftButton.isPressed)
                            {
                                GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<TransformFollow>().enabled = false;
                                GorillaTagger.Instance.rightHandTriggerCollider.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                                GorillaTagger.Instance.rightHandTriggerCollider.transform.position = hit.point;
                            }
                            GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<TransformFollow>().enabled = true;
                        }
                        break;

                }
            }
            catch { }
        }
    }
}
