// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.GameCamera;
using Zerori.Replay;
using UnityEngine;

namespace Zerori.UI.Panels
{
    internal sealed class ReplayPanel
    {
        private const int RenameLength = 32;

        private Vector2 puppetScroll;
        private Vector2 savedScroll;
        private string? renaming;
        private string renameText = string.Empty;

        internal void Draw(Panel panel)
        {
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                DrawPlayback(panel, replay);
                return;
            }
            if (ReplaySession.IsRecording)
            {
                panel.Label($"Recording {ReplayTimeline.Format(ReplaySession.RecordedSeconds)} / {ReplayTimeline.Format(ReplaySession.MaxSeconds)}");
                panel.Button("Stop Recording", ReplaySession.StopRecording);
                return;
            }
            panel.Button("Record", ReplaySession.StartRecording, !ReplaySession.IsLoading);
            if (ReplaySession.IsSaving) panel.Label("Saving replay...");
            if (ReplaySession.IsLoading) panel.Label($"Loading {ReplaySession.LoadingName}...");
            DrawSavedReplays(panel);
        }

        private void DrawSavedReplays(Panel panel)
        {
            string[] names = ReplaySession.SavedReplays;
            if (names.Length == 0)
            {
                panel.Label("No saved replays.");
                return;
            }
            savedScroll = panel.ScrollArea(savedScroll, Panel.ListHeight, names.Length, () =>
            {
                foreach (string name in names)
                {
                    if (name == renaming) DrawRenameRow(panel, name);
                    else DrawReplayRow(panel, name);
                }
            });
        }

        private void DrawReplayRow(Panel panel, string name)
        {
            panel.HorizontalArea(() =>
            {
                panel.Button(name, () => ReplaySession.Play(name), () => BeginRename(name), !ReplaySession.IsLoading);
                panel.Button("x", () => ReplaySession.Delete(name));
            });
        }

        private void DrawRenameRow(Panel panel, string name)
        {
            panel.HorizontalArea(() =>
            {
                renameText = panel.TextField(renameText, RenameLength, Presets.Sanitize);
                panel.Button("Save", () => CommitRename(name), renameText.Trim().Length > 0);
                panel.Button("x", () => renaming = null);
            });
        }

        private void BeginRename(string name)
        {
            renaming = name;
            renameText = name;
        }

        private void CommitRename(string name)
        {
            if (ReplaySession.Rename(name, renameText.Trim())) renaming = null;
        }

        private void DrawPlayback(Panel panel, ReplayPlayer replay)
        {
            panel.Label($"{ReplayTimeline.Format(replay.Cursor)} / {ReplayTimeline.Format(replay.Recording.Duration)}");
            panel.HorizontalArea(() =>
            {
                panel.Button(replay.Playing ? "Pause" : "Play", replay.TogglePlaying);
                panel.Button("Stop", ReplaySession.ExitReplay);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Spectate", () => replay.Camera = CameraMode.Spectator, replay.Camera != CameraMode.Spectator);
                panel.Button("FreeCam", () => replay.Camera = CameraMode.Freecam, replay.Camera != CameraMode.Freecam);
                panel.Button("Web", () => replay.Camera = CameraMode.Web, replay.Camera != CameraMode.Web);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous", () => replay.SpectateNext(-1));
                panel.Button("Next", () => replay.SpectateNext(1));
            });
            replay.RigLerp = panel.Slider("Rig Lerp", replay.RigLerp, ReplayPlayer.MinRigLerp, ReplayPlayer.MaxRigLerp);
            panel.Label(CameraStatus(replay));
            IReadOnlyList<ReplayPuppet> puppets = replay.Puppets;
            puppetScroll = panel.ScrollArea(puppetScroll, Panel.ListHeight, puppets.Count, () =>
            {
                for (int i = 0; i < puppets.Count; i++)
                {
                    int index = i;
                    panel.Button(puppets[i].Actor.Name, () => replay.Spectate(index), puppets[i].Visible && !replay.IsSpectating(puppets[i]));
                }
            });
        }

        private static string CameraStatus(ReplayPlayer replay) => replay.Camera switch
        {
            CameraMode.Freecam => "FreeCam: hold right mouse, WASD to fly",
            CameraMode.Web => "Web: watching from the camera web",
            _ => $"Spectating: {replay.Spectated?.Actor.Name ?? "nobody"}"
        };
    }
}
