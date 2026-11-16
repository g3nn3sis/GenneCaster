// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Collections.Concurrent;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Zetori.DiscordRichPresence
{
    internal sealed class DiscordRPCWrapper : IDisposable
    {
        private DiscordRpcClient? client;
        private readonly object sync = new object();
        private CancellationTokenSource? cts;
        private Task? worker;
        private readonly ConcurrentQueue<Action> presenceQueue = new();
        private bool disposed;
        private bool isConnecting;
        private int reconnectAttempt;

        internal string ApplicationId { get; private set; } = string.Empty;
        internal string Username { get; private set; } = "Unknown";
        internal string UserId { get; private set; } = "Unknown";
        internal bool IsConnected => client is { IsInitialized: true } && !disposed;

        internal event Action<string, string>? OnReady;
        internal event Action<string>? OnError;
        internal event Action? OnDisconnected;

        private readonly DateTime sessionStart = DateTime.UtcNow;

        internal bool Initialize(string applicationId, int pipe = 0)
        {
            lock (sync)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(DiscordRPCWrapper));

                if (worker != null)
                    return false;

                ApplicationId = applicationId;

                cts = new CancellationTokenSource();
                worker = Task.Run(() => WorkerLoop(pipe, cts.Token));

                return true;
            }
        }

        private async Task WorkerLoop(int pipe, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (client == null || !client.IsInitialized)
                    {
                        await TryConnect(pipe);
                    }

                    ProcessQueue();
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                    await HandleReconnectDelay(token);
                }
            }
        }

        private async Task TryConnect(int pipe)
        {
            if (isConnecting || disposed)
                return;

            isConnecting = true;

            try
            {
                client?.Dispose();

                client = new DiscordRpcClient(ApplicationId, pipe)
                {
                    Logger = new DiscordLogBridge { Level = LogLevel.Warning }
                };

                client.OnReady += (_, e) =>
                {
                    Username = e.User.Username;
                    UserId = e.User.ID.ToString();
                    reconnectAttempt = 0;

                    OnReady?.Invoke(Username, UserId);
                };

                client.OnError += (_, e) =>
                    OnError?.Invoke(e.Message);

                client.OnConnectionFailed += (_, _) =>
                    OnDisconnected?.Invoke();

                client.Initialize();
                reconnectAttempt = 0;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                await HandleReconnectDelay(CancellationToken.None);
            }
            finally
            {
                isConnecting = false;
            }
        }

        private async Task HandleReconnectDelay(CancellationToken token)
        {
            reconnectAttempt++;

            int delay = Math.Min(
                30000,
                1000 * (int)Math.Pow(2, reconnectAttempt)
            );

            await Task.Delay(delay, token);
        }

        private void ProcessQueue()
        {
            if (!IsConnected || client == null)
                return;

            while (presenceQueue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }
        }

        private readonly RichPresence currentPresence = new()
        {
            Assets = new Assets(),
            Party = new Party(),
            Buttons = Array.Empty<Button>(),
            Timestamps = new Timestamps()
        };

        internal void UpdatePresence(Action<RichPresence> updateAction)
        {
            presenceQueue.Enqueue(() =>
            {
                if (!IsConnected || client == null)
                    return;

                try
                {
                    updateAction(currentPresence);

                    currentPresence.Timestamps ??= new Timestamps();

                    if (currentPresence.Timestamps.Start == null)
                        currentPresence.Timestamps.Start = sessionStart;

                    client.SetPresence(currentPresence);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            });
        }

        internal void UpdatePresence(
            string? details = null,
            string? state = null,
            string? largeImageKey = null,
            string? largeImageText = null,
            string? smallImageKey = null,
            string? smallImageText = null,
            string? partyId = null,
            int? partyMax = null,
            int? partySize = null,
            Button[]? buttons = null,
            bool clearParty = false)
        {
            UpdatePresence(p =>
            {
                if (details != null)
                    p.Details = details;

                if (state != null)
                    p.State = state;

                p.Assets ??= new Assets();

                if (clearParty)
                    p.Party = new Party();
                else
                    p.Party ??= new Party();

                if (largeImageKey != null)
                    p.Assets.LargeImageKey = largeImageKey;

                if (largeImageText != null)
                    p.Assets.LargeImageText = largeImageText;

                if (smallImageKey != null)
                    p.Assets.SmallImageKey = smallImageKey;

                if (smallImageText != null)
                    p.Assets.SmallImageText = smallImageText;

                if (partyId != null)
                    p.Party.ID = partyId;

                if (partyMax != null)
                    p.Party.Max = partyMax.Value;

                if (partySize != null)
                    p.Party.Size = partySize.Value;

                if (buttons != null)
                    p.Buttons = buttons;
            });
        }

        internal void ClearPresence()
        {
            if (!IsConnected || client == null)
                return;

            try
            {
                client.ClearPresence();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        internal void Shutdown()
        {
            lock (sync)
            {
                if (disposed)
                    return;

                try
                {
                    cts?.Cancel();
                    client?.ClearPresence();
                    client?.Dispose();
                    client = null;

                    Username = "Unknown";
                    UserId = "Unknown";
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }
        }

        public void Dispose()
        {
            Shutdown();

            disposed = true;

            try
            {
                cts?.Dispose();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }

            GC.SuppressFinalize(this);
        }
    }
}
