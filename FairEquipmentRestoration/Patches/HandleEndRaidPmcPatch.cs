using FairEquipmentRestoration.Config;
using FairEquipmentRestoration.Models;
using FairEquipmentRestoration.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services.InRaid;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

namespace FairEquipmentRestoration.Patches;

[Injectable]
public class HandleEndRaidPmcPatch : AbstractPatch
{
    private static ISptLogger<HandleEndRaidPmcPatch> _logger = default!;
    private static ICloner _cloner = default!;
    private static PmcStateContainer _stateContainer = default!;
    private static InventoryRecoveryHelper _inventoryRecoveryHelper = default!;
    private static FairEquipmentRestorationConfig _modConfig = default!;
    private static LostOnDeathConfig _lostOnDeathConfig = default!;

    public HandleEndRaidPmcPatch(
        ISptLogger<HandleEndRaidPmcPatch> logger,
        ICloner cloner,
        PmcStateContainer stateContainer,
        InventoryRecoveryHelper inventoryRecoveryHelper,
        FairEquipmentRestorationConfig modConfig,
        LostOnDeathConfig lostOnDeathConfig)
    {
        _logger = logger;
        _cloner = cloner;
        _stateContainer = stateContainer;
        _inventoryRecoveryHelper = inventoryRecoveryHelper;
        _modConfig = modConfig;
        _lostOnDeathConfig = lostOnDeathConfig;
    }

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
        __state = null;

        if (!_modConfig.Enable)
        {
            return true;
        }

        if (!isDead && !isTransfer)
        {
            // If any surviving status then need to clear saved states
            _stateContainer.ClearState();

            return true;
        }

        if (_lostOnDeathConfig.WipeOnRaidStart)
        {
            _logger.Warning("[FairEquipmentRestoration] SPT lostondeath.json wipeOnRaidStart is enabled, mod disabled");
        }

        try
        {
            var preRaidProfile = _cloner.Clone(fullServerProfile.CharacterData?.PmcData);
            if (preRaidProfile is null)
            {
                _logger.Warning("[FairEquipmentRestoration] Server profile passed to prefix is null, processing skipped");
                return true;
            }

            UpdateStateContainer(preRaidProfile, request);

            var postRaidProfile = _cloner.Clone(request.Results?.Profile);
            if (postRaidProfile is null)
            {
                _logger.Warning("[FairEquipmentRestoration] Post-raid profile passed to prefix is null, processing skipped");
                return true;
            }

            // Need to process lost insured items because it happens on transfer too
            // The collection needs to be filtered because restoreLostItems config exists
            var filteredLost = _inventoryRecoveryHelper.GetFilteredLostInsuredItems(request.LostInsuredItems, preRaidProfile, postRaidProfile);
            request.LostInsuredItems = filteredLost;

            if (!isDead)
            {
                return true;
            }

            var state = _stateContainer.RestoreState();
            if (state.PreRaidState is null)
            {
                _logger.Warning("[FairEquipmentRestoration] Could not obtain state from state container, processing skipped");
                return true;
            }

            var restoredProfile = _inventoryRecoveryHelper.GetRestoredProfile(state.PreRaidState, postRaidProfile, state.TransferredItems, sessionId);

            __state = restoredProfile;
        }
        catch (Exception ex)
        {
            _logger.Error($"[FairEquipmentRestoration] Error in HandlePostRaidPmc Prefix", ex);
        }

        return true;
    }

    private static void UpdateStateContainer(
        PmcData preRaidProfile,
        EndLocalRaidRequestData request)
    {
        if (!_stateContainer.HasPreRaidState() || _modConfig.UpdateOnTransfer)
        {
            _logger.Debug("[FairEquipmentRestoration] Updating state...");
            _stateContainer.SavePreRaidState(preRaidProfile);
        }

        _stateContainer.SaveTransferredItems(request.TransferItems);
    }

    [PatchPostfix]
    public static void Postfix(
        MongoId sessionId,
        SptProfile fullServerProfile,
        bool isDead,
        RestoredProfileResult? __state)
    {
        if (!_modConfig.Enable)
        {
            return;
        }

        if (_lostOnDeathConfig.WipeOnRaidStart)
        {
            _logger.Warning("[FairEquipmentRestoration] SPT lostondeath.json wipeOnRaidStart is enabled, mod disabled");
        }

        if (!isDead)
        {
            return;
        }

        var serverProfile = fullServerProfile?.CharacterData?.PmcData;
        if (serverProfile is null)
        {
            _logger.Warning("[FairEquipmentRestoration] Server profile passed to postfix is null, processing skipped");
            return;
        }

        if (__state is null)
        {
            _logger.Warning("[FairEquipmentRestoration] Restored profile passed to postfix is null, processing skipped");
            return;
        }

        try
        {
            _inventoryRecoveryHelper.RestoreInventory(serverProfile, __state.Value, sessionId);
        }
        catch (Exception ex)
        {
            _logger.Error($"[FairEquipmentRestoration] Error in HandlePostRaidPmc Postfix", ex);
        }
    }
}