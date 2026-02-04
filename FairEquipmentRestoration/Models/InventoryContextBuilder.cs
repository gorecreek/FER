using FairEquipmentRestoration.Exceptions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Models
{
    public sealed class InventoryContextBuilder
    {
        private readonly Dictionary<Inventory, IReadOnlyList<Item>> _inventories = [];

        internal InventoryContextBuilder()
        {
        }

        public InventoryContextBuilder Add(Inventory role, IReadOnlyList<Item>? inventory)
        {
            if (inventory is null)
            {
                throw new InvalidInventoryException();
            }

            if (!_inventories.TryAdd(role, inventory))
            {
                throw new InvalidOperationException($"Inventory '{role}' was already added.");
            }

            return this;
        }

        public InventoryContext Build()
        {
            if (_inventories.Count == 0)
            {
                throw new InvalidOperationException("No inventories were added to InventoryContext.");
            }

            return new InventoryContext(_inventories);
        }
    }
}
