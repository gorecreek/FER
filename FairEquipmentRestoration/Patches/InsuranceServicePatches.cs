using FairEquipmentRestoration.Services;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System.Reflection;

namespace FairEquipmentRestoration.Patches;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class InsuranceServicePatcher(
    ISptLogger<InsuranceServicePatcher> logger) : IOnLoad
{
    public Task OnLoad()
    {
        try
        {
            new ItemCannotBeLostOnDeathPatch().Enable();
        }
        catch (Exception ex)
        {
            logger.Error("[FairEquipmentRestoration] Error during enabling patch for ItemCannotBeLostOnDeath", ex);
        }

        return Task.CompletedTask;
    }
}

public class ItemCannotBeLostOnDeathPatch() : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(InsuranceService).GetMethod(
            "ItemCannotBeLostOnDeath",
            BindingFlags.Instance | BindingFlags.NonPublic,
            Type.DefaultBinder,
            [
                typeof(Item), 
                typeof(IEnumerable<Item>)
            ],
            null
        )!;
    }

    [PatchPostfix]
    public static void Postfix(ref bool __result)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var modConfigProvider = ServiceLocator.ServiceProvider.GetService<ModConfigProvider>()!;
#pragma warning restore CS0618 // Type or member is obsolete

        var config = modConfigProvider.Get();
        if (config.Enable)
        {
            // This method checks if an item about to be send in insurance was in the secure container
            // It is needed because the client can send items from secure slots in LostInsuredItems 
            // That check is not needed when using this mod since it manages LostInsuredItems
            __result = false;
        }
    }
}