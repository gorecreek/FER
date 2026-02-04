using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Extensions
{
    public static class PmcDataExtensions
    {
        public static List<Item>? GetEquipmentItems(this PmcData? profile)
        {
            if (profile?.Inventory?.Items is null
                || profile?.Inventory?.Equipment is null)
            {
                return null;
            }

            return profile.Inventory.Items.GetItemWithChildren(profile.Inventory.Equipment.Value);
        }

        public static List<Item>? GetEquipmentAndQuestItems(this PmcData? profile)
        {
            if (profile?.Inventory?.Items is null
                || profile?.Inventory?.Equipment is null
                || profile?.Inventory?.QuestRaidItems is null)
            {
                return null;
            }

            var equipmentItems = profile.Inventory.Items.GetItemWithChildren(profile.Inventory.Equipment.Value);
            var questItems = profile.Inventory.Items.GetItemWithChildren(profile.Inventory.QuestRaidItems.Value);

            return [.. equipmentItems, .. questItems];
        }

        public static HashSet<MongoId> GetInsuredItemIds(this PmcData? profile)
        {
            var items = profile?.InsuredItems ?? [];
            return [.. items
                .Where(x => x.ItemId is not null)
                .Select(x => x.ItemId!.Value)];
        }

        public static List<InsuredItem> GetInsuredItems(this PmcData? profile)
        {
            return profile?.InsuredItems ?? [];
        }

        public static HashSet<MongoId> GetFiRItemIds(this PmcData? profile)
        {
            var firItemIds = new HashSet<MongoId>();

            var allItems = profile.GetEquipmentItems();
            if (allItems is null)
            {
                return firItemIds;
            }

            foreach (var item in allItems)
            {
                if (item.Upd?.SpawnedInSession ?? false)
                {
                    firItemIds.Add(item.Id);
                }
            }

            return firItemIds;
        }
    }
}
