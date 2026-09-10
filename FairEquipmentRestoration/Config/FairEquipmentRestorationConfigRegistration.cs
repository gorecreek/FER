using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;

namespace FairEquipmentRestoration.Config
{
    public class FairEquipmentRestorationConfigRegistration : IOnDIConstruct
    {
        private static ModHelper _modHelper = default!;

        public FairEquipmentRestorationConfigRegistration(ModHelper modHelper)
        {
        }

        public static Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken)
        {
            FairEquipmentRestorationConfig config = LoadConfigFromDisk();
            serviceCollection.AddSingleton(config);

            return Task.CompletedTask;
        }

        private static FairEquipmentRestorationConfig LoadConfigFromDisk(){
            var config = _modHelper.GetJsonDataFromModFile<FairEquipmentRestorationConfig>(string.Empty, "config.jsonc");

            return config;
        }
    }
}
