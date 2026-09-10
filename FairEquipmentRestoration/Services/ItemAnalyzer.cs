using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Extensions;
using FairEquipmentRestoration.Models;
using Microsoft.Extensions.Logging;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class ItemAnalyzer(
        ISptLogger<ItemAnalyzer> logger,
        ItemLogHelper itemLogHelper,
        FairEquipmentRestorationConfig modConfig,
        LostOnDeathHelper lostOnDeathHelper)
    {
        public InventoryDiffAnalysis AnalyzeInventoryDiff(
            PmcData preRaidProfile,
            PmcData postRaidProfile,
            HashSet<MongoId> transferredItemIds)
        {
            logger.Debug("Analyzing inventory diff...");

            var ctx = InventoryContext
                .Builder()
                .Add(Inventory.PreRaid, preRaidProfile.GetEquipmentItems())
                .Add(Inventory.PostRaid, postRaidProfile.GetEquipmentItems())
                .Build();

            var itemAnalysis = ItemDiffAnalysis.Empty;

            var preRaidInventory = ctx.Get(Inventory.PreRaid);
            foreach (var preRaidItem in preRaidInventory)
            {
                var postRaidItem = ctx.GetItem(Inventory.PostRaid, preRaidItem.Id);

                ItemDiffAnalysis analysis;
                if (postRaidItem is not null)
                {
                    analysis = AnalyzeSurvivingItem(preRaidItem, postRaidItem, ctx, preRaidProfile, postRaidProfile);
                }
                else
                {
                    analysis = AnalyzeLostItem(preRaidItem, ctx, preRaidProfile, transferredItemIds);
                }
                    
                itemAnalysis = itemAnalysis.Combine(analysis);
            }

            var filteredOrphanedItemIds = FilterOrphanedItemIds(itemAnalysis, postRaidProfile, ctx);
            var insuredItems = preRaidProfile.GetInsuredItems();

            LogItemIds(transferredItemIds, filteredOrphanedItemIds, itemAnalysis);
            itemLogHelper.LogInsuredItems(LogLevel.Debug, insuredItems, ctx.Get(Inventory.PreRaid), "Insured items:");

            return new InventoryDiffAnalysis
            {
                LostItemIds = itemAnalysis.LostItemIds,
                LostUnableToReturnItemIds = itemAnalysis.LostUnableToReturnItemIds,
                OrphanedItemIds = filteredOrphanedItemIds,
                InsuredItems = insuredItems
            };
        }

        private ItemDiffAnalysis AnalyzeSurvivingItem(
            Item preRaidItem, 
            Item postRaidItem, 
            InventoryContext ctx, 
            PmcData preRaidProfile,
            PmcData postRaidProfile)
        {
            var lostUnableToReturnItemIds = new HashSet<MongoId>();
            if (modConfig.RestoreOnlyLostOnDeathSlots)
            {
                var wasKept = lostOnDeathHelper.IsItemKeptAfterDeath(preRaidItem.Id, ctx.GetDict(Inventory.PreRaid), preRaidProfile);
                var isKept = lostOnDeathHelper.IsItemKeptAfterDeath(postRaidItem.Id, ctx.GetDict(Inventory.PostRaid), postRaidProfile);

                if (wasKept && !isKept)
                {
                    // An item would be lost if dragged from kept on death slot to lost on death slot
                    // and we should save it to send in mail later
                    lostUnableToReturnItemIds.Add(preRaidItem.Id);
                }
            }

            return new ItemDiffAnalysis
            {
                LostUnableToReturnItemIds = lostUnableToReturnItemIds
            };
        }

        private ItemDiffAnalysis AnalyzeLostItem(
            Item preRaidItem,
            InventoryContext ctx,
            PmcData preRaidProfile,
            HashSet<MongoId> transferredItemIds)
        {
            var lostItemIds = new HashSet<MongoId>();
            var lostUnableToReturnItemIds = new HashSet<MongoId>();
            var orphanedItemIds = new HashSet<MongoId>();

            var isItemTransferred = transferredItemIds.Contains(preRaidItem.Id);
            if (modConfig.RestoreLostItems)
            {
                var isKept = modConfig.RestoreOnlyLostOnDeathSlots
                    && lostOnDeathHelper.IsItemKeptAfterDeath(preRaidItem.Id, ctx.GetDict(Inventory.PreRaid), preRaidProfile);
                if (isKept && !isItemTransferred)
                {
                    // Items lost from lostondeath=false slots are only possible to return in mail
                    lostUnableToReturnItemIds.Add(preRaidItem.Id);
                }
            }

            if (!modConfig.RestoreLostItems || isItemTransferred)
            {
                lostItemIds.Add(preRaidItem.Id);

                var children = ctx.Get(Inventory.PreRaid).GetChildren(preRaidItem.Id).Select(x => x.Id);
                orphanedItemIds.UnionWith(children);
            }

            return new ItemDiffAnalysis
            {
                LostItemIds = lostItemIds,
                LostUnableToReturnItemIds = lostUnableToReturnItemIds,
                OrphanedItemIds = orphanedItemIds
            };
        }

        private void LogItemIds(
            IReadOnlySet<MongoId> transferredItemIds,
            IReadOnlySet<MongoId> orphanedItemIds,
            ItemDiffAnalysis analysis)
        {
            if (!modConfig.EnableItemDebugLogging)
            {
                return;
            }

            foreach (var id in transferredItemIds)
            {
                logger.Debug($"Transferred item: {id}");
            }

            foreach (var id in analysis.LostItemIds)
            {
                logger.Debug($"Lost item: {id}");
            }

            foreach (var id in analysis.LostUnableToReturnItemIds)
            {
                logger.Debug($"Lost and unable to return item: {id}");
            }

            foreach (var id in orphanedItemIds)
            {
                logger.Debug($"Orphaned item: {id}");
            }
        }

        private HashSet<MongoId> FilterOrphanedItemIds(
            ItemDiffAnalysis analysis,
            PmcData profile,
            InventoryContext ctx)
        {
            var restoreLostOnDeath = modConfig.RestoreOnlyLostOnDeathSlots;
            var postRaidInventoryDict = ctx.GetDict(Inventory.PostRaid);
            var filtered = analysis.OrphanedItemIds
                .Except(analysis.LostItemIds)
                // Additional check to prevent dupes using mail
                .Where(x => restoreLostOnDeath && !lostOnDeathHelper.IsItemKeptAfterDeath(x, postRaidInventoryDict, profile)
                    || !restoreLostOnDeath)
                .ToHashSet();

            return filtered;
        }

        public HashSet<MongoId> GetItemIdsToExcludeFromLostInsured(
            PmcData preRaidProfile,
            PmcData postRaidProfile)
        {
            var ctx = InventoryContext
                .Builder()
                .Add(Inventory.PreRaid, preRaidProfile.GetEquipmentItems())
                .Add(Inventory.PostRaid, postRaidProfile.GetEquipmentItems())
                .Build();

            var itemIdsToExclude = new HashSet<MongoId>();
            var preRaidInventory = ctx.Get(Inventory.PreRaid);
            foreach (var preRaidItem in preRaidInventory)
            {
                var postRaidItem = ctx.GetItem(Inventory.PostRaid, preRaidItem.Id);
                // Ideally also check if item was transferred but they shouldnt exist in that collection
                if (postRaidItem is not null || modConfig.RestoreLostItems)
                {
                    itemIdsToExclude.Add(preRaidItem.Id);
                }
            }

            return itemIdsToExclude;
        }
    }
}