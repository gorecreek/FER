using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Models
{
    public sealed class InventoryContext
    {
        public IReadOnlyDictionary<Inventory, IReadOnlyList<Item>> Inventories { get; }

        public IReadOnlyDictionary<Inventory, IReadOnlyDictionary<MongoId, Item>> ById { get; }

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

        public InventoryContext(
            IReadOnlyDictionary<Inventory, IReadOnlyList<Item>> inventories)
        {
            Inventories = inventories;
            ById = inventories.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(x => x.Id) as IReadOnlyDictionary<MongoId, Item>);
        }

        public static InventoryContextBuilder Builder() => new();
    }
}
