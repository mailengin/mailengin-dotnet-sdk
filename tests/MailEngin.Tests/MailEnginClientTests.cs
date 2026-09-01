using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MailEngin.Tests;

public sealed class MailEnginClientTests
{
    [Fact]
    public async Task SendAsyncMapsRequestAndResponse()
    {
        var handler = new StubHandler(request => {
            Assert.Equal("Bearer re_test_key", request.Headers.Authorization?.ToString());
            Assert.Contains("mailengin-dotnet/0.1.0", request.Headers.UserAgent.ToString());
            Assert.Equal("/api/developer/send", request.RequestUri?.AbsolutePath);
            return Json(HttpStatusCode.OK, """{"id":"msg_1","from":"hello@example.com","to":"person@example.com","template_name":"welcome","created_at":"2026-08-18T10:00:00Z"}""");
        });
        using var client = new MailEnginClient("re_test_key", httpClient: new HttpClient(handler));
        var result = await client.Emails.SendAsync(new SendEmailRequest { To = "person@example.com", TemplateName = "welcome" });
        Assert.Equal("msg_1", result.Id);
    }

    [Fact]
    public async Task ExposesRateLimitAndValidatesHtml()
    {
        var handler = new StubHandler(_ => {
            var response = Json((HttpStatusCode)429, """{"message":"Rate limit exceeded","code":"rate_limited"}""");
            response.Headers.Add("Retry-After", "12");
            response.Headers.Add("x-request-id", "req_1");
            return response;
        });
        using var client = new MailEnginClient("re_test_key", httpClient: new HttpClient(handler));
        var error = await Assert.ThrowsAsync<MailEnginException>(() => client.Emails.SendAsync(new SendEmailRequest { To = "person@example.com", TemplateName = "welcome" }));
        Assert.Equal((HttpStatusCode)429, error.Status);
        Assert.Equal(12, error.RetryAfter);
        Assert.True(error.IsRetryable);
        await Assert.ThrowsAsync<ArgumentException>(() => client.Emails.SendAsync(new SendEmailRequest { To = "person@example.com", Html = "<p>Hello</p>" }));
    }

    [Fact]
    public async Task RejectsMalformedResponseAndRecipientOverflow()
    {
        using var client = new MailEnginClient("re_test_key", httpClient: new HttpClient(
            new StubHandler(_ => Json(HttpStatusCode.OK, "{}"))));
        var malformed = await Assert.ThrowsAsync<MailEnginException>(() => client.Emails.SendAsync(
            new SendEmailRequest { To = "person@example.com", TemplateName = "welcome" }));
        Assert.Equal("invalid_response", malformed.ErrorCode);

        var bulk = new SendBulkEmailRequest { TemplateName = "welcome" };
        for (var index = 0; index < 1001; index++) bulk.To.Add(new BulkRecipient("person@example.com"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Emails.SendBulkAsync(bulk));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_handler(request));
    }
}
