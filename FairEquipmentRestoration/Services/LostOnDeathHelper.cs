using FairEquipmentRestoration.Accessors;
using FairEquipmentRestoration.Exceptions;
using FairEquipmentRestoration.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class LostOnDeathHelper(
        InRaidHelperAccessor inRaidHelperAccessor,
        InRaidHelper inRaidHelper,
        InRaidHelperProvider inRaidHelperProvider,
        ISptLogger<LostOnDeathHelper> logger,
        ICloner cloner,
        ItemLogHelper itemLogHelper)
    {
        protected readonly InRaidHelper InvertedInRaidHelper = inRaidHelperProvider.GetWithInvertedLostOnDeathConfig();

        public void ReplaceItemsNotLostOnDeath(PmcData profile, PmcData profileToReplaceWith, MongoId sessionId)
        {
            logger.Debug("Replacing items not lost on death...");

            var deletedProfileToReplaceWith = cloner.Clone(profileToReplaceWith)!;
            inRaidHelper.DeleteInventory(deletedProfileToReplaceWith, sessionId);

            var otherInventory = deletedProfileToReplaceWith.GetEquipmentAndQuestItems();
            var profileInventory = profile.Inventory?.Items;
            if (otherInventory is null
                || profile.Inventory is null
                || profileInventory is null)
            {
                throw new InvalidInventoryException();
            }

            var fastPanel = cloner.Clone(profile.Inventory.FastPanel);

            InvertedInRaidHelper.DeleteInventory(profile, sessionId);

            profile.Inventory.FastPanel = fastPanel;

            itemLogHelper.LogInventory(LogLevel.Debug, profile, message: "Removed items not lost on death from inventory:");

            // Will handle dupes
            inRaidHelperAccessor.AddItemsToInventory(otherInventory, profileInventory);

            itemLogHelper.LogInventory(LogLevel.Debug, deletedProfileToReplaceWith, message: "Preserved items to add from other inventory:");
        }

        public bool IsItemKeptAfterDeath(MongoId id, IReadOnlyDictionary<MongoId, Item> inventory, PmcData pmcData)
        {
            if (pmcData.Inventory?.Equipment is null)
            {
                throw new InvalidInventoryException();
            }

            var item = inventory.GetValueOrDefault(id);
            if (item is null)
            {
                logger.Warning($"Item {id} was not found in inventory, keep after death = false");
                return false;
            }

            // SPT InRaidHelper.IsItemKeptAfterDeath will check everything for us,
            // but it needs items directly equipped on player
            Item currentItem = item;
            while (currentItem.ParentId is not null && currentItem.ParentId != pmcData.Inventory.Equipment)
            {
                var foundItem = inventory.GetValueOrDefault(currentItem.ParentId);
                if (foundItem is null)
                {
                    logger.Warning($"Item {currentItem.ParentId} was not found in inventory, keep after death = false");
                    return false;
                }

                currentItem = foundItem;
            }

            var isKept = inRaidHelperAccessor.IsItemKeptAfterDeath(pmcData, currentItem);
            return isKept;
        }
    }
}
