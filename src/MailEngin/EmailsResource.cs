using System.Text.Json;

namespace MailEngin;

public sealed class EmailsResource
{
    private readonly MailEnginClient _client;
    internal EmailsResource(MailEnginClient client) => _client = client;

    public async Task<SendEmailResponse> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.To)) throw new ArgumentException("mailengin.Emails.SendAsync requires To.", nameof(request));
        RequireContent(request.TemplateName, request.TemplateId, request.Html, request.Subject);
        var payload = WithoutNulls(new Dictionary<string, object?> {
            ["to"] = request.To, ["template_name"] = request.TemplateName, ["template_id"] = request.TemplateId,
            ["variables"] = request.Variables, ["subject"] = request.Subject, ["from_email"] = request.FromEmail,
            ["html"] = request.Html, ["reply_to_mailengin"] = request.ReplyToMailEngin,
        });
        using var data = await _client.PostAsync("/api/developer/send", payload, cancellationToken).ConfigureAwait(false);
        var root = data.RootElement;
        try
        {
            return new SendEmailResponse {
                Id = root.GetProperty("id").GetString()!, From = root.GetProperty("from").GetString()!,
                To = root.GetProperty("to").GetString()!, TemplateName = GetNullableString(root, "template_name"),
                CreatedAt = root.GetProperty("created_at").GetString()!,
            };
        }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException)
        {
            throw new MailEnginException("MailEngin API returned an invalid response.", errorCode: "invalid_response", body: root.GetRawText(), innerException: error);
        }
    }

    public async Task<SendBulkEmailResponse> SendBulkAsync(SendBulkEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.To.Count == 0) throw new ArgumentException("mailengin.Emails.SendBulkAsync requires recipients.", nameof(request));
        if (request.To.Count > 1000) throw new ArgumentException("mailengin.Emails.SendBulkAsync accepts up to 1000 recipients.", nameof(request));
        RequireContent(request.TemplateName, request.TemplateId, request.Html, request.Subject);
        var recipients = new List<object>();
        foreach (var recipient in request.To)
        {
            if (string.IsNullOrWhiteSpace(recipient.Email)) throw new ArgumentException("Every bulk recipient requires an email address.", nameof(request));
            recipients.Add(recipient.Variables is null ? recipient.Email : new Dictionary<string, object?> { ["email"] = recipient.Email, ["variables"] = recipient.Variables });
        }
        var payload = WithoutNulls(new Dictionary<string, object?> {
            ["to"] = recipients, ["template_name"] = request.TemplateName, ["template_id"] = request.TemplateId,
            ["variables"] = request.Variables, ["subject"] = request.Subject, ["from_email"] = request.FromEmail,
            ["html"] = request.Html, ["reply_to_mailengin"] = request.ReplyToMailEngin,
        });
        using var data = await _client.PostAsync("/api/developer/send-bulk", payload, cancellationToken).ConfigureAwait(false);
        var root = data.RootElement;
        try
        {
            return new SendBulkEmailResponse {
                Success = root.GetProperty("success").GetBoolean(), JobId = root.GetProperty("jobId").GetString()!,
                QueuedCount = root.GetProperty("queued_count").GetInt32(), SentCount = GetNullableInt(root, "sent_count"),
                FailedCount = GetNullableInt(root, "failed_count"), TemplateName = GetNullableString(root, "template_name"),
                Message = root.GetProperty("message").GetString()!,
            };
        }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException)
        {
            throw new MailEnginException("MailEngin API returned an invalid response.", errorCode: "invalid_response", body: root.GetRawText(), innerException: error);
        }
    }

    private static void RequireContent(string? templateName, string? templateId, string? html, string? subject)
    {
        var hasTemplate = !string.IsNullOrWhiteSpace(templateName) || !string.IsNullOrWhiteSpace(templateId);
        if (!hasTemplate && string.IsNullOrWhiteSpace(html)) throw new ArgumentException("Provide TemplateName, TemplateId, or Html.");
        if (!hasTemplate && string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Raw HTML sends require Subject.");
    }

    private static Dictionary<string, object?> WithoutNulls(Dictionary<string, object?> values) =>
        values.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value);
    private static string? GetNullableString(JsonElement root, string key) =>
        root.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
    private static int? GetNullableInt(JsonElement root, string key) =>
        root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
}
