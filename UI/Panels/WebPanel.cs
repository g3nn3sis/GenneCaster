// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.GameCamera;
using Zerori.GameCamera.Modes;
using Zerori.GameCamera.Webs;
using UnityEngine;

namespace Zerori.UI.Panels
{
    internal sealed class WebPanel
    {
        private const int WebNameLength = 24;

        private Vector2 scroll;
        private string webName = string.Empty;
        private string[] webNames = Array.Empty<string>();
        private Action[] webLoad = Array.Empty<Action>();
        private Action[] webDelete = Array.Empty<Action>();
        private bool websDirty = true;

        internal void Draw(Panel panel)
        {
            CameraWeb.Sync();
            if (websDirty) RefreshWebs();
            int nodes = CameraWeb.Nodes.Count;
            panel.Label(Status(nodes));
            panel.Button("Use Web Camera", CameraManager.UseWeb, CameraManager.ActiveMode != CameraMode.Web);
            DrawSelection(panel, nodes);
            DrawEditing(panel, nodes);
            DrawFiles(panel, nodes);
        }

        private static string Status(int nodes)
        {
            if (nodes == 0) return "No nodes placed";
            string web = CameraWeb.Name.Length > 0 ? CameraWeb.Name : "Unsaved";
            return $"{web}: node {Mathf.Min(WebCamera.ActiveNode, nodes - 1) + 1} of {nodes}";
        }

        private static void DrawSelection(Panel panel, int nodes)
        {
            WebCamera.Dynamic = panel.Toggle("Selection", WebCamera.Dynamic, "Dynamic", "Manual");
            if (WebCamera.Dynamic) return;
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous", WebCamera.PreviousNode, nodes > 1);
                panel.Button("Next", WebCamera.NextNode, nodes > 1);
            });
        }

        private static void DrawEditing(Panel panel, int nodes)
        {
            bool editing = WebCamera.Editing;
            panel.Button($"Edit Nodes: {(editing ? "On" : "Off")}", () => WebCamera.SetEditing(!editing));
            if (!editing) return;
            panel.Label($"Hold right mouse to fly. {CasterConfig.WebPlaceNodeKey.Value} adds a node, {CasterConfig.WebRemoveNodeKey.Value} removes the nearest.");
            panel.Button("Add Node Here", AddNodeAtCamera);
            panel.Button("Remove Nearest", RemoveNodeNearestCamera, nodes > 0);
            panel.Button("Clear Nodes", CameraWeb.Clear, nodes > 0);
        }

        private void DrawFiles(Panel panel, int nodes)
        {
            webName = panel.TextField(webName, WebNameLength, Presets.Sanitize);
            panel.Button("Save Web", SaveWeb, webName.Trim().Length > 0 && nodes > 0);
            if (webNames.Length == 0)
            {
                panel.Label("No webs saved.");
                return;
            }
            scroll = panel.ScrollArea(scroll, Panel.ListHeight, webNames.Length, () =>
            {
                for (int i = 0; i < webNames.Length; i++)
                {
                    int index = i;
                    panel.HorizontalArea(() =>
                    {
                        panel.Button(webNames[index], webLoad[index]);
                        panel.Button("x", webDelete[index]);
                    });
                }
            });
        }

        private static void AddNodeAtCamera()
        {
            CameraWrapper? camera = CameraManager.camera;
            if (camera != null && camera.IsAlive) CameraWeb.Add(camera.Transform.position);
        }

        private static void RemoveNodeNearestCamera()
        {
            CameraWrapper? camera = CameraManager.camera;
            if (camera != null && camera.IsAlive) CameraWeb.RemoveNearest(camera.Transform.position);
        }

        private void SaveWeb()
        {
            if (CameraWeb.Save(webName)) websDirty = true;
        }

        private void RefreshWebs()
        {
            websDirty = false;
            webNames = CameraWebFile.Names();
            webLoad = new Action[webNames.Length];
            webDelete = new Action[webNames.Length];
            for (int i = 0; i < webNames.Length; i++)
            {
                string name = webNames[i];
                webLoad[i] = () =>
                {
                    CameraWeb.Load(name);
                    webName = name;
                };
                webDelete[i] = () =>
                {
                    CameraWebFile.Delete(name);
                    websDirty = true;
                };
            }
        }
    }
}
