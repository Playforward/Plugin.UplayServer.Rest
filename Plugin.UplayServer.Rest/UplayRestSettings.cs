namespace Plugin.UplayServer.Rest;

/// <summary>
/// Per-title UbiServices identity, read from Configs/UplayServer.RestConfig.json.
/// </summary>
public sealed class UplayRestSettings
{
    public List<TitleSettings> Titles { get; set; } =
    [
        new()
        {
            Name = "Far Cry New Dawn",
            AppId = "4eb9089b-324e-4e65-a055-5ea05cfeb68f",
            SpaceId = "6f75883b-3b5b-45ce-a2d4-2a7a8477180c",
            // empty: use the built-in captures embedded in this assembly
            ResourceDir = "",
        },
        new()
        {
            Name = "Far Cry 5",
            AppId = "e0b29a98-ce38-4bc7-96ee-9d20e793e9e3",
            SpaceId = "f40e304d-8e8d-4343-8270-d06487c35add",
            ResourceDir = "Titles/FarCry5",
        },
    ];

    public string? SpaceId { get; set; }
    public string? AppId { get; set; }
}

/// <summary>One title's identity and where its captured responses live.</summary>
public sealed class TitleSettings
{
    public string Name { get; set; } = "";

    /// <summary>
    /// UbiServices application id 
    /// </summary>
    public string AppId { get; set; } = "";

    /// <summary>UbiServices space id, used by the /v*/spaces/{spaceId}/... routes.</summary>
    public string SpaceId { get; set; } = "";

    /// <summary>
    /// Directory relative to the server's working directory holding this
    /// title's captured responses: configuration.json, entities.json,
    /// configs_events.json, rendezvous_configuration.json, etc.
    /// </summary>
    public string ResourceDir { get; set; } = "";

    /// <summary>
    /// Replaces the captured RendezVous public_key with the public half
    /// of <see cref="RendezVousIdentityFile"/> when the shared identity is
    /// available. Set false for a title that should keep its captured upstream
    /// identity.
    /// </summary>
    public bool AdvertiseLocalRendezVousIdentity { get; set; } = true;

    /// <summary>
    /// Shared local-server identity document, relative to the server working
    /// directory unless absolute. Quazal reads the same document to obtain the
    /// matching signing private key. Secret material must never be placed in
    /// title captures or source control.
    /// </summary>
    public string RendezVousIdentityFile { get; set; } = "Configs/RendezVousIdentity.json";
}
