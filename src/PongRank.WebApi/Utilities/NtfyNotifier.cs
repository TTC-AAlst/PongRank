using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using PongRank.Model;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// Posts a per-sync summary to the ntfy `apps` topic, which the docker-01 ntfy→Slack
/// bridge mirrors to #apps. Priority 2 = Slack-only (below the phone's min-priority 4).
/// A failed POST is logged and swallowed — a notification must never break a sync.
/// </summary>
public class NtfyNotifier : INtfyNotifier
{
    private readonly HttpClient _http;
    private readonly NtfySettings _settings;
    private readonly ILogger<NtfyNotifier> _logger;

    public NtfyNotifier(HttpClient http, NtfySettings settings, ILogger<NtfyNotifier> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    public async Task NotifyAsync(SyncSummary s)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token))
            return; // no token → notifications disabled (local/dev)

        // Title is the ntfy header → keep it ASCII; the emoji + middot live in the UTF-8 body.
        var title = $"PongRank sync: {s.Competition} {s.Year}";
        var body =
            $"🏓 synced {s.TournamentsAdded} tournaments from {s.Year} ({s.MatchesAdded} matches)\n" +
            $"{s.ClubsSynced}/{s.ClubsTotal} clubs · {s.TournamentsSynced}/{s.TournamentsTotal} tournaments · {s.Outcome}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.Url}/{_settings.Topic}")
        {
            Content = new StringContent(body, Encoding.UTF8),
        };
        request.Headers.TryAddWithoutValidation("Title", title);
        request.Headers.TryAddWithoutValidation("Priority", "2"); // low → Slack-only, never the phone
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);

        try
        {
            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ntfy sync notification failed for {Competition} {Year}", s.Competition, s.Year);
        }
    }
}
