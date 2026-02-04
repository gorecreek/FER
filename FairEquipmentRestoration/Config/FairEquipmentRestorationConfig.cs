using System.Text.Json.Serialization;

namespace FairEquipmentRestoration.Config
{
    public record FairEquipmentRestorationConfig
    {
        [JsonPropertyName("enable")]
        public required bool Enable { get; set; } = true;

        [JsonPropertyName("updateOnTransfer")]
        public required bool UpdateOnTransfer { get; set; } = false;

        [JsonPropertyName("restoreOnlyLostOnDeathSlots")]
        public required bool RestoreOnlyLostOnDeathSlots { get; set; } = true;

        [JsonPropertyName("restoreLostItems")]
        public required bool RestoreLostItems { get; set; } = false;

        [JsonPropertyName("restoreItemCondition")]
        public required bool RestoreItemCondition { get; set; } = false;

        [JsonPropertyName("restoreFoundInRaid")]
        public required bool RestoreFoundInRaid { get; set; } = false;

        [JsonPropertyName("restoreInsurance")]
        public required bool RestoreInsurance { get; set; } = true;

        [JsonPropertyName("enableItemDebugLogging")]
        public bool EnableItemDebugLogging { get; set; } = false;
    }
}
