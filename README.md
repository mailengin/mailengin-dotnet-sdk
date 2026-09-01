# MailEngin .NET SDK

[![.NET](https://img.shields.io/badge/.NET-Standard_2.0_%7C_8-512bd4.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-ready-004880.svg)](https://www.nuget.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-111827.svg)](./LICENSE)

The official .NET SDK for sending transactional email through [MailEngin](https://mailengin.app). It provides asynchronous APIs, nullable-aware request and response models, cancellation tokens, configurable timeouts, injectable `HttpClient` instances, and structured exceptions.

> [!IMPORTANT]
> This package is for trusted server-side applications only. Never include a MailEngin API key in Blazor WebAssembly, MAUI client applications, desktop binaries, JavaScript, or other distributed client code.

## Supported Targets

- .NET Standard 2.0
- .NET 8 or newer

## Installation

```bash
dotnet add package MailEngin
```

Package Manager Console:

```powershell
Install-Package MailEngin
```

## Before You Send

1. [Verify a sending domain](https://mailengin.app/dashboard/domains).
2. [Create an API key](https://mailengin.app/dashboard/api-keys) and save the full secret.
3. [Create and publish a Developer Template](https://mailengin.app/dashboard/dev-templates).
4. Copy the template API name, such as `welcome-email`.

Store the key in your deployment platform's secret manager or environment:

```env
MAILENGIN_API_KEY=re_your_full_secret_key
```

MailEngin displays the full key only once. A masked key cannot authenticate requests.

## Quick Start

```csharp
using MailEngin;

using var client = new MailEnginClient(
    Environment.GetEnvironmentVariable("MAILENGIN_API_KEY")
        ?? throw new InvalidOperationException("MAILENGIN_API_KEY is missing."));

var email = await client.Emails.SendAsync(new SendEmailRequest
{
    To = "user@example.com",
    FromEmail = "hello@yourdomain.com",
    TemplateName = "welcome-email",
    Variables = new Dictionary<string, object?>
    {
        ["first_name"] = "Asha",
    },
});

Console.WriteLine(email.Id);
```

The published template supplies the subject and HTML. Values in `Variables` replace matching template variables such as `{{first_name}}`.

## Send One Email

```csharp
var request = new SendEmailRequest
{
    To = "customer@example.com",
    FromEmail = "hello@yourdomain.com",
    TemplateName = "account-verification",
    Variables = new Dictionary<string, object?>
    {
        ["first_name"] = "Asha",
        ["verification_url"] = "https://yourapp.com/verify/token",
    },
    ReplyToMailEngin = true,
};

var email = await client.Emails.SendAsync(request, cancellationToken);
Console.WriteLine($"Queued email {email.Id} at {email.CreatedAt}");
```

### Send request properties

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `To` | `string` | Yes | Recipient email address. |
| `TemplateName` | `string?` | Recommended | Published template API name or exact display name. |
| `TemplateId` | `string?` | No | Legacy template identifier. Prefer `TemplateName`. |
| `Variables` | `IDictionary<string, object?>?` | No | Values used to render template variables. |
| `Subject` | `string?` | Raw HTML only | Template subject override, or required subject for raw HTML. |
| `FromEmail` | `string?` | Recommended | Sender on a verified domain authorized for the API key. |
| `Html` | `string?` | Advanced | Raw HTML used when no template is supplied. |
| `ReplyToMailEngin` | `bool?` | No | Route recipient replies into the MailEngin inbox. |

Exactly one content source is required: `TemplateName`, `TemplateId`, or `Html`. Raw HTML sends also require `Subject`.

## Send Personalized Bulk Email

Bulk requests support up to 1,000 recipients. Request-level variables apply to every recipient; recipient variables take precedence.

```csharp
var job = await client.Emails.SendBulkAsync(new SendBulkEmailRequest
{
    To = new List<BulkRecipient>
    {
        new("asha@example.com", new Dictionary<string, object?>
        {
            ["first_name"] = "Asha",
        }),
        new("ben@example.com", new Dictionary<string, object?>
        {
            ["first_name"] = "Ben",
        }),
    },
    FromEmail = "hello@yourdomain.com",
    TemplateName = "product-update",
    Variables = new Dictionary<string, object?>
    {
        ["product_name"] = "MailEngin",
    },
}, cancellationToken);

Console.WriteLine($"Queued {job.QueuedCount} recipients in job {job.JobId}");
```

For recipients without individual variables:

```csharp
var job = await client.Emails.SendBulkAsync(new SendBulkEmailRequest
{
    To = new List<BulkRecipient>
    {
        new("a@example.com"),
        new("b@example.com"),
    },
    TemplateName = "maintenance-notice",
});
```

A successful bulk response confirms that recipients were queued. It is not a guarantee that every message was delivered.

## Send Raw HTML

Published templates are recommended for reusable product email. For a one-off message, provide both `Subject` and `Html`:

```csharp
var email = await client.Emails.SendAsync(new SendEmailRequest
{
    To = "user@example.com",
    FromEmail = "reports@yourdomain.com",
    Subject = "Your report is ready",
    Html = "<h1>Report ready</h1><p>You can download it now.</p>",
});
```

## Cancellation

Both send methods accept a `CancellationToken`:

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var email = await client.Emails.SendAsync(request, cancellation.Token);
```

Caller cancellation produces a `MailEnginException` with code `request_aborted`. Expiry of the SDK timeout produces `request_timeout`.

## Sender Selection

MailEngin resolves the sender in this order:

1. `FromEmail` supplied in the request.
2. Sender saved in the published Developer Template.
3. `noreply@<authorized-domain>` fallback.

The sender domain must be verified and authorized for the API key.

## Error Handling

API, timeout, cancellation, malformed-response, and network failures throw `MailEnginException`:

```csharp
try
{
    await client.Emails.SendAsync(request, cancellationToken);
}
catch (MailEnginException error)
{
    Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine(error.Status);       // HTTP status, when available
    Console.Error.WriteLine(error.ErrorCode);    // Machine-readable error code
    Console.Error.WriteLine(error.RequestId);    // Include when contacting support
    Console.Error.WriteLine(error.RetryAfter);   // Seconds supplied with HTTP 429
    Console.Error.WriteLine(error.Body);         // Original response body
    Console.Error.WriteLine(error.IsRetryable);
}
```

`IsRetryable` is true for network errors, timeouts, HTTP `408`, HTTP `429`, and `5xx` responses. The SDK never retries sends automatically because a retry could create a duplicate email until idempotency keys are supported.

Invalid local input throws `ArgumentException` before an API request is made.

## Configuration

```csharp
using var client = new MailEnginClient(
    apiKey: apiKey,
    baseUrl: "https://api.mailengin.app",
    timeout: TimeSpan.FromSeconds(15),
    httpClient: httpClient);
```

| Constructor argument | Default | Description |
| --- | --- | --- |
| `apiKey` | None | Full server-side MailEngin API key. |
| `baseUrl` | `https://api.mailengin.app` | Override for local, test, or dedicated environments. |
| `timeout` | 30 seconds | Maximum duration applied to each request. |
| `httpClient` | New client | Injectable shared or mocked `HttpClient`. |

If you inject an `HttpClient`, your application owns it and the SDK will not dispose it. If the SDK creates the client, dispose `MailEnginClient` when the application scope ends.

## ASP.NET Core Integration

Register one typed singleton and let `IHttpClientFactory` manage the HTTP transport:

```csharp
builder.Services.AddHttpClient("MailEngin");
builder.Services.AddSingleton(serviceProvider =>
{
    var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var apiKey = configuration["MailEngin:ApiKey"]
        ?? throw new InvalidOperationException("MailEngin API key is missing.");

    return new MailEnginClient(apiKey, httpClient: factory.CreateClient("MailEngin"));
});
```

Back `MailEngin:ApiKey` with an environment variable or secret manager, not a committed settings file.

## Testing

Inject an `HttpClient` backed by a custom `HttpMessageHandler` to return deterministic responses. The repository xUnit tests cover headers, field mapping, bulk limits, errors, and cancellation without requiring a real customer API key.

## Development

```bash
dotnet restore
dotnet test -c Release
dotnet pack src/MailEngin/MailEngin.csproj -c Release -o artifacts
```

See [CONTRIBUTING.md](./CONTRIBUTING.md) for contribution rules and [PUBLISHING.md](./PUBLISHING.md) for maintainer release instructions.

## Resources

- [MailEngin API documentation](https://mailengin.app/dashboard/docs)
- [Developer Templates](https://mailengin.app/dashboard/dev-templates)
- [API keys](https://mailengin.app/dashboard/api-keys)
- [Security policy](./SECURITY.md)
- [Changelog](./CHANGELOG.md)

## License

Released under the [MIT License](./LICENSE). Copyright 2026 MailEngin.
