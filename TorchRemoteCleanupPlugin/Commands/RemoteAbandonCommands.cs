using System;
using System.Globalization;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace RemoteAbandon.Commands
{
    [Category("abandon")]
    public class RemoteAbandonCommands : CommandModule
    {
        private Plugin Plugin => Plugin.Instance;

        [Command("info", "Explains the remote grid abandonment rules on this server.")]
        [Permission(MyPromoteLevel.None)]
        public void Info()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("Remote Grid Abandon is not initialized.");
                return;
            }

            if (!Plugin.Config.EnablePlayerCommands && (Context.Player?.PromoteLevel ?? MyPromoteLevel.None) < MyPromoteLevel.Admin)
            {
                Context.Respond("Player commands for Remote Grid Abandon are currently disabled.");
                return;
            }

            var cfg = Plugin.Config;
            var sb = new StringBuilder();
            sb.AppendLine("=== Remote Grid Abandon Info ===");
            sb.AppendLine($"Status: {(cfg.Enabled ? "ACTIVE" : "DISABLED")}");
            sb.AppendLine("- When you click 'X' (Remove) on a grid in the Info Tab:");
            sb.AppendLine($"  - Beacons destroyed: {(cfg.DestroyPlayerBeacons ? "YES (Your beacons removed)" : "NO")}");
            sb.AppendLine($"  - Other players' beacons: {(cfg.PreserveOtherPlayerBeacons ? "PRESERVED" : "Not preserved")}");
            sb.AppendLine($"  - Block ownership: {(cfg.ResetTerminalOwnershipToNobody ? "Wiped to Nobody" : "Retained")}");
            sb.AppendLine($"  - PCU Budget: {(cfg.TransferAuthorshipToNobody ? "Instantly refunded" : "Retained")}");
            sb.AppendLine($"  - Physical Grid: Left in space as a derelict (NOT deleted).");
            if (cfg.PreventAbandonInCombat)
            {
                sb.AppendLine($"  - Combat lockout: Cannot abandon if hostiles within {cfg.CombatCheckRadius:N0}m.");
            }
            if (cfg.MaxGridPCU > 0)
            {
                sb.AppendLine($"  - Max grid PCU: {cfg.MaxGridPCU:N0} (larger grids cannot be abandoned).");
            }

            Context.Respond(sb.ToString());
        }

        [Command("status", "Shows the current Remote Grid Abandon configuration status.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Status()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("Remote Grid Abandon plugin is not initialized.");
                return;
            }

            var cfg = Plugin.Config;
            var sb = new StringBuilder();
            sb.AppendLine("=== Remote Grid Abandon Status ===");
            sb.AppendLine($"Plugin Enabled: {cfg.Enabled}");
            sb.AppendLine($"Debug Logging: {cfg.EnableDebugLogging}");
            sb.AppendLine($"Write Dedicated Log File: {cfg.WriteDedicatedLogFile}");
            sb.AppendLine($"Player In-Game Commands: {cfg.EnablePlayerCommands}");
            sb.AppendLine($"Destroy Player Beacons: {cfg.DestroyPlayerBeacons}");
            sb.AppendLine($"Preserve Other Players' Beacons: {cfg.PreserveOtherPlayerBeacons}");
            sb.AppendLine($"Depower Grid On Abandon: {cfg.DepowerGridOnAbandon}");
            sb.AppendLine($"Reset Terminal Ownership: {cfg.ResetTerminalOwnershipToNobody}");
            sb.AppendLine($"Transfer Authorship (Refund PCU): {cfg.TransferAuthorshipToNobody}");
            sb.AppendLine($"Custom Owner ID: {cfg.CustomOwnerIdentityId}");
            sb.AppendLine($"Prevent Abandon In Combat: {cfg.PreventAbandonInCombat} (Radius: {cfg.CombatCheckRadius:N0}m)");
            sb.AppendLine($"Prevent Abandon On Damage: {cfg.PreventAbandonOnDamage} (Cooldown: {cfg.DamageCooldownSeconds}s)");
            sb.AppendLine($"Max Grid PCU Limit: {(cfg.MaxGridPCU > 0 ? cfg.MaxGridPCU.ToString() : "Unlimited")}");
            sb.AppendLine($"Send Player Notification: {cfg.SendNotificationToPlayer}");
            sb.AppendLine($"Send HUD Notification: {cfg.SendHudNotification}");
            sb.AppendLine($"Send Chat Notification: {cfg.SendChatNotification}");
            sb.AppendLine($"Abandon Success Message: {cfg.NotificationMessage}");
            sb.AppendLine($"Combat Blocked Message: {cfg.CombatBlockedMessage}");
            sb.AppendLine($"Damage Blocked Message: {cfg.DamageBlockedMessage}");
            sb.AppendLine($"PCU Exceeded Message: {cfg.PcuBlockedMessage}");

            Context.Respond(sb.ToString());
        }

        [Command("stats", "Displays real-time grid abandonment telemetry and counters.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Stats()
        {
            if (Plugin?.Statistics == null)
            {
                Context.Respond("Remote Grid Abandon statistics are not available.");
                return;
            }

            var stats = Plugin.Statistics;
            var sb = new StringBuilder();
            sb.AppendLine("=== Remote Grid Abandon Telemetry ===");
            sb.AppendLine($"Total Grids Abandoned: {stats.TotalGridsAbandoned:N0}");
            sb.AppendLine($"Total Subgrids Processed: {stats.TotalSubgridsProcessed:N0}");
            sb.AppendLine($"Total Beacons Destroyed: {stats.TotalBeaconsDestroyed:N0}");
            sb.AppendLine($"Total PCU Refunded: {stats.TotalPcuRefunded:N0}");
            sb.AppendLine($"Total Combat Lockouts: {stats.TotalCombatBlocked:N0}");
            sb.AppendLine($"Last Grid Abandoned: {stats.LastAbandonedGridName} (SteamID: {stats.LastAbandonedPlayerSteamId}) @ {stats.LastAbandonedTimestamp}");

            Context.Respond(sb.ToString());
        }

        [Command("resetstats", "Resets the Remote Grid Abandon statistics counters.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ResetStats()
        {
            Plugin?.Statistics?.Reset();
            Context.Respond("Remote Grid Abandon statistics counters have been reset to zero.");
        }

        [Command("toggle", "Toggles the Remote Grid Abandon plugin on or off.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Toggle()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("Remote Grid Abandon is not initialized.");
                return;
            }

            Plugin.Config.Enabled = !Plugin.Config.Enabled;
            Plugin.SaveConfig();
            Context.Respond($"Remote Grid Abandon Override is now {(Plugin.Config.Enabled ? "ENABLED" : "DISABLED (Vanilla full deletion active)")}.");
        }

        [Command("reload", "Reloads the Remote Grid Abandon configuration from disk.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            if (Plugin == null)
            {
                Context.Respond("Remote Grid Abandon is not initialized.");
                return;
            }

            Plugin.LoadConfig();
            Context.Respond("Remote Grid Abandon configuration reloaded from disk.");
        }

        [Command("set", "Changes a configuration setting on the fly. Usage: !abandon set <property> <value>")]
        [Permission(MyPromoteLevel.Admin)]
        public void Set(string property, string value)
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("Remote Abandon is not initialized.");
                return;
            }

            var cfg = Plugin.Config;

            bool TryParseBool(out bool boolVal)
            {
                if (bool.TryParse(value, out boolVal)) return true;
                Context.Respond($"Invalid boolean value '{value}' for property '{property}'. Expected 'true' or 'false'.");
                return false;
            }

            bool TryParseInt(out int intVal)
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intVal)) return true;
                Context.Respond($"Invalid integer value '{value}' for property '{property}'. Expected a whole number.");
                return false;
            }

            bool TryParseLong(out long longVal)
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longVal)) return true;
                Context.Respond($"Invalid integer value '{value}' for property '{property}'. Expected a 64-bit integer.");
                return false;
            }

            bool TryParseFloat(out float floatVal)
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatVal)) return true;
                Context.Respond($"Invalid numeric value '{value}' for property '{property}'. Expected a decimal number.");
                return false;
            }

            switch (property.ToLowerInvariant())
            {
                case "enabled":
                    if (!TryParseBool(out bool enabledVal)) return;
                    cfg.Enabled = enabledVal;
                    break;
                case "debug":
                case "enabledebuglogging":
                    if (!TryParseBool(out bool debugVal)) return;
                    cfg.EnableDebugLogging = debugVal;
                    break;
                case "logfile":
                case "writededicatedlogfile":
                    if (!TryParseBool(out bool logfileVal)) return;
                    cfg.WriteDedicatedLogFile = logfileVal;
                    break;
                case "playercommands":
                case "enableplayercommands":
                    if (!TryParseBool(out bool playerCmdsVal)) return;
                    cfg.EnablePlayerCommands = playerCmdsVal;
                    break;
                case "destroybeacons":
                case "destroyplayerbeacons":
                    if (!TryParseBool(out bool destroyBeaconsVal)) return;
                    cfg.DestroyPlayerBeacons = destroyBeaconsVal;
                    break;
                case "preserveotherbeacons":
                case "preserveotherplayerbeacons":
                    if (!TryParseBool(out bool preserveVal)) return;
                    cfg.PreserveOtherPlayerBeacons = preserveVal;
                    break;
                case "depower":
                case "depowergridonabandon":
                    if (!TryParseBool(out bool depowerVal)) return;
                    cfg.DepowerGridOnAbandon = depowerVal;
                    break;
                case "resetownership":
                case "resetterminalownershiptonobody":
                    if (!TryParseBool(out bool resetVal)) return;
                    cfg.ResetTerminalOwnershipToNobody = resetVal;
                    break;
                case "transferauthorship":
                case "transferauthorshiptonobody":
                    if (!TryParseBool(out bool transferVal)) return;
                    cfg.TransferAuthorshipToNobody = transferVal;
                    break;
                case "customownerid":
                    if (!TryParseLong(out long ownerIdVal)) return;
                    cfg.CustomOwnerIdentityId = ownerIdVal;
                    break;
                case "combatcheck":
                case "preventabandonincombat":
                    if (!TryParseBool(out bool combatCheckVal)) return;
                    cfg.PreventAbandonInCombat = combatCheckVal;
                    break;
                case "combatradius":
                case "combatcheckradius":
                    if (!TryParseFloat(out float radiusVal)) return;
                    cfg.CombatCheckRadius = radiusVal;
                    break;
                case "damagecheck":
                case "preventabandonondamage":
                case "ondamage":
                    if (!TryParseBool(out bool dmgCheckVal)) return;
                    cfg.PreventAbandonOnDamage = dmgCheckVal;
                    break;
                case "damagecooldown":
                case "damagecooldownseconds":
                    if (!TryParseInt(out int cooldownVal)) return;
                    cfg.DamageCooldownSeconds = cooldownVal;
                    break;
                case "maxpcu":
                case "maxgridpcu":
                    if (!TryParseInt(out int maxPcuVal)) return;
                    cfg.MaxGridPCU = maxPcuVal;
                    break;
                case "notify":
                case "sendnotificationtoplayer":
                    if (!TryParseBool(out bool notifyVal)) return;
                    cfg.SendNotificationToPlayer = notifyVal;
                    break;
                case "hudnotify":
                case "sendhudnotification":
                    if (!TryParseBool(out bool hudVal)) return;
                    cfg.SendHudNotification = hudVal;
                    break;
                case "chatnotify":
                case "sendchatnotification":
                    if (!TryParseBool(out bool chatVal)) return;
                    cfg.SendChatNotification = chatVal;
                    break;
                case "message":
                case "notificationmessage":
                    cfg.NotificationMessage = value;
                    break;
                case "combatmessage":
                case "combatblockedmessage":
                    cfg.CombatBlockedMessage = value;
                    break;
                case "damagemessage":
                case "damageblockedmessage":
                    cfg.DamageBlockedMessage = value;
                    break;
                case "pcumessage":
                case "pcublockedmessage":
                    cfg.PcuBlockedMessage = value;
                    break;
                default:
                    Context.Respond($"Unknown setting '{property}'. Valid options: enabled, debug, logfile, playercommands, destroybeacons, preserveotherbeacons, depower, resetownership, transferauthorship, customownerid, combatcheck, combatradius, damagecheck, damagecooldown, maxpcu, notify, hudnotify, chatnotify, message, combatmessage, damagemessage, pcumessage.");
                    return;
            }

            Plugin.SaveConfig();
            Context.Respond($"Remote Grid Abandon: Set '{property}' to '{value}'. Configuration saved.");
        }
    }
}

