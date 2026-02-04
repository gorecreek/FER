using SPTarkov.Server.Core.Models.Common;

namespace FairEquipmentRestoration.Models
{
    public readonly record struct ItemDiffAnalysis
    {
        public ItemDiffAnalysis()
        {
        }

        public IReadOnlySet<MongoId> LostItemIds { get; init; } = new HashSet<MongoId>();

        public IReadOnlySet<MongoId> LostUnableToReturnItemIds { get; init; } = new HashSet<MongoId>();

        public IReadOnlySet<MongoId> OrphanedItemIds { get; init; } = new HashSet<MongoId>();

        public ItemDiffAnalysis Combine(ItemDiffAnalysis other)
        {
            return new ItemDiffAnalysis
            {
                LostItemIds = LostItemIds.Union(other.LostItemIds).ToHashSet(),
                LostUnableToReturnItemIds = LostUnableToReturnItemIds.Union(other.LostUnableToReturnItemIds).ToHashSet(),
                OrphanedItemIds = OrphanedItemIds.Union(other.OrphanedItemIds).ToHashSet()
            };
        }

        public static ItemDiffAnalysis Empty
        {
            get => new();
        }
    }
}
