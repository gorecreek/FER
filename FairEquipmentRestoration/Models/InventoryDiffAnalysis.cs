using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Models
{
    public readonly record struct InventoryDiffAnalysis
    {
        public InventoryDiffAnalysis()
        {
        }

        public IReadOnlySet<MongoId> LostItemIds { get; init; } = new HashSet<MongoId>();

        public IReadOnlySet<MongoId> LostUnableToReturnItemIds { get; init; } = new HashSet<MongoId>();

        public IReadOnlySet<MongoId> OrphanedItemIds { get; init; } = new HashSet<MongoId>();

        public IReadOnlyList<InsuredItem> InsuredItems { get; init; } = [];
    }
}
