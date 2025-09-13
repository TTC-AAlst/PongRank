using Itenium.Forge.Core;
using Itenium.Forge.Settings;
using PongRank.ML.Models;

namespace PongRank.WebApi.Utilities;

public class WebApiSettings : IForgeSettings
{
    public ForgeSettings Forge { get; } = new();

    public string Origins { get; set; } = "";
    public bool StartSyncJob { get; set; }
    public SyncJobSettings SyncJob { get; set; } = new();
    public MLSettings ML { get; set; } = new();
    public CurrentYearSettings CurrentYear { get; set; } = new();
    public string Loki { get; set; } = "";
}

/// <summary>
/// Settings for current year syncing
/// </summary>
public class CurrentYearSettings
{
    /// <summary>
    /// Vttl Club UniqueIndex
    /// </summary>
    public string Vttl { get; set; } = "";
    /// <summary>
    /// Sporta Club UniqueIndex
    /// </summary>
    public string Sporta { get; set; } = "";

    public override string ToString() => $"Vttl={Vttl}, Sporta={Sporta}";
}