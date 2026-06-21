namespace PongRank.WebApi.Utilities;

/// <summary>
/// ntfy notification config. Url + Topic come from appsettings.json; Token is injected
/// from the NTFY_TOKEN env var (Coolify → SOPS). An empty Token disables notifications,
/// so local/dev runs never post to Slack.
/// </summary>
public class NtfySettings
{
    public string Url { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Token { get; set; } = "";
}
