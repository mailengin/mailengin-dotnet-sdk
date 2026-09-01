namespace MailEngin;

public sealed class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public string? TemplateId { get; set; }
    public IDictionary<string, object?>? Variables { get; set; }
    public string? Subject { get; set; }
    public string? FromEmail { get; set; }
    public string? Html { get; set; }
    public bool? ReplyToMailEngin { get; set; }
}

public sealed class SendEmailResponse
{
    public string Id { get; internal set; } = string.Empty;
    public string From { get; internal set; } = string.Empty;
    public string To { get; internal set; } = string.Empty;
    public string? TemplateName { get; internal set; }
    public string CreatedAt { get; internal set; } = string.Empty;
}

public sealed class BulkRecipient
{
    public BulkRecipient(string email, IDictionary<string, object?>? variables = null)
    {
        Email = email;
        Variables = variables;
    }

    public string Email { get; }
    public IDictionary<string, object?>? Variables { get; }
}

public sealed class SendBulkEmailRequest
{
    public IList<BulkRecipient> To { get; set; } = new List<BulkRecipient>();
    public string? TemplateName { get; set; }
    public string? TemplateId { get; set; }
    public IDictionary<string, object?>? Variables { get; set; }
    public string? Subject { get; set; }
    public string? FromEmail { get; set; }
    public string? Html { get; set; }
    public bool? ReplyToMailEngin { get; set; }
}

public sealed class SendBulkEmailResponse
{
    public bool Success { get; internal set; }
    public string JobId { get; internal set; } = string.Empty;
    public int QueuedCount { get; internal set; }
    public int? SentCount { get; internal set; }
    public int? FailedCount { get; internal set; }
    public string? TemplateName { get; internal set; }
    public string Message { get; internal set; } = string.Empty;
}
