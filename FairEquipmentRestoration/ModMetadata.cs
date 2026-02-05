using SPTarkov.Server.Core.Models.Spt.Mod;

namespace FairEquipmentRestoration;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.gorecreek.fairequipmentrestoration";
    public override string Name { get; init; } = "Fair Equipment Restoration";
    public override string Author { get; init; } = "gorecreek";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.8");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "GPL-3.0-only";
}
