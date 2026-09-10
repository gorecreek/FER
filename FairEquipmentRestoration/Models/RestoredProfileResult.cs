using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace FairEquipmentRestoration.Models
{
    public readonly record struct RestoredProfileResult
    {
        public PmcData Profile { get; init; }

        public IReadOnlySet<MongoId> FiRItemIds { get; init; }
    }
}
