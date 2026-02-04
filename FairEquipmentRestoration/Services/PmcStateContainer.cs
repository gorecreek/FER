using FairEquipmentRestoration.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FairEquipmentRestoration.Services
{
    [Injectable(injectionType: InjectionType.Singleton)]
    public class PmcStateContainer
    {
        private PmcData? _preRaidState;
        private HashSet<MongoId> _transferredItems = [];

        public void SavePreRaidState(PmcData? data)
        {
            _preRaidState = data;
        }

        public void SaveTransferredItems(Dictionary<string, IEnumerable<Item>>? items)
        {
            var itemsToAdd = (items ?? [])
                .SelectMany(x => x.Value)
                .Select(x => x.Id)
                .ToHashSet();

            _transferredItems.UnionWith(itemsToAdd);
        }

        public bool HasPreRaidState()
        {
            return _preRaidState is not null;
        }

        public void ClearState()
        {
            _preRaidState = null;
            _transferredItems = [];
        }

        public PmcStateSnapshot RestoreState()
        {
            var state = new PmcStateSnapshot()
            {
                PreRaidState = _preRaidState,
                TransferredItems = _transferredItems
            };

            _preRaidState = null;
            _transferredItems = [];

            return state;
        }
    }
}
