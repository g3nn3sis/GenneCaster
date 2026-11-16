// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Zerori.Base
{
    internal static class KeyBinds
    {
        private const int NotListening = -1;

        private static KeyBind[]? all;
        private static int listening = NotListening;
        private static int capturedFrame = -1;

        internal static KeyBind[] All => all ??= new[]
        {
            new KeyBind("Menu", CasterConfig.MenuKey),
            new KeyBind("Next Player", CasterConfig.NextPlayerKey),
            new KeyBind("Previous Player", CasterConfig.PreviousPlayerKey),
            new KeyBind("Hold To Talk", CasterConfig.PushToTalkKey),
            new KeyBind("Orbit Left", CasterConfig.OrbitLeftKey),
            new KeyBind("Orbit Right", CasterConfig.OrbitRightKey),
            new KeyBind("Orbit Up", CasterConfig.OrbitUpKey),
            new KeyBind("Orbit Down", CasterConfig.OrbitDownKey),
            new KeyBind("Reset Orbit", CasterConfig.OrbitResetKey),
            new KeyBind("Web Next Node", CasterConfig.WebNextNodeKey),
            new KeyBind("Web Previous Node", CasterConfig.WebPreviousNodeKey),
            new KeyBind("Web Place Node", CasterConfig.WebPlaceNodeKey),
            new KeyBind("Web Remove Node", CasterConfig.WebRemoveNodeKey),
            new KeyBind("Fly Forward", CasterConfig.FlyForwardKey),
            new KeyBind("Fly Back", CasterConfig.FlyBackKey),
            new KeyBind("Fly Left", CasterConfig.FlyLeftKey),
            new KeyBind("Fly Right", CasterConfig.FlyRightKey),
            new KeyBind("Fly Up", CasterConfig.FlyUpKey),
            new KeyBind("Fly Down", CasterConfig.FlyDownKey),
            new KeyBind("No Clip", CasterConfig.NoClipKey),
            new KeyBind("Replay Pause", CasterConfig.ReplayPauseKey),
            new KeyBind("Replay Timeline", CasterConfig.ReplayTimelineKey),
            new KeyBind("Timer Start", CasterConfig.ScoreboardStartKey),
            new KeyBind("Timer Pause", CasterConfig.ScoreboardPauseKey),
            new KeyBind("Timer Reset", CasterConfig.ScoreboardResetKey),
            new KeyBind("Team A Score Up", CasterConfig.ScoreboardTeamAUpKey),
            new KeyBind("Team A Score Down", CasterConfig.ScoreboardTeamADownKey),
            new KeyBind("Team B Score Up", CasterConfig.ScoreboardTeamBUpKey),
            new KeyBind("Team B Score Down", CasterConfig.ScoreboardTeamBDownKey)
        };

        internal static bool Capturing => listening != NotListening || Time.frameCount <= capturedFrame;

        internal static bool IsListening(int index) => listening == index;

        internal static void ToggleListening(int index) => listening = listening == index ? NotListening : index;

        internal static void ResetAll()
        {
            listening = NotListening;
            foreach (KeyBind bind in All) bind.Entry.Value = (Key)bind.Entry.DefaultValue;
        }

        internal static void Tick()
        {
            if (listening == NotListening) return;
            Keyboard? keyboard = Keyboard.current;
            if (keyboard == null) return;
            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control == null || !control.wasPressedThisFrame) continue;
                All[listening].Entry.Value = control.keyCode;
                listening = NotListening;
                capturedFrame = Time.frameCount;
                return;
            }
        }
    }
}
