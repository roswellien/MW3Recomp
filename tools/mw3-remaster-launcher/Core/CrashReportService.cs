using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MW3RemasterLauncher.Core;

public sealed record CrashReportResult(string ReportId, bool ExistingIssueUpdated, bool Overflow, bool ArchivedOnly = false);

/// <summary>Submit user-approved, redacted crash diagnostics to the project support endpoint.</summary>
public sealed class CrashReportService
{
    private static readonly HttpClient SharedClient = CreateClient();
    private readonly HttpClient _client;

    public CrashReportService(HttpClient? client = null)
    {
        _client = client ?? SharedClient;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CoreCriticalLauncher-CrashReports/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task<CrashReportResult> SubmitAsync(object report, CancellationToken cancellationToken = default)
    {
        var configuredEndpoint = Environment.GetEnvironmentVariable("CORE_CRITICAL_CRASH_REPORT_URL");
        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps || !endpoint.IsDefaultPort ||
            endpoint.UserInfo.Length != 0 || endpoint.Fragment.Length != 0)
            throw new InvalidOperationException("Crash reporting is not configured for this build. Export a local support report instead.");
        using var response = await _client.PostAsJsonAsync(endpoint, report, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Response? result;
        try
        {
            result = JsonSerializer.Deserialize<Response>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException) when (!response.IsSuccessStatusCode)
        {
            // A proxy or access gateway may return plain text or HTML instead of the API's JSON error.
            result = null;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("The crash report service returned invalid JSON.", error);
        }
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                (result?.Error ?? "The crash report service is unavailable.") +
                " HTTP " + (int)response.StatusCode + ".", null, response.StatusCode);
        if (result?.ReportId == null || result.ReportId.Length is < 1 or > 16 ||
            result.ReportId.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("The crash report service returned an invalid report ID.");

        return new CrashReportResult(result.ReportId, result.Duplicate, result.Overflow, result.ArchivedOnly);
    }

    private sealed record Response(
        [property: JsonPropertyName("report_id")] string? ReportId,
        bool Duplicate,
        bool Overflow,
        [property: JsonPropertyName("archived_only")] bool ArchivedOnly,
        string? Error);
}
