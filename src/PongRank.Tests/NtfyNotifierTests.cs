using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PongRank.Model;
using PongRank.WebApi.Utilities;
using Xunit;

namespace PongRank.Tests;

public class NtfyNotifierTests
{
    /// <summary>Captures the outgoing request instead of hitting the network.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static NtfyNotifier Notifier(CapturingHandler handler, string token) =>
        new(new HttpClient(handler),
            new NtfySettings { Url = "https://ntfy.test", Topic = "apps", Token = token },
            NullLogger<NtfyNotifier>.Instance);

    [Fact]
    public async Task Posts_title_priority_bearer_and_body()
    {
        var handler = new CapturingHandler();

        await Notifier(handler, "tk_test").SyncCompletedAsync(Competition.Vttl, 2024, 38, 12, 12, 6, 6);

        Assert.Equal(1, handler.Calls);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://ntfy.test/apps", handler.Request.RequestUri!.ToString());
        Assert.Equal("PongRank sync: Vttl 2024", handler.Request.Headers.GetValues("Title").Single());
        Assert.Equal("2", handler.Request.Headers.GetValues("Priority").Single());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("tk_test", handler.Request.Headers.Authorization.Parameter);
        Assert.Equal("🏓 38 new matches\n12/12 clubs · 6/6 tournaments synced", handler.Body);
    }

    [Fact]
    public async Task No_token_is_a_no_op()
    {
        var handler = new CapturingHandler();

        await Notifier(handler, "").SyncCompletedAsync(Competition.Vttl, 2024, 38, 12, 12, 6, 6);

        Assert.Equal(0, handler.Calls);
    }
}
