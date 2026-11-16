// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;

namespace Zetori.DiscordRichPresence
{
    internal sealed class DiscordRPCManager : ObjectComponent
    {
        private const string ApplicationId = "1361832683411607725";

        internal static DiscordRPCWrapper? Client;

        internal void Start()
        {
            Client = new DiscordRPCWrapper();
            Client.OnReady += (_, _) =>
            {
                Log.Forget("discord-error");
                Log.Info("Connected to Discord.");
                if (CasterConfig.DiscordPresence.Value) DiscordRPCStateManager.SetLoadingPresence();
            };
            Client.OnError += message => Log.WarningOnce("discord-error", $"Discord Rich Presence: {message}");
            Client.Initialize(ApplicationId);
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            Client?.Dispose();
            Client = null;
        }
    }
}
