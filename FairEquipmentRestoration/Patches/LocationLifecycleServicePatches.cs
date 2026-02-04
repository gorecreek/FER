using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Models;
using FairEquipmentRestoration.Services;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

namespace FairEquipmentRestoration.Patches;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class LocationLifecycleServicePatcher(
    ISptLogger<LocationLifecycleServicePatcher> logger) : IOnLoad
{
    public Task OnLoad()
    {
        try
        {
            new HandleEndRaidPmcPatch().Enable();
        }
        catch (Exception ex)
        {
            logger.Error("[FairEquipmentRestoration] Error during enabling patch for HandleEndRaidPmc", ex);
        }

        return Task.CompletedTask;
    }
}

public class HandleEndRaidPmcPatch() : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocationLifecycleService).GetMethod(
            "HandlePostRaidPmc",
            BindingFlags.Instance | BindingFlags.NonPublic,
            Type.DefaultBinder,
            [
                typeof(MongoId),
                typeof(SptProfile),
                typeof(PmcData),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(EndLocalRaidRequestData),
                typeof(string)
            ],
            null
        )!;
    }

    [PatchPrefix]
    public static bool Prefix(
        MongoId sessionId,
        SptProfile fullServerProfile,
        bool isDead,
        bool isTransfer,
        EndLocalRaidRequestData request,
        out RestoredProfileResult? __state)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<LocationLifecycleService>>()!;
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>()!;
        var modConfigProvider = ServiceLocator.ServiceProvider.GetService<ModConfigProvider>()!;
        var stateContainer = ServiceLocator.ServiceProvider.GetService<PmcStateContainer>()!;
        var configServer = ServiceLocator.ServiceProvider.GetService<ConfigServer>()!;
        var lostOnDeathConfig = configServer.GetConfig<LostOnDeathConfig>();
        var inventoryRecoveryHelper = ServiceLocator.ServiceProvider.GetService<InventoryRecoveryHelper>()!;
#pragma warning restore CS0618 // Type or member is obsolete

        __state = null;

        var config = modConfigProvider.Get();

        if (!config.Enable)
        {
            return true;
        }

        if (!isDead && !isTransfer)
        {
            // If any surviving status then need to clear saved states
            stateContainer.ClearState();

            return true;
        }

        if (lostOnDeathConfig.WipeOnRaidStart)
        {
            logger.Warning("[FairEquipmentRestoration] SPT lostondeath.json wipeOnRaidStart is enabled, mod disabled");
        }

        try
        {
            var preRaidProfile = cloner.Clone(fullServerProfile.CharacterData?.PmcData);
            if (preRaidProfile is null)
            {
                logger.Warning("[FairEquipmentRestoration] Server profile passed to prefix is null, processing skipped");
                return true;
            }

            UpdateStateContainer(preRaidProfile, request, config);

            var postRaidProfile = cloner.Clone(request.Results?.Profile);
            if (postRaidProfile is null)
            {
                logger.Warning("[FairEquipmentRestoration] Post-raid profile passed to prefix is null, processing skipped");
                return true;
            }

            // Need to process lost insured items because it happens on transfer too
            // The collection needs to be filtered because restoreLostItems config exists
            var filteredLost = inventoryRecoveryHelper.GetFilteredLostInsuredItems(request.LostInsuredItems, preRaidProfile, postRaidProfile);
            request.LostInsuredItems = filteredLost;

            if (!isDead)
            {
                return true;
            }

            var state = stateContainer.RestoreState();
            if (state.PreRaidState is null)
            {
                logger.Warning("[FairEquipmentRestoration] Could not obtain state from state container, processing skipped");
                return true;
            }

            var restoredProfile = inventoryRecoveryHelper.GetRestoredProfile(state.PreRaidState, postRaidProfile, state.TransferredItems, sessionId);

            __state = restoredProfile;
        }
        catch (Exception ex)
        {
            logger.Error($"[FairEquipmentRestoration] Error in HandlePostRaidPmc Prefix", ex);
        }

        return true;
    }

    private static void UpdateStateContainer(
        PmcData preRaidProfile,
        EndLocalRaidRequestData request,
        FairEquipmentRestorationConfig config)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<LocationLifecycleService>>()!;
        var stateContainer = ServiceLocator.ServiceProvider.GetService<PmcStateContainer>()!;
#pragma warning restore CS0618 // Type or member is obsolete

        if (!stateContainer.HasPreRaidState() || config.UpdateOnTransfer)
        {
            logger.Debug("[FairEquipmentRestoration] Updating state...");
            stateContainer.SavePreRaidState(preRaidProfile);
        }

        stateContainer.SaveTransferredItems(request.TransferItems);
    }

    [PatchPostfix]
    public static void Postfix(
        MongoId sessionId,
        SptProfile fullServerProfile,
        bool isDead,
        RestoredProfileResult? __state)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<LocationLifecycleService>>()!;
        var inventoryRecoveryHelper = ServiceLocator.ServiceProvider.GetService<InventoryRecoveryHelper>()!;
        var modConfigProvider = ServiceLocator.ServiceProvider.GetService<ModConfigProvider>()!;
        var configServer = ServiceLocator.ServiceProvider.GetService<ConfigServer>()!;
        var lostOnDeathConfig = configServer.GetConfig<LostOnDeathConfig>();
#pragma warning restore CS0618 // Type or member is obsolete

        var config = modConfigProvider.Get();
        if (!config.Enable)
        {
            return;
        }

        if (lostOnDeathConfig.WipeOnRaidStart)
        {
            logger.Warning("[FairEquipmentRestoration] SPT lostondeath.json wipeOnRaidStart is enabled, mod disabled");
        }

        if (!isDead)
        {
            return;
        }

        var serverProfile = fullServerProfile?.CharacterData?.PmcData;
        if (serverProfile is null)
        {
            logger.Warning("[FairEquipmentRestoration] Server profile passed to postfix is null, processing skipped");
            return;
        }

        if (__state is null)
        {
            logger.Warning("[FairEquipmentRestoration] Restored profile passed to postfix is null, processing skipped");
            return;
        }

        try
        {
            inventoryRecoveryHelper.RestoreInventory(serverProfile, __state.Value, sessionId);
        }
        catch (Exception ex)
        {
            logger.Error($"[FairEquipmentRestoration] Error in HandlePostRaidPmc Postfix", ex);
        }
    }
}