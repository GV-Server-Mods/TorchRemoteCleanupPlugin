using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using RemoteAbandon.Config;
using RemoteAbandon.Services;
using RemoteAbandon.Views;
using HarmonyLib;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;

namespace RemoteAbandon
{
    public class Plugin : TorchPluginBase, IWpfPlugin, INotifyPropertyChanged
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Persistent<RemoteAbandonConfig> _config;
        private RemoteAbandonControl _control;
        private Harmony _harmony;

        public static Plugin Instance { get; private set; }

        public RemoteAbandonConfig Config => _config?.Data;
        public RemoteAbandonStatistics Statistics { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            LoadConfig();
            Statistics = new RemoteAbandonStatistics();

            try
            {
                _harmony = new Harmony("com.torch.remoteabandon");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Info("Remote Abandon plugin initialized and Harmony patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply Harmony patches for Remote Abandon!");
            }
        }

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

        public void SaveConfig()
        {
            try
            {
                _config?.Save();
                Log.Info("Remote Abandon configuration saved.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save Remote Abandon configuration!");
            }
        }

        public UserControl GetControl()
        {
            return _control ?? (_control = new RemoteAbandonControl(this));
        }

        public override void Dispose()
        {
            try
            {
                _harmony?.UnpatchAll("com.torch.remoteabandon");
                Log.Info("Remote Abandon plugin unpatched.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error unpatching Remote Abandon.");
            }

            Instance = null;
            base.Dispose();
        }
    }
}