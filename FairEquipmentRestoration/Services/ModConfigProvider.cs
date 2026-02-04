using FairEquipmentRestoration.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using System.Reflection;

namespace FairEquipmentRestoration.Services
{
    [Injectable]
    public class ModConfigProvider(ModHelper modHelper)
    {
        public FairEquipmentRestorationConfig Get()
        {
            var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var config = modHelper.GetJsonDataFromFile<FairEquipmentRestorationConfig>(pathToMod, "config.json");

            return config;
        }
    }
}
