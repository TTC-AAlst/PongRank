using PongRank.Model;
using PongRank.WebApi.Utilities;

namespace PongRank.Tests.Fakes;

/// <summary>Records SyncCompletedAsync calls so tests can assert what (if anything) was notified.</summary>
internal class FakeNtfyNotifier : INtfyNotifier
{
    public List<(Competition Competition, int Year, int NewMatches, int ClubsSynced, int ClubsTotal, int TournamentsSynced, int TournamentsTotal)> Sent { get; } = new();

    public Task SyncCompletedAsync(Competition competition, int year, int newMatches,
        int clubsSynced, int clubsTotal, int tournamentsSynced, int tournamentsTotal)
    {
        Sent.Add((competition, year, newMatches, clubsSynced, clubsTotal, tournamentsSynced, tournamentsTotal));
        return Task.CompletedTask;
    }
}
