using System;
using Torch;
using Torch.API;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;

namespace RemoteAbandon.Utils
{
    public static class ChatUtils
    {
        public const string Prefix = "RemoteAbandon";

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
                    Plugin.Log.Warn(ex, $"Failed to send notification to SteamID {steamId}");
                }
            });
        }
    }
}

