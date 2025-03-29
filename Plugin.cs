using System;
using HarmonyLib;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Logger = Rocket.Core.Logging.Logger;

namespace MrKan.DeathStorage
{
    public class Plugin : RocketPlugin<Config>
    {
        public static Plugin? Instance { get; private set; }
        public static Config? Config { get => Instance?.Configuration.Instance; }
        private static Harmony? Harmony;
        protected override void Load()
        {
            Instance = this;
            Harmony = new Harmony("MrKan.DeathStorage");
            Harmony.PatchAll();

            DeathStorageManager.Init();

            PlayerLife.OnPreDeath += OnPreDeath;
            PlayerLife.OnRevived_Global += OnRevived;
        }

        private void OnPreDeath(PlayerLife life)
        {
            try
            {
                DeathStorageManager.Create(life.player);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to create DroppedStorage on PreDeath");
            }
        }

        private void OnRevived(PlayerLife life)
        {
            try
            {
                DeathStorageManager.GiveStashedItems(life.player);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to give stashed items on revive");
            }
        }

        protected override void Unload()
        {
            PlayerLife.OnPreDeath -= OnPreDeath;
            PlayerLife.OnRevived_Global -= OnRevived;

            Harmony?.UnpatchAll();
            Instance = null;
        }
    }
}
