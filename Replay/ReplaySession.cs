// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.Player;
using System.Threading.Tasks;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class ReplaySession : ObjectComponent
    {
        private static readonly TimeSpan QuitSaveTimeout = TimeSpan.FromSeconds(30);

        private static ReplayRecorder? recorder;
        private static ReplayPlayer? player;
        private static Task? saving;
        private static string? savingName;
        private static Task<ReplayRecording>? loading;
        private static string? loadingName;
        private static string[] savedReplays = Array.Empty<string>();
        private static bool listDirty = true;

        internal static bool IsRecording => recorder != null;
        internal static bool IsReplaying => player != null;
        internal static bool IsSaving => saving != null;
        internal static bool IsLoading => loading != null;
        internal static string? LoadingName => loadingName;
        internal static ReplayPlayer? Player => player;
        internal static float RecordedSeconds => recorder?.Elapsed ?? 0f;
        internal static float MaxSeconds => ReplayRecorder.MaxDuration;

        internal static string[] SavedReplays
        {
            get
            {
                if (!listDirty) return savedReplays;
                listDirty = false;
                savedReplays = ReplayFile.Names();
                return savedReplays;
            }
        }

        internal static void StartRecording()
        {
            if (IsRecording || IsReplaying || IsLoading) return;
            recorder = new ReplayRecorder();
            Log.Info("Replay recording started.");
        }

        internal static void StopRecording()
        {
            ReplayRecording? recording = FinishRecording();
            if (recording == null) return;
            string name = ReplayFile.NewName();
            Task previous = saving ?? Task.CompletedTask;
            savingName = name;
            saving = previous.ContinueWith(_ => ReplayFile.Save(recording, name), TaskScheduler.Default);
        }

        internal static void Play(string name)
        {
            if (IsRecording || IsReplaying || IsLoading || name == savingName) return;
            loadingName = name;
            loading = Task.Run(() => ReplayFile.Load(name));
        }

        internal static void Delete(string name)
        {
            if (name == loadingName || name == savingName) return;
            ReplayFile.Delete(name);
            listDirty = true;
        }

        internal static void ExitReplay()
        {
            if (player == null) return;
            try { player.Dispose(); }
            catch (Exception e) { Log.Exception(e, "Leaving the replay failed"); }
            player = null;
        }

        internal void Update()
        {
            PollSaving();
            PollLoading();
            if (player == null) return;
            try
            {
                ReadKeys(player);
                player.Tick(Time.unscaledDeltaTime);
            }
            catch (Exception e) { Log.ExceptionOnce("replay-play", e, "Playing the replay failed"); }
        }

        internal void LateUpdate()
        {
            if (recorder == null) return;
            try
            {
                recorder.Tick();
                if (recorder.Full) StopRecording();
            }
            catch (Exception e) { Log.ExceptionOnce("replay-record", e, "Recording the replay failed"); }
        }

        private static ReplayRecording? FinishRecording()
        {
            if (recorder == null) return null;
            ReplayRecording? recording = null;
            try { recording = recorder.Finish(); }
            catch (Exception e) { Log.Exception(e, "Finishing the replay recording failed"); }
            recorder = null;
            return recording;
        }

        private static void PollSaving()
        {
            if (saving == null || !saving.IsCompleted) return;
            if (saving.IsFaulted) Log.Exception(saving.Exception!.GetBaseException(), $"Saving replay '{savingName}' failed");
            else Log.Info($"Saved replay '{savingName}'.");
            saving = null;
            savingName = null;
            listDirty = true;
        }

        private static void PollLoading()
        {
            if (loading == null || !loading.IsCompleted) return;
            Task<ReplayRecording> finished = loading;
            string? name = loadingName;
            loading = null;
            loadingName = null;
            if (finished.IsFaulted)
            {
                Log.Exception(finished.Exception!.GetBaseException(), $"Loading replay '{name}' failed");
                return;
            }
            if (IsRecording || IsReplaying) return;
            try { player = new ReplayPlayer(finished.Result); }
            catch (Exception e) { Log.Exception(e, $"Starting replay '{name}' failed"); }
        }

        private static void ReadKeys(ReplayPlayer replay)
        {
            if (CasterConfig.KeyPressed(CasterConfig.ReplayPauseKey)) replay.TogglePlaying();
            if (NetworkedPlayerManager.TryReadPlayerKey(out int otherIndex))
            {
                if (otherIndex == NetworkedPlayerManager.LocalPlayerKey) replay.SpectateLocal();
                else replay.SpectateOther(otherIndex);
            }
            if (CasterConfig.KeyPressed(CasterConfig.NextPlayerKey)) replay.SpectateNext(1);
            else if (CasterConfig.KeyPressed(CasterConfig.PreviousPlayerKey)) replay.SpectateNext(-1);
        }

        private static void SaveBeforeQuit()
        {
            ExitReplay();
            StopRecording();
            if (saving == null) return;
            try
            {
                if (!saving.Wait(QuitSaveTimeout)) Log.Warning("Saving the replay did not finish before quitting.");
            }
            catch (AggregateException e) { Log.Exception(e.GetBaseException(), "Saving the replay failed"); }
        }

        protected override void OnApplicationQuit()
        {
            SaveBeforeQuit();
            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            SaveBeforeQuit();
            base.OnDestroy();
        }
    }
}
