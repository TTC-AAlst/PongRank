using PongRank.ML.Models;

namespace PongRank.WebApi.Utilities;

public class WebApiSettings
{
    public string Origins { get; set; } = "";
    public bool StartSyncJob { get; set; }
    public MLSettings ML { get; set; } = new();
    public string Loki { get; set; } = "";
}