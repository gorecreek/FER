using FairEquipmentRestoration.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using System.Reflection;

namespace FairEquipmentRestoration.Patches;

[Injectable]
public class ItemCannotBeLostOnDeathPatch : AbstractPatch
{
    private static FairEquipmentRestorationConfig _config = default!;

    public ItemCannotBeLostOnDeathPatch(FairEquipmentRestorationConfig config)
    {
        _config = config;
    }

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
        if (_config.Enable)
        {
            // This method checks if an item about to be send in insurance was in the secure container
            // It is needed because the client can send items from secure slots in LostInsuredItems 
            // That check is not needed when using this mod since it manages LostInsuredItems
            __result = false;
        }
    }
}