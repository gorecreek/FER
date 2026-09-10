using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Models
{
    public sealed class InventoryContext(
        IReadOnlyDictionary<Inventory, IReadOnlyList<Item>> inventories)
    {
        public IReadOnlyDictionary<Inventory, IReadOnlyList<Item>> Inventories { get; } = inventories;

        public IReadOnlyDictionary<Inventory, IReadOnlyDictionary<MongoId, Item>> ById { get; } = inventories.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(x => x.Id) as IReadOnlyDictionary<MongoId, Item>);

        public IReadOnlyList<Item> Get(Inventory role)
        {
            return Inventories[role];
        }

        public IReadOnlyDictionary<MongoId, Item> GetDict(Inventory role)
        {
            return ById[role];
        }

        public Item? GetItem(Inventory role, MongoId id)
        {
            return ById[role].GetValueOrDefault(id);
        }

        public static InventoryContextBuilder Builder() => new();
    }
}
