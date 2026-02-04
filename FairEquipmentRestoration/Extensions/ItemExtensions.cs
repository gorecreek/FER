using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Extensions
{
    public static class ItemExtensions
    {
        public static List<Item> GetChildren(this IEnumerable<Item> items, MongoId baseItemId, bool excludeStoredItems = false)
        {
            return [.. items.GetItemWithChildren(baseItemId, excludeStoredItems).Where(x => x.Id != baseItemId)];
        }
    }
}
