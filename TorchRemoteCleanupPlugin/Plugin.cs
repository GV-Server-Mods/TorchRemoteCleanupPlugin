using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using RemoteAbandon.Config;
using RemoteAbandon.Services;
using RemoteAbandon.Views;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers.PatchManager;

namespace RemoteAbandon
{
    /// <summary>
    /// Core Torch plugin class for RemoteAbandon, handling lifecycle, configuration, and patch registration.
    /// </summary>
    public class Plugin : TorchPluginBase, IWpfPlugin, INotifyPropertyChanged
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Persistent<RemoteAbandonConfig> _config;
        private RemoteAbandonControl _control;

        public static Plugin Instance { get; private set; }

        public RemoteAbandonConfig Config => _config?.Data;
        public RemoteAbandonStatistics Statistics { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes the plugin, loads persistent configuration, and applies Torch PatchManager patches.
        /// </summary>
        /// <param name="torch">The Torch server instance.</param>
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            LoadConfig();
            Statistics = new RemoteAbandonStatistics();

            try
            {
                if (Torch.Managers.GetManager(typeof(ITorchSessionManager)) is ITorchSessionManager sessionManager)
                {
                    sessionManager.SessionStateChanged += OnSessionStateChanged;
                }

                if (Torch.Managers.GetManager(typeof(PatchManager)) is PatchManager patchManager)
                {
                    var ctx = patchManager.AcquireContext();
                    RemoteAbandonPatch.Patch(ctx);
                    patchManager.Commit();
                    Log.Info("Remote Abandon plugin initialized and Torch patches applied successfully.");
                }
                else
                {
                    Log.Error("Torch PatchManager not found! Unable to register Remote Abandon patch.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply patches for Remote Abandon!");
            }
        }

        private void OnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Loaded)
            {
                DamageTracker.Init();
            }
            else if (newState == TorchSessionState.Unloading)
            {
                DamageTracker.Cleanup();
            }
        }

        /// <summary>
        /// Loads the XML configuration from the plugin storage folder or creates default settings.
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(StoragePath, "RemoteAbandon.cfg");
                _config = Persistent<RemoteAbandonConfig>.Load(configPath);
                if (_config?.Data == null)
                {
                    _config = new Persistent<RemoteAbandonConfig>(configPath, new RemoteAbandonConfig());
                    _config.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load RemoteAbandon configuration! Creating default config.");
                _config = new Persistent<RemoteAbandonConfig>(Path.Combine(StoragePath, "RemoteAbandon.cfg"), new RemoteAbandonConfig());
            }

            OnPropertyChanged(nameof(Config));
        }

        /// <summary>
        /// Saves current configuration settings to disk.
        /// </summary>
        public bool SaveConfig()
        {
            try
            {
                _config?.Save();
                Log.Info("Remote Abandon configuration saved.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save Remote Abandon configuration!");
                return false;
            }
        }

        /// <summary>
        /// Returns the WPF user interface control for the Torch server UI.
        /// </summary>
        /// <returns>WPF UserControl for the plugin tab.</returns>
        public UserControl GetControl()
        {
            return _control ??= new RemoteAbandonControl(this);
        }

        /// <summary>
        /// Cleans up plugin resources and releases references upon server shutdown or plugin unload.
        /// </summary>
        public override void Dispose()
        {
            if (Torch?.Managers?.GetManager(typeof(ITorchSessionManager)) is ITorchSessionManager sessionManager)
            {
                sessionManager.SessionStateChanged -= OnSessionStateChanged;
            }

            DamageTracker.Cleanup();
            Instance = null;
            base.Dispose();
        }
    }
}