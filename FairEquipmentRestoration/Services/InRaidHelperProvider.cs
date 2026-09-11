using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class InRaidHelperProvider(
        ISptLogger<InRaidHelper> logger,
        TemplateTable templateTable,
        InventoryHelper inventoryHelper,
        InRaidConfig inRaidConfig,
        ICloner cloner,
        ConfigHelper configHelper
    )
    {
        public InRaidHelper GetWithInvertedLostOnDeathConfig()
        {
            var invertedLostOnDeathConfig = configHelper.GetInvertedLostOnDeathConfig();
            var helper = new InRaidHelper(logger, templateTable, inventoryHelper, inRaidConfig, invertedLostOnDeathConfig, cloner);

            return helper;
        }
    }
}
