
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using Logger = Rocket.Core.Logging.Logger;

namespace MrKan.DeathStorage
{
    public class DroppedStorage
    {
        private static readonly ClientInstanceMethod SendClothingState = ClientInstanceMethod.Get(typeof(PlayerClothing), "ReceiveClothingState");

        public CSteamID Owner { get; }
        public BarricadeDrop Drop { get; }
        public Items Items { get; }
        public DateTime DroppedAt { get; }

        public delegate void TakingItemsFromInventory(DroppedStorage storage, PlayerInventory inventory);
        public static event TakingItemsFromInventory? OnTakingItemsFromInventory;
        internal DroppedStorage(CSteamID owner, BarricadeDrop drop)
        {
            Owner = owner;
            Drop = drop;
            DroppedAt = DateTime.Now;

            if (Drop.interactable is not InteractableStorage storage)
            {
                throw new ArgumentException("Drop must be a storage drop");
            }

            Items = storage.items;
            Items.resize(15, 250);
        }

        public void TakeItemsFromInventory(PlayerInventory inventory)
        {
            try
            {
                OnTakingItemsFromInventory?.Invoke(this, inventory);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to invoke OnTakingItemsFromInventory");
            }

            for (byte page = 0; page < PlayerInventory.STORAGE; page++)
            {
                var pageItems = inventory.items.ElementAtOrDefault(page);
                if (pageItems == null || pageItems.getItemCount() == 0)
                {
                    continue;
                }

                for (int index = pageItems.items.Count - 1; index >= 0; index--)
                {
                    var item = pageItems.getItem((byte)index);
                    if (item == null)
                    {
                        continue;
                    }
                    if (!Plugin.Config!.DontLoseOnDeath.Contains(item.item.id))
                    {
                        Items.tryAddItem(item.item);
                        inventory.removeItem(page, (byte)index);
                    }
                }
            }
        }

        public void TakeClothesFromInventory(PlayerClothing clothing)
        {
            GetClothesAndPages(clothing, out bool[] clothes, out byte[] availablePages);

            // backpack
            if (!clothes[0])
            {
                MoveItemsFromPage(clothing.player.inventory, PlayerInventory.BACKPACK, availablePages);

                Items.tryAddItem(new Item(clothing.backpack, 1, clothing.backpackQuality, clothing.backpackState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("backpackAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.backpackQuality = 0;
                clothing.backpackState = new byte[0];
            }

            // vest
            if (!clothes[1])
            {
                MoveItemsFromPage(clothing.player.inventory, PlayerInventory.VEST, availablePages);

                Items.tryAddItem(new Item(clothing.vest, 1, clothing.vestQuality, clothing.vestState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("vestAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.vestQuality = 0;
                clothing.vestState = new byte[0];
            }

            // shirt
            if (!clothes[2])
            {
                MoveItemsFromPage(clothing.player.inventory, PlayerInventory.SHIRT, availablePages);

                Items.tryAddItem(new Item(clothing.shirt, 1, clothing.shirtQuality, clothing.shirtState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("shirtAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.shirtQuality = 0;
                clothing.shirtState = new byte[0];
            }

            // pants
            if (!clothes[3])
            {
                MoveItemsFromPage(clothing.player.inventory, PlayerInventory.PANTS, availablePages);

                Items.tryAddItem(new Item(clothing.pants, 1, clothing.pantsQuality, clothing.pantsState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("pantsAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.pantsQuality = 0;
                clothing.pantsState = new byte[0];
            }

            // hat
            if (!clothes[4])
            {
                Items.tryAddItem(new Item(clothing.hat, 1, clothing.hatQuality, clothing.hatState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("hatAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.hatQuality = 0;
                clothing.hatState = new byte[0];
            }

            // glasses
            if (!clothes[5])
            {
                Items.tryAddItem(new Item(clothing.glasses, 1, clothing.glassesQuality, clothing.glassesState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("glassesAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.glassesQuality = 0;
                clothing.glassesState = new byte[0];
            }

            // mask
            if (!clothes[6])
            {
                Items.tryAddItem(new Item(clothing.mask, 1, clothing.maskQuality, clothing.maskState));
                PropertyInfo property = typeof(HumanClothes).GetProperty("maskAsset");
                property.GetSetMethod(true).Invoke(clothing.thirdClothes, new object?[] { null });
                clothing.maskQuality = 0;
                clothing.maskState = new byte[0];
            }

            SendClothingState.InvokeAndLoopback(clothing.GetNetId(), ENetReliability.Reliable, Provider.EnumerateClients_Remote(), (writer) => WriteClothingState(clothing, writer));

        }

        public void FinalizeStorage()
        {
            int totalWidth = 0, totalHeight = 0;
            foreach (var item in Items.items)
            {
                int w;
                int h;
                if (item.rot % 2 == 0)
                {
                    w = item.size_x;
                    h = item.size_y;
                }
                else
                {
                    w = item.size_y;
                    h = item.size_x;
                }

                totalWidth = Math.Max(totalWidth, item.x + w);
                totalHeight = Math.Max(totalHeight, item.y + h);
            }

            Items.resize((byte)totalWidth, (byte)totalHeight);
        }

        private void GetClothesAndPages(PlayerClothing cloting, out bool[] clothes, out byte[] pages)
        {
            List<byte> availablePages = new() { PlayerInventory.SLOTS };

            clothes = new bool[7];
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.backpack)) clothes[0] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.vest)) clothes[1] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.shirt)) clothes[2] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.pants)) clothes[3] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.hat)) clothes[4] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.glasses)) clothes[5] = true;
            if (Plugin.Config!.DontLoseOnDeath.Contains(cloting.mask)) clothes[6] = true;

            for (int index = 0; index < 4; index++)
            {
                if (clothes[index]) availablePages.Add((byte)(index + 3));
            }

            availablePages.Add(PlayerInventory.SLOTS);
            pages = availablePages.ToArray();
        }

        private void MoveItemsFromPage(PlayerInventory inventory, byte page, byte[] availablePages)
        {
            var pageItems = inventory.items[page];
            if (pageItems.getItemCount() == 0) return;

            for (int index = pageItems.getItemCount() - 1; index > -1; index--)
            {
                var itemJar = pageItems.getItem((byte)index);
                if (itemJar == null) continue;

                bool found = false;
                foreach (byte targetPage in availablePages)
                {
                    var targetItems = inventory.items[targetPage];
                    if (targetItems.tryFindSpace(itemJar.size_x, itemJar.size_y, out byte x, out byte y, out byte rot))
                    {
                        inventory.ReceiveDragItem(page, itemJar.x, itemJar.y, targetPage, x, y, rot);
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    DeathStorageManager.StashItem(inventory.player.channel.owner.playerID.steamID.m_SteamID, itemJar.item);
                    inventory.removeItem(page, (byte)index);
                }
            }
        }

        private static void WriteClothingState(PlayerClothing cl, NetPakWriter writer)
        {
            writer.WriteGuid(cl.shirtAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.shirtQuality);
            writer.WriteStateArray(cl.shirtState);
            writer.WriteGuid(cl.pantsAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.pantsQuality);
            writer.WriteStateArray(cl.pantsState);
            writer.WriteGuid(cl.hatAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.hatQuality);
            writer.WriteStateArray(cl.hatState);
            writer.WriteGuid(cl.backpackAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.backpackQuality);
            writer.WriteStateArray(cl.backpackState);
            writer.WriteGuid(cl.vestAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.vestQuality);
            writer.WriteStateArray(cl.vestState);
            writer.WriteGuid(cl.maskAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.maskQuality);
            writer.WriteStateArray(cl.maskState);
            writer.WriteGuid(cl.glassesAsset?.GUID ?? Guid.Empty);
            writer.WriteUInt8(cl.glassesQuality);
            writer.WriteStateArray(cl.glassesState);
            writer.WriteBit(cl.isVisual);
            writer.WriteBit(cl.isSkinned);
            writer.WriteBit(cl.isMythic);
        }

        public bool IsExpired()
        {
            if (Plugin.Config == null)
            {
                return false;
            }

            var delta = (DateTime.Now - DroppedAt).TotalSeconds;
            return delta >= Plugin.Config.BarricadeLifetime;
        }

        public void Destroy()
        {
            Items.clear();

            BarricadeManager.tryGetRegion(Drop.model, out byte x, out byte y, out ushort plant, out _);
            BarricadeManager.destroyBarricade(Drop, x, y, plant);
        }
    }
}