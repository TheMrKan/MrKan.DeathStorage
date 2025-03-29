using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace MrKan.DeathStorage
{
    public static class DeathStorageManager
    {
        private static ItemBarricadeAsset? s_Asset;
        private static List<DroppedStorage> s_Storages = new();
        private static Dictionary<ulong, List<Item>> s_StashedItems = new();
        public static void Init()
        {
            s_Asset = Assets.find(EAssetType.ITEM, Plugin.Config!.BarricadeId) as ItemBarricadeAsset;
            if (s_Asset == null)
            {
                Logger.LogWarning($"Failed to find barricade asset {Plugin.Config!.BarricadeId}.");
            }

            Plugin.Instance!.StartCoroutine(StorageLifetimeChecker());
        }

        public static DroppedStorage Create(Player player)
        {
            var rot = player.transform.rotation.eulerAngles.y;
            return Create(player.channel.owner.playerID.steamID, player.transform.position, rot);
        }

        public static DroppedStorage Create(CSteamID owner, Vector3 position, float rotation)
        {
            if (s_Asset == null)
            {
                throw new Exception("Barricade asset not found");
            }

            var barricade = new Barricade(s_Asset);
            var transform = BarricadeManager.dropBarricade(barricade, null, position, 0, rotation, 0, 0, 0);
            var drop = BarricadeManager.FindBarricadeByRootTransform(transform);

            var storage = new DroppedStorage(owner, drop);
            s_Storages.Add(storage);
            return storage;
        }

        public static DroppedStorage GetStorage(CSteamID owner)
        {
            return s_Storages.LastOrDefault(s => s.Owner == owner);
        }

        public static void StashItem(ulong playerId, Item item)
        {
            if (!s_StashedItems.TryGetValue(playerId, out var stash))
            {
                stash = new();
                s_StashedItems[playerId] = stash;
            }

            stash.Add(item);
        }

        public static void GiveStashedItems(Player player)
        {
            if (!s_StashedItems.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out var stash))
            {
                return;
            }
            s_StashedItems.Remove(player.channel.owner.playerID.steamID.m_SteamID);

            foreach (var item in stash)
            {
                player.inventory.forceAddItem(item, false);
            }
        }

        private static IEnumerator StorageLifetimeChecker()
        {
            while (Plugin.Instance != null)
            {
                try
                {
                    DestroyExpiredStorages();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "An error occured in StorageLifetimeChecker");
                }

                yield return new WaitForSeconds(10);
            }
        }

        private static void DestroyExpiredStorages()
        {
            for (int i = s_Storages.Count - 1; i >= 0; i--)
            {
                var storage = s_Storages[i];
                if (storage.IsExpired())
                {
                    storage.Destroy();
                }
                s_Storages.RemoveAt(i);
            }
        }
    }
}