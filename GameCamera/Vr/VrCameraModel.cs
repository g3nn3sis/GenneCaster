// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Zetori.Base;
using Zetori.Game;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraModel : IDisposable
    {
        private const string MeshResource = "Zetori.Models.vr_camera.zmdl";
        private const string TextureResource = "Zetori.Models.vr_camera.png";
        private const string TextureKeyword = "_USE_TEXTURE";
        private const int PreviewWidth = 640;
        private const int PreviewHeight = 360;
        private const int PreviewDepthBits = 16;
        private const float PreviewInterval = 1f / 30f;
        private const float PreviewRange = 6f;
        private const float LabelFontSize = 0.09f;
        private const string DepthPass = "DepthOnly";
        private const string RenderTypeTag = "RenderType";
        private const string TransparentRenderType = "Transparent";
        private const int ScreenDrawOrder = 1;
        private const int ButtonDrawOrder = 2;
        private const int IconDrawOrder = 3;
        private const int IconLensDrawOrder = 4;
        private const int LabelSortingOrder = 1;
        private const float BarOffset = 0.247f;
        private const float BarHalfLength = 0.1075f;
        private const float GrabReach = 0.11f;

        private static readonly Vector3 RearLensPosition = new Vector3(-0.028f, 0.022f, 0.014f);
        private static readonly Vector3 ScreenLensPosition = new Vector3(-0.028f, 0.022f, -0.016f);
        private static readonly Quaternion ScreenLensRotation = Quaternion.Euler(0f, 180f, 0f);
        private static readonly Vector3 LabelOffset = new Vector3(0f, 0f, -0.0034f);
        private static readonly Vector2 LabelSize = new Vector2(0.05f, 0.03f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.22f, 0.26f);
        private static readonly Color FlipUnavailableColor = new Color(0.38f, 0.4f, 0.44f);

        private readonly List<Material> materials = new List<Material>();
        private readonly List<Color> materialColors = new List<Color>();
        private readonly List<int> materialDrawOrders = new List<int>();
        private readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly GameObject root;
        private readonly GameObject previewObject;
        private readonly Camera previewCamera;
        private readonly RenderTexture previewTexture;
        private Texture2D? bodyTexture;
        private Renderer[] renderers = Array.Empty<Renderer>();
        private Camera? outputCamera;
        private Material idleButton = null!;
        private Material activeButton = null!;
        private Renderer flipIcon = null!;
        private Material flipAvailable = null!;
        private Material flipUnavailable = null!;
        private VrCameraMode? shownMode;
        private bool lensFlipped;
        private bool renderersHidden;
        private bool blending;
        private float shownFade = 1f;
        private float opaqueSourceBlend;
        private float opaqueDestinationBlend;
        private float opaqueSourceAlphaBlend;
        private float opaqueDestinationAlphaBlend;
        private float opaqueDepthWrite;
        private int opaqueRenderQueue;
        private string opaqueRenderType = string.Empty;
        private float nextPreview;

        internal Transform Root => root.transform;
        internal Transform Lens { get; private set; } = null!;
        internal VrCameraButton[] ModeButtons { get; private set; } = Array.Empty<VrCameraButton>();
        internal VrCameraButton FieldOfViewDown { get; private set; } = null!;
        internal VrCameraButton FieldOfViewUp { get; private set; } = null!;
        internal VrCameraButton FlipButton { get; private set; } = null!;

        private VrCameraModel(Camera source)
        {
            root = new GameObject("_Zetori VR Camera");
            UnityEngine.Object.DontDestroyOnLoad(root);
            previewObject = new GameObject("_Zetori VR Camera Preview");
            UnityEngine.Object.DontDestroyOnLoad(previewObject);
            previewTexture = new RenderTexture(PreviewWidth, PreviewHeight, PreviewDepthBits) { name = "_Zetori VR Camera Preview" };
            previewCamera = previewObject.AddComponent<Camera>();
            previewCamera.CopyFrom(source);
            previewCamera.cameraType = CameraType.Reflection;
            previewCamera.stereoTargetEye = StereoTargetEyeMask.None;
            previewCamera.targetTexture = previewTexture;
            previewCamera.enabled = false;
        }

        internal static VrCameraModel Create(Camera source)
        {
            var model = new VrCameraModel(source);
            try
            {
                model.Build();
                RenderPipelineManager.beginCameraRendering += model.OnBeginCameraRendering;
                return model;
            }
            catch
            {
                model.Dispose();
                throw;
            }
        }

        internal void SetScale(float scale) => root.transform.localScale = Vector3.one * scale;

        internal void Show()
        {
            if (!root.activeSelf) root.SetActive(true);
        }

        internal void Hide()
        {
            if (root.activeSelf) root.SetActive(false);
            previewCamera.enabled = false;
        }

        internal bool WithinGrabReach(Vector3 hand)
        {
            Vector3 local = root.transform.InverseTransformPoint(hand);
            float across = Mathf.Abs(local.x) - BarOffset;
            float along = Mathf.Max(0f, Mathf.Abs(local.y) - BarHalfLength);
            return across * across + along * along + local.z * local.z <= GrabReach * GrabReach;
        }

        internal void SetFade(float fade)
        {
            if (Mathf.Approximately(fade, shownFade)) return;
            shownFade = fade;
            bool blend = fade < 1f;
            for (int i = 0; i < materials.Count; i++)
            {
                if (blend != blending) ApplyBlending(materials[i], materialDrawOrders[i], blend);
                Color color = materialColors[i];
                color.a = fade;
                UberShader.BaseColor.SetValue(materials[i], color);
            }
            blending = blend;
            foreach (TextMeshPro label in labels) label.alpha = fade;
        }

        private void ApplyBlending(Material material, int drawOrder, bool blend)
        {
            material.SetFloat(UberShader.SrcBlend.nameID, blend ? (float)BlendMode.SrcAlpha : opaqueSourceBlend);
            material.SetFloat(UberShader.DstBlend.nameID, blend ? (float)BlendMode.OneMinusSrcAlpha : opaqueDestinationBlend);
            material.SetFloat(UberShader.SrcBlendAlpha.nameID, blend ? (float)BlendMode.One : opaqueSourceAlphaBlend);
            material.SetFloat(UberShader.DstBlendAlpha.nameID, blend ? (float)BlendMode.OneMinusSrcAlpha : opaqueDestinationAlphaBlend);
            material.SetFloat(UberShader.ZWrite.nameID, blend ? 0f : opaqueDepthWrite);
            material.SetOverrideTag(RenderTypeTag, blend ? TransparentRenderType : opaqueRenderType);
            material.SetShaderPassEnabled(DepthPass, !blend);
            material.renderQueue = blend ? (int)RenderQueue.Transparent + drawOrder : opaqueRenderQueue;
        }

        internal void SetMode(VrCameraMode mode)
        {
            if (shownMode == mode) return;
            shownMode = mode;
            for (int i = 0; i < ModeButtons.Length; i++)
                ModeButtons[i].Renderer.sharedMaterial = i == (int)mode ? activeButton : idleButton;
            flipIcon.sharedMaterial = mode == VrCameraMode.Handheld ? flipAvailable : flipUnavailable;
            ApplyLens();
        }

        internal void FlipLens()
        {
            lensFlipped = !lensFlipped;
            ApplyLens();
        }

        private void ApplyLens()
        {
            bool facesScreenSide = lensFlipped && shownMode == VrCameraMode.Handheld;
            Lens.localPosition = facesScreenSide ? ScreenLensPosition : RearLensPosition;
            Lens.localRotation = facesScreenSide ? ScreenLensRotation : Quaternion.identity;
        }

        internal void UpdatePreview(Camera output, Vector3 position, Quaternion rotation, float fieldOfView, float viewerDistance, float scale)
        {
            outputCamera = output;
            previewCamera.transform.SetPositionAndRotation(position, rotation);
            previewCamera.fieldOfView = fieldOfView;
            float now = Time.unscaledTime;
            bool due = now >= nextPreview && viewerDistance <= PreviewRange * scale;
            if (due) nextPreview = now + PreviewInterval;
            previewCamera.enabled = due;
        }

        private void Build()
        {
            byte[] meshData = EmbeddedResources.Read(MeshResource) ?? throw new FileNotFoundException("The VR camera model is missing.", MeshResource);
            Dictionary<string, VrCameraMeshPart> parts = VrCameraMeshFile.Read(meshData);
            foreach (VrCameraMeshPart part in parts.Values) meshes.Add(part.Mesh);
            bodyTexture = AssetManager.LoadTexture(TextureResource, true) ?? throw new FileNotFoundException("The VR camera texture is missing.", TextureResource);

            idleButton = CreateMaterial(ButtonColor, ButtonDrawOrder);
            activeButton = CreateMaterial(UI.ColorSpace.Current.Color, ButtonDrawOrder);
            Place(parts["body"], "body", CreateTexturedMaterial(bodyTexture, 0), root.transform);
            Place(parts["screen"], "screen", CreateTexturedMaterial(previewTexture, ScreenDrawOrder), root.transform);

            ModeButtons = new[]
            {
                Button(parts, "button_first", "FIRST"),
                Button(parts, "button_third", "THIRD"),
                Button(parts, "button_follow", "FOLLOW"),
                Button(parts, "button_hand", "HAND")
            };
            FieldOfViewDown = Button(parts, "button_fovdown", "FOV -");
            FieldOfViewUp = Button(parts, "button_fovup", "FOV +");

            var lens = new GameObject("lens");
            lens.transform.SetParent(root.transform, false);
            Lens = lens.transform;
            ApplyLens();
            BuildFlipButton(parts);

            renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void BuildFlipButton(Dictionary<string, VrCameraMeshPart> parts)
        {
            flipAvailable = CreateMaterial(Color.white, IconDrawOrder);
            flipUnavailable = CreateMaterial(FlipUnavailableColor, IconDrawOrder);
            MeshRenderer cap = Place(parts["button_flip"], "button_flip", idleButton, root.transform);
            flipIcon = Place(parts["flip_icon"], "icon", flipAvailable, root.transform);
            flipIcon.transform.SetParent(cap.transform, true);
            Material iconLens = CreateMaterial(ButtonColor, IconLensDrawOrder);
            Place(parts["flip_dot"], "lens", iconLens, root.transform).transform.SetParent(cap.transform, true);
            FlipButton = new VrCameraButton(cap.transform, cap);
        }

        private VrCameraButton Button(Dictionary<string, VrCameraMeshPart> parts, string partName, string label)
        {
            MeshRenderer renderer = Place(parts[partName], partName, idleButton, root.transform);
            Text("label", renderer.transform, LabelOffset, LabelSize, LabelFontSize, TextAlignmentOptions.Center).text = label;
            return new VrCameraButton(renderer.transform, renderer);
        }

        private static MeshRenderer Place(VrCameraMeshPart part, string name, Material material, Transform parent)
        {
            var partObject = new GameObject(name);
            partObject.transform.SetParent(parent, false);
            partObject.transform.localPosition = part.Pivot;
            partObject.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            var renderer = partObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private TextMeshPro Text(string name, Transform parent, Vector3 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            var text = textObject.AddComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.sortingOrder = LabelSortingOrder;
            TMP_FontAsset? font = HudAssets.CurrentFont;
            if (font != null) text.font = font;
            labels.Add(text);
            return text;
        }

        private Material CreateTexturedMaterial(Texture texture, int drawOrder)
        {
            Material material = CreateMaterial(Color.white, drawOrder);
            material.EnableKeyword(TextureKeyword);
            UberShader.BaseMap.SetValue(material, texture);
            return material;
        }

        private Material CreateMaterial(Color color, int drawOrder)
        {
            var material = new Material(UberShader.ReferenceMaterial);
            UberShader.BaseColor.SetValue(material, color);
            opaqueSourceBlend = material.GetFloat(UberShader.SrcBlend.nameID);
            opaqueDestinationBlend = material.GetFloat(UberShader.DstBlend.nameID);
            opaqueSourceAlphaBlend = material.GetFloat(UberShader.SrcBlendAlpha.nameID);
            opaqueDestinationAlphaBlend = material.GetFloat(UberShader.DstBlendAlpha.nameID);
            opaqueDepthWrite = material.GetFloat(UberShader.ZWrite.nameID);
            opaqueRenderQueue = material.renderQueue;
            opaqueRenderType = material.GetTag(RenderTypeTag, false);
            materials.Add(material);
            materialColors.Add(color);
            materialDrawOrders.Add(drawOrder);
            return material;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            bool hidden = camera == previewCamera || camera == outputCamera;
            if (hidden == renderersHidden) return;
            renderersHidden = hidden;
            foreach (Renderer renderer in renderers)
                if (renderer != null) renderer.forceRenderingOff = hidden;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (previewTexture != null)
            {
                previewTexture.Release();
                UnityEngine.Object.Destroy(previewTexture);
            }
            if (bodyTexture != null) UnityEngine.Object.Destroy(bodyTexture);
            foreach (Material material in materials)
                if (material != null) UnityEngine.Object.Destroy(material);
            materials.Clear();
            foreach (Mesh mesh in meshes)
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
            if (previewObject != null) UnityEngine.Object.Destroy(previewObject);
            if (root != null) UnityEngine.Object.Destroy(root);
        }
    }
}
