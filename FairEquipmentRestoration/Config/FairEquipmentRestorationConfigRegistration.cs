using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;

namespace FairEquipmentRestoration.Config
{
    public class FairEquipmentRestorationConfigRegistration : IOnDIConstruct
    {
        public static Task OnDIConstructAsync(
            IServiceCollection serviceCollection,
            CancellationToken cancellationToken)
        {
            // TODO: rewrite more idiomatically
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var modHelper = serviceProvider.GetService<ModHelper>();

            FairEquipmentRestorationConfig config = LoadConfigFromDisk(modHelper!);
            serviceCollection.AddSingleton(config);
            
            return Task.CompletedTask;
        }

        private static FairEquipmentRestorationConfig LoadConfigFromDisk(ModHelper modHelper)
        {
            var config = modHelper.GetJsonDataFromModFile<FairEquipmentRestorationConfig>(
                string.Empty, 
                "config.jsonc");

            return config;
        }
    }
}
