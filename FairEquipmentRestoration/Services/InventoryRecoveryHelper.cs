using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Extensions;
using FairEquipmentRestoration.Models;
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
    public class InventoryRecoveryHelper(
        ISptLogger<InventoryRecoveryHelper> logger,
        ICloner cloner,
        InRaidHelper inRaidHelper,
        ItemLogHelper itemLogHelper,
        ItemAnalyzer itemAnalyzer,
        ItemProcessor itemProcessor,
        LostOnDeathHelper lostOnDeathHelper,
        ModConfigProvider modConfigProvider)
    {
        protected readonly FairEquipmentRestorationConfig Config = modConfigProvider.Get();

        public List<Item>? GetFilteredLostInsuredItems(
            IEnumerable<Item>? lostInsuredItems, 
            PmcData preRaidProfile,
            PmcData postRaidProfile)
        {
            logger.Debug("Filtering lost insured items...");

            if (lostInsuredItems is null)
            {
                return null;
            }

            var itemIdsToExclude = itemAnalyzer.GetItemIdsToExcludeFromLostInsured(preRaidProfile, postRaidProfile);
            var filteredItems = lostInsuredItems.Where(x => !itemIdsToExclude.Contains(x.Id)).ToList();

            itemLogHelper.LogItems(LogLevel.Debug, filteredItems, preRaidProfile.Inventory?.Items, "Filtered lost insured items:");

            return filteredItems;
        }

        public RestoredProfileResult GetRestoredProfile(
            PmcData preRaidProfile,
            PmcData postRaidProfile,
            HashSet<MongoId> transferredItemIds,
            MongoId sessionId)
        {
            logger.Debug("Deriving restored inventory...");

            itemLogHelper.LogInventory(LogLevel.Debug, preRaidProfile, message: "Pre-raid inventory:");
            itemLogHelper.LogInventory(LogLevel.Debug, postRaidProfile, message: "Post-raid inventory:");

            var analysis = itemAnalyzer.AnalyzeInventoryDiff(
                preRaidProfile,
                postRaidProfile,
                transferredItemIds);

            var restoredProfile = GetRestoredProfile(preRaidProfile, postRaidProfile, analysis, sessionId);
            var firItemIds = restoredProfile.GetFiRItemIds();

            if (Config.EnableItemDebugLogging)
            {
                foreach (var itemId in firItemIds)
                {
                    logger.Debug($"FiR item: {itemId}");
                }
            }

            logger.Debug("Restored profile derived successfully");

            return new RestoredProfileResult()
            {
                Profile = restoredProfile,
                FiRItemIds = firItemIds
            };
        }

        private PmcData GetRestoredProfile(
            PmcData preRaidProfile,
            PmcData postRaidProfile,
            InventoryDiffAnalysis analysis,
            MongoId sessionId)
        {
            var newProfile = cloner.Clone(preRaidProfile)!;
            itemProcessor.ApplyItemChanges(
                newProfile,
                preRaidProfile,
                postRaidProfile,
                analysis,
                sessionId);

            itemProcessor.RemoveLostQuestItems(newProfile, preRaidProfile, postRaidProfile, sessionId);

            itemLogHelper.LogInventory(LogLevel.Debug, newProfile, message: "Processed restored inventory:");

            if (Config.RestoreOnlyLostOnDeathSlots)
            {
                lostOnDeathHelper.ReplaceItemsNotLostOnDeath(newProfile, postRaidProfile, sessionId);
            }

            newProfile.InsuredItems = [.. analysis.InsuredItems];

            return newProfile;
        }

        public void RestoreInventory(PmcData serverProfile, RestoredProfileResult restoredProfile, MongoId sessionId)
        {
            logger.Debug("Replacing server inventory with restored inventory...");

            var profile = restoredProfile.Profile;
            inRaidHelper.SetInventory(sessionId, serverProfile, profile, false, false);

            // SetInventory preserves insured item collection which was altered
            // when the inventory was removed during EndLocalRaidPmc method call
            // so need to update it here if we want to preserve insurance
            serverProfile.InsuredItems = profile.InsuredItems;

            if (Config.RestoreFoundInRaid)
            {
                itemProcessor.RestoreFiRStatusOnItems(serverProfile, restoredProfile.FiRItemIds);
            }

            logger.Debug("Inventory restored successfully");
        }
    }
}