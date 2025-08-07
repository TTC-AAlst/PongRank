using Itenium.Forge.Core;
using Itenium.Forge.Settings;
using PongRank.ML.Models;

namespace PongRank.WebApi.Utilities;

public class WebApiSettings : IForgeSettings
{
    public ForgeSettings Forge { get; } = new();

    public string Origins { get; set; } = "";
    public bool StartSyncJob { get; set; }
    public MLSettings ML { get; set; } = new();
    public string Loki { get; set; } = "";
}