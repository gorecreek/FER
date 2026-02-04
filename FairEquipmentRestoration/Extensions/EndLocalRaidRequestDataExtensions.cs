using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace FairEquipmentRestoration.Extensions
{
    public static class EndLocalRaidRequestDataExtensions
    {
        public static HashSet<MongoId> GetTransferredItemIds(this EndLocalRaidRequestData request)
        {
            var transferredItemIds = (request.TransferItems ?? [])
                .SelectMany(x => x.Value)
                .Select(x => x.Id)
                .ToHashSet();

            return transferredItemIds;
        }
    }
}
