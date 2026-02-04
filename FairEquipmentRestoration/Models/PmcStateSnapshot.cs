using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace FairEquipmentRestoration.Models
{
    public readonly record struct PmcStateSnapshot
    {
        public PmcData? PreRaidState { get; init; }

        public HashSet<MongoId> TransferredItems { get; init; }
    }
}
