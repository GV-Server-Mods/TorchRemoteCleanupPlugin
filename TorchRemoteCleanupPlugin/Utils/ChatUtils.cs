using System;
using RemoteAbandon.Config;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRageMath;

namespace RemoteAbandon.Utils
{
    /// <summary>
    /// Utility methods for dispatching HUD toast notifications and in-game chat messages.
    /// </summary>
    public static class ChatUtils
    {
        public const string Prefix = "RemoteAbandon";

        /// <summary>
        /// Sends an on-screen HUD toast notification to a specific player.
        /// </summary>
        public static void SendNotificationToPlayer(ulong steamId, string message, int displayTimeMs = 5000, string font = null)
        {
            if (steamId == 0 || string.IsNullOrEmpty(message)) return;

            TorchBase.Instance?.Invoke(() =>
            {
                try
                {
                    var msg = new NotificationMessage(message, displayTimeMs, font ?? MyFontEnum.Green);
                    ModCommunication.SendMessageTo(msg, steamId);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn(ex, $"Failed to send HUD notification to SteamID {steamId}");
                }
            });
        }

        /// <summary>
        /// Sends a direct in-game chat message to a player's chat log window.
        /// </summary>
        public static void SendChatMessageToPlayer(ulong steamId, string message, Color? color = null, string font = null)
        {
            if (steamId == 0 || string.IsNullOrEmpty(message)) return;

            TorchBase.Instance?.Invoke(() =>
            {
                try
                {
                    var chatManager = TorchBase.Instance.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
                    if (chatManager != null)
                    {
                        Color msgColor = color ?? (font == MyFontEnum.Red ? Color.Red : Color.Green);
                        chatManager.SendMessageAsOther(Prefix, message, msgColor, steamId, font ?? MyFontEnum.White);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn(ex, $"Failed to send chat message to SteamID {steamId}");
                }
            });
        }

        /// <summary>
        /// Dispatches feedback to HUD, in-game chat, or both based on configuration.
        /// </summary>
        public static void SendPlayerFeedback(RemoteAbandonConfig config, ulong steamId, string message, bool isError = false, int displayTimeMs = 5000)
        {
            if (config == null || !config.SendNotificationToPlayer || steamId == 0 || string.IsNullOrEmpty(message))
                return;

            string font = isError ? MyFontEnum.Red : MyFontEnum.Green;
            Color chatColor = isError ? Color.Red : Color.Green;

            if (config.SendHudNotification)
            {
                SendNotificationToPlayer(steamId, message, displayTimeMs, font);
            }

            if (config.SendChatNotification)
            {
                SendChatMessageToPlayer(steamId, message, chatColor, font);
            }
        }
    }
}

