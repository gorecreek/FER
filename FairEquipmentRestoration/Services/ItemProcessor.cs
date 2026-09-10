using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Exceptions;
using FairEquipmentRestoration.Extensions;
using FairEquipmentRestoration.Models;
using Microsoft.Extensions.Logging;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services.Commerce;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class ItemProcessor(
        ISptLogger<ItemProcessor> logger,
        ItemLogHelper itemLogHelper,
        InventoryHelper inventoryHelper,
        LostOnDeathHelper lostOnDeathHelper,
        MailSendService mailService,
        FairEquipmentRestorationConfig modConfig)
    {
        public void ApplyItemChanges(
            PmcData restoredProfile,
            PmcData preRaidProfile,
            PmcData postRaidProfile,
            InventoryDiffAnalysis analysis,
            MongoId sessionId)
        {
            logger.Debug("Applying item changes to restored inventory...");

            var ctx = InventoryContext
                .Builder()
                .Add(Inventory.Restored, restoredProfile.Inventory?.Items)
                .Add(Inventory.PreRaid, preRaidProfile.GetEquipmentItems())
                .Add(Inventory.PostRaid, postRaidProfile.GetEquipmentItems())
                .Build();

            UpdateSurvivingItems(ctx);

            var allUnableToReturnItemIds = analysis.OrphanedItemIds
                .Union(analysis.LostUnableToReturnItemIds)
                .ToHashSet();

            SendUnableToReturnItems(allUnableToReturnItemIds, restoredProfile, sessionId);
            RemoveLostItems(analysis.LostItemIds, restoredProfile, sessionId);
        }

        private void UpdateSurvivingItems(InventoryContext ctx)
        {
            logger.Debug("Updating surviving items...");

            var preRaidInventory = ctx.Get(Inventory.PreRaid);
            foreach (var preRaidItem in preRaidInventory)
            {
                var postRaidItem = ctx.GetItem(Inventory.PostRaid, preRaidItem.Id);
                if (postRaidItem is null)
                {
                    continue;
                }

                var itemToUpdate = ctx.GetItem(Inventory.Restored, preRaidItem.Id);
                if (itemToUpdate is null)
                {
                    logger.Warning($"Failed to find pre-raid item {preRaidItem.Id} in restored inventory");
                    continue;
                }

                UpdateItem(itemToUpdate, postRaidItem, ctx.Get(Inventory.Restored), ctx.Get(Inventory.PostRaid));
            }
        }

        private void UpdateItem(
            Item item,
            Item otherItem,
            IReadOnlyList<Item> inventory,
            IReadOnlyList<Item> otherInventory)
        {
            itemLogHelper.LogItem(LogLevel.Debug, item, inventory, "Updating item:");
            itemLogHelper.LogItem(LogLevel.Debug, otherItem, otherInventory, "with other item:");

            var stackCountToSet = GetStackCountToSet(item.Upd?.StackObjectsCount, otherItem.Upd?.StackObjectsCount);

            var originalFiR = item.Upd?.SpawnedInSession;
            var newFiR = otherItem.Upd?.SpawnedInSession;
            var firToSet = modConfig.RestoreFoundInRaid
                ? originalFiR
                : newFiR;

            if (!modConfig.RestoreItemCondition)
            {
                item.Upd = otherItem.Upd;
            }

            if (item.Upd is not null)
            {
                item.Upd.StackObjectsCount = stackCountToSet;
                item.Upd.SpawnedInSession = firToSet;
            }
            else
            {
                item.Upd = new Upd
                {
                    StackObjectsCount = stackCountToSet,
                    SpawnedInSession = firToSet
                };
            }
        }

        private double? GetStackCountToSet(double? originalStack, double? newStack)
        {
            double? minStack = null;
            if (originalStack is null) minStack = newStack;
            if (newStack is null) minStack = originalStack;
            if (originalStack is not null && newStack is not null)
            {
                minStack = Math.Min(originalStack.Value, newStack.Value);
            }

            // Treat items in stack as lost and restore them if restoreLostItems is true
            var stackCountToSet = modConfig.RestoreLostItems
                ? originalStack
                : minStack;

            return stackCountToSet;
        }

        private void RemoveLostItems(IReadOnlySet<MongoId> itemIds, PmcData profile, MongoId sessionId)
        {
            logger.Debug("Removing lost items...");

            foreach (var itemId in itemIds)
            {
                if (modConfig.EnableItemDebugLogging)
                {
                    logger.Debug($"Removing lost pre-raid item: {itemId}");
                }

                inventoryHelper.RemoveItem(profile, itemId, sessionId);
            }
        }

        private void SendUnableToReturnItems(IReadOnlySet<MongoId> itemIds, PmcData profile, MongoId sessionId)
        {
            logger.Debug("Sending items that can't return normally...");

            var inventory = (profile.Inventory?.Items)
                ?? throw new InvalidInventoryException();

            var items = inventory
                .Where(x => itemIds.Contains(x.Id))
                .ToList();

            SendItemsToPlayer(items, inventory, sessionId);
        }

        private void SendItemsToPlayer(List<Item> items, List<Item> inventory, string sessionId)
        {
            if (items.Count == 0)
            {
                return;
            }

            itemLogHelper.LogItems(LogLevel.Debug, items, inventory, "Items to be sent over mail:");

            logger.Debug("Sending items in mail to player...");
            mailService.SendSystemMessageToPlayer(sessionId, string.Empty, items);
        }

        public void RemoveLostQuestItems(
            PmcData restoredProfile,
            PmcData preRaidProfile,
            PmcData postRaidProfile,
            MongoId sessionId)
        {
            logger.Debug("Removing lost quest items...");

            var ctx = InventoryContext
                .Builder()
                .Add(Inventory.Restored, restoredProfile.Inventory?.Items)
                .Add(Inventory.PreRaid, [.. preRaidProfile.GetQuestItemsInProfile()])
                .Add(Inventory.PostRaid, [.. postRaidProfile.GetQuestItemsInProfile()])
                .Build();

            foreach (var preRaidItem in ctx.Get(Inventory.PreRaid))
            {
                var postRaidItem = ctx.GetItem(Inventory.PostRaid, preRaidItem.Id);
                if (postRaidItem is null)
                {
                    // Quest items lost in raid always need to be removed
                    itemLogHelper.LogItem(LogLevel.Debug, preRaidItem, ctx.Get(Inventory.Restored), "Removing pre-raid quest item:");
                    inventoryHelper.RemoveItem(restoredProfile, preRaidItem.Id, sessionId);
                }
            }
        }

        public void RestoreFiRStatusOnItems(PmcData profile, IReadOnlySet<MongoId> firItemIds)
        {
            logger.Debug("Restoring FiR status on items...");

            var ctx = InventoryContext
                .Builder()
                .Add(Inventory.Restored, profile.GetEquipmentItems())
                .Build();

            foreach (var id in firItemIds)
            {
                var item = ctx.GetItem(Inventory.Restored, id);
                var isKept = modConfig.RestoreOnlyLostOnDeathSlots
                    && lostOnDeathHelper.IsItemKeptAfterDeath(id, ctx.GetDict(Inventory.Restored), profile);
                if (item?.Upd is not null && !isKept)
                {
                    item.Upd.SpawnedInSession = true;
                }
            }
        }
    }
}