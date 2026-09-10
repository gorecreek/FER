using SPTarkov.Server.Core.Models.Spt.Mod;

namespace FairEquipmentRestoration;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.gorecreek.fairequipmentrestoration";
    public string Name { get; init; } = "Fair Equipment Restoration";
    public string Author { get; init; } = "gorecreek";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.5");

    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "GPL-3.0-only";
    public bool HasPrepatcher { get; init; } = false;
}
