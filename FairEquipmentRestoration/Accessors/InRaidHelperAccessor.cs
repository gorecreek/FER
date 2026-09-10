using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Accessors
{
    [Injectable]
    public class InRaidHelperAccessor(InRaidHelper inRaidHelper)
    {
        public bool IsItemKeptAfterDeath(PmcData pmcData, Item itemToCheck)
        {
            var result = Traverse.Create(inRaidHelper)
                .Method(nameof(IsItemKeptAfterDeath), pmcData, itemToCheck)
                .GetValue();

            return (bool)result;
        }

        public void AddItemsToInventory(IEnumerable<Item> itemsToAdd, List<Item> serverInventoryItems)
        {
            Traverse.Create(inRaidHelper)
                .Method(nameof(AddItemsToInventory), itemsToAdd, serverInventoryItems)
                .GetValue();
        }
    }
}
