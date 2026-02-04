using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class InRaidHelperProvider(
        ISptLogger<InRaidHelper> logger,
        InventoryHelper inventoryHelper,
#pragma warning disable CS0618 // Type or member is obsolete
        ConfigServer configServer,
#pragma warning restore CS0618 // Type or member is obsolete
        ICloner cloner,
        DatabaseService databaseService,
        ConfigHelper configHelper
    )
    {
        public InRaidHelper GetWithInvertedLostOnDeathConfig()
        {
            var helper = new InRaidHelper(logger, inventoryHelper, configServer, cloner, databaseService);
            var config = configHelper.GetInvertedLostOnDeathConfig();

            Traverse.Create(helper).Field<LostOnDeathConfig>("LostOnDeathConfig").Value = config;

            return helper;
        }
    }
}
