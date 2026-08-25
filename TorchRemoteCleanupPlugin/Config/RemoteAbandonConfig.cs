using System;
using Torch;
using Torch.Views;

namespace RemoteAbandon.Config
{
    public class RemoteAbandonConfig : ViewModel
    {
        // --- General Settings ---
        private bool _enabled = true;
        private bool _enableDebugLogging = false;
        private bool _enablePlayerCommands = false;
        private bool _writeDedicatedLogFile = true;

        // --- Beacon Stripping & Derelict Setup ---
        private bool _destroyPlayerBeacons = true;
        private bool _preserveOtherPlayerBeacons = true;
        private bool _depowerGridOnAbandon = false;

        // --- Ownership & PCU Management ---
        private bool _resetTerminalOwnershipToNobody = true;
        private bool _transferAuthorshipToNobody = true;
        private long _customOwnerIdentityId = 0L;

        // --- Combat & Anti-Exploit Restrictions ---
        private bool _preventAbandonInCombat = false;
        private float _combatCheckRadius = 3000.0f;
        private int _maxGridPCU = 0; // 0 = unlimited

        // --- Player Notification ---
        private bool _sendNotificationToPlayer = true;
        private string _notificationMessage = "Grid '{0}' was abandoned as a derelict. PCU refunded.";

        // --- General Settings ---
        [Display(Order = 1, Name = "Enable Plugin", GroupName = "General", Description = "Master toggle for Remote Abandon override.")]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [Display(Order = 2, Name = "Enable Debug Logging", GroupName = "General", Description = "Log detailed trace information to the Torch console.")]
        public bool EnableDebugLogging
        {
            get => _enableDebugLogging;
            set => SetValue(ref _enableDebugLogging, value);
        }

        [Display(Order = 3, Name = "Enable Player In-Game Commands", GroupName = "General", Description = "Toggle whether non-admin players can run informational in-game chat commands (!abandon info).")]
        public bool EnablePlayerCommands
        {
            get => _enablePlayerCommands;
            set => SetValue(ref _enablePlayerCommands, value);
        }

        [Display(Order = 4, Name = "Write Dedicated Log File", GroupName = "General", Description = "Append all grid abandonment events to a standalone RemoteAbandon.log file in plugin storage.")]
        public bool WriteDedicatedLogFile
        {
            get => _writeDedicatedLogFile;
            set => SetValue(ref _writeDedicatedLogFile, value);
        }

        // --- Beacon Stripping & Derelict Setup ---
        [Display(Order = 5, Name = "Destroy Player Beacons", GroupName = "Beacon Stripping", Description = "Destroy beacons built or owned by the abandoning player.")]
        public bool DestroyPlayerBeacons
        {
            get => _destroyPlayerBeacons;
            set => SetValue(ref _destroyPlayerBeacons, value);
        }

        [Display(Order = 6, Name = "Preserve Other Players' Beacons", GroupName = "Beacon Stripping", Description = "Keep beacons owned by other players/factions (e.g. scrap/claim beacons) intact.")]
        public bool PreserveOtherPlayerBeacons
        {
            get => _preserveOtherPlayerBeacons;
            set => SetValue(ref _preserveOtherPlayerBeacons, value);
        }

        [Display(Order = 7, Name = "Depower Grid On Abandon", GroupName = "Beacon Stripping", Description = "Turn off functional power blocks (reactors, batteries, solar panels) when grid is abandoned.")]
        public bool DepowerGridOnAbandon
        {
            get => _depowerGridOnAbandon;
            set => SetValue(ref _depowerGridOnAbandon, value);
        }

        // --- Ownership & PCU Management ---
        [Display(Order = 8, Name = "Reset Functional Block Ownership", GroupName = "Ownership & PCU", Description = "Reset terminal block ownership to Nobody (0L) upon abandon.")]
        public bool ResetTerminalOwnershipToNobody
        {
            get => _resetTerminalOwnershipToNobody;
            set => SetValue(ref _resetTerminalOwnershipToNobody, value);
        }

        [Display(Order = 9, Name = "Transfer Authorship To Refund PCU", GroupName = "Ownership & PCU", Description = "Transfer block authorship (BuiltBy) away from player to refund their PCU and remove from Info Tab.")]
        public bool TransferAuthorshipToNobody
        {
            get => _transferAuthorshipToNobody;
            set => SetValue(ref _transferAuthorshipToNobody, value);
        }

        [Display(Order = 10, Name = "Custom Owner / NPC Identity ID", GroupName = "Ownership & PCU", Description = "Optional Identity ID (e.g. Scrap Faction) to transfer ownership/authorship to instead of Nobody (0L). 0 to disable.")]
        public long CustomOwnerIdentityId
        {
            get => _customOwnerIdentityId;
            set => SetValue(ref _customOwnerIdentityId, Math.Max(0L, value));
        }

        // --- Combat & Anti-Exploit Restrictions ---
        [Display(Order = 11, Name = "Prevent Abandon In Combat", GroupName = "Combat & Anti-Exploit", Description = "Block players from abandoning grids if enemies or hostile players are nearby.")]
        public bool PreventAbandonInCombat
        {
            get => _preventAbandonInCombat;
            set => SetValue(ref _preventAbandonInCombat, value);
        }

        [Display(Order = 12, Name = "Combat Check Radius (Meters)", GroupName = "Combat & Anti-Exploit", Description = "Radius in meters around the grid to scan for hostile players when Prevent Abandon In Combat is enabled.")]
        public float CombatCheckRadius
        {
            get => _combatCheckRadius;
            set => SetValue(ref _combatCheckRadius, Math.Max(100.0f, value));
        }

        [Display(Order = 13, Name = "Max Grid PCU Limit", GroupName = "Combat & Anti-Exploit", Description = "Maximum PCU of a grid permitted to be remotely abandoned (0 = unlimited).")]
        public int MaxGridPCU
        {
            get => _maxGridPCU;
            set => SetValue(ref _maxGridPCU, Math.Max(0, value));
        }

        // --- Player Notification ---
        [Display(Order = 14, Name = "Send Notification To Player", GroupName = "Notifications", Description = "Display on-screen HUD/chat notification to player when grid is abandoned.")]
        public bool SendNotificationToPlayer
        {
            get => _sendNotificationToPlayer;
            set => SetValue(ref _sendNotificationToPlayer, value);
        }

        [Display(Order = 15, Name = "Notification Message Template", GroupName = "Notifications", Description = "Format string for player notification. {0} will be replaced with the Grid Name.")]
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetValue(ref _notificationMessage, value);
        }
    }
}

