using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using Logger = Rocket.Core.Logging.Logger;

namespace MrKan.DeathStorage
{
    [HarmonyPatch]
    public static class PlayerPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerInventory), "onLifeUpdated")]
        public static bool InventoryPostfix(bool isDead, PlayerInventory __instance)
        {
            if (Plugin.Instance == null)
            {
                return true;
            }

            if (!isDead) return true;

            if ((Provider.isServer || __instance.channel.isOwner) && isDead)
            {
                __instance.closeStorage();
            }

            if (!Provider.isServer)
            {
                return false;
            }

            var storage = DeathStorageManager.GetStorage(__instance.player.channel.owner.playerID.steamID);
            if (storage == null)
            {
                try
                {
                    storage = DeathStorageManager.Create(__instance.player);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to create DroppedStorage");
                    return true;
                }

                Logger.LogError("Existing DroppedStorage not found so the new one was created");
            }


            try
            {
                storage.TakeItemsFromInventory(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to take items from inventory");
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerClothing), "onLifeUpdated")]
        public static bool onLifeUpdated(bool isDead, PlayerClothing __instance)
        {
            if (Plugin.Instance == null)
            {
                return true;
            }

            if (!isDead) return true;

            var storage = DeathStorageManager.GetStorage(__instance.player.channel.owner.playerID.steamID);
            if (storage == null)
            {
                try
                {
                    storage = DeathStorageManager.Create(__instance.player);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to create DroppedStorage");
                    return true;
                }

                Logger.LogError("Existing DroppedStorage not found so the new one was created");
            }

            try
            {
                storage.TakeClothesFromInventory(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to take clothes from inventory");
            }

            return false;
        }
    }
}