using System.Text.Json.Serialization;

namespace Plugin.UplayServer.Rest;

/// <summary>
/// Read from Configs/UplayServer.RestConfig.json. The titles this plugin
/// serves are no longer here: they are the host's, in Configs/Titles.json
/// (<see cref="ServerShared.Titles.TitleRegistry"/>), so that the REST and
/// Quazal halves of one game agree on what it is called and where its
/// captures live. What remains is the shape of the old file, kept for one
/// release so a deployment that still carries a title list here is told
/// where it went rather than silently serving nothing for it.
/// </summary>
public sealed class UplayRestSettings
{
    /// <summary>
    /// Superseded by Configs/Titles.json. Entries left here are reported at
    /// start and not served.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LegacyTitleSettings>? Titles { get; set; }

    /// <summary>Superseded twice over: first by <see cref="Titles"/>, now by Configs/Titles.json.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpaceId { get; set; }

    /// <summary>Superseded twice over: first by <see cref="Titles"/>, now by Configs/Titles.json.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AppId { get; set; }
}

/// <summary>
/// The per-title shape the REST config used to hold, read only to name what
/// an operator has to move. Its fields map onto
/// <see cref="ServerShared.Titles.TitleDescriptor"/> one for one.
/// </summary>
public sealed class LegacyTitleSettings
{
    public string Name { get; set; } = "";
    public string AppId { get; set; } = "";
    public string SpaceId { get; set; } = "";
    public string LegacyOnlineConfigId { get; set; } = "";
    public string ResourceDir { get; set; } = "";
    public bool AdvertiseLocalRendezVousIdentity { get; set; } = true;
    public string RendezVousIdentityFile { get; set; } = "Configs/RendezVousIdentity.json";
}
