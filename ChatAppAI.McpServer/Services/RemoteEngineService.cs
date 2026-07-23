using System.Net;
using Design.ORiN3.RemoteEngineEx.V1;
using Grpc.Net.Client;
using Message.Client.ORiN3.RemoteEngine.V1;

namespace ChatAppAI.McpServer.Services;

public sealed record ProviderProcessInfo(Guid Id, string Name, string Version, int Pid, string ProviderId);

public sealed record AvailableProviderInfo(
    string Id,
    string Version,
    string Title,
    string Description,
    string Authors,
    string ProjectUrl,
    string ProviderConfigData);

public sealed record ManualLink(string Text, string Uri);

public sealed record ProviderManual(string Uri, string Culture, string Text, IReadOnlyList<ManualLink> Links);

public sealed class RemoteEngineService(IHttpClientFactory httpClientFactory)
{
    private const uint TimeoutIntervalMilliseconds = 10_000;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<IReadOnlyList<AvailableProviderInfo>> ListProvidersAsync(
        string remoteEngineUri,
        string? providerName,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(remoteEngineUri, async client =>
        {
            var providers = new List<AvailableProviderInfo>();
            await foreach (var result in client.GetAvailableProvidersAsync(GetAvailableProvidersOption.All, cancellationToken))
            {
                var package = result.ProviderPackageInfo;
                if (string.IsNullOrWhiteSpace(package.Id) ||
                    (!string.IsNullOrWhiteSpace(providerName) && !MatchesProviderName(package, providerName)))
                {
                    continue;
                }

                providers.Add(new AvailableProviderInfo(
                    package.Id,
                    package.Version,
                    package.Title,
                    package.Description,
                    package.Authors,
                    package.ProjectUrl,
                    result.ProviderConfigData));
            }

            return providers;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProviderProcessInfo> ResolveProviderAsync(
        string remoteEngineUri,
        string? providerName,
        int? pid,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerName) && pid is null)
        {
            throw new ArgumentException("providerName または pid のどちらかを指定してください。");
        }

        var providers = await ListRunningProvidersAsync(remoteEngineUri, providerName, cancellationToken).ConfigureAwait(false);
        var provider = pid is not null
            ? providers.FirstOrDefault(item => item.Pid == pid.Value)
            : providers.FirstOrDefault();

        return provider ?? throw new InvalidOperationException("指定されたプロバイダーを特定できませんでした。");
    }

    private static bool MatchesProviderName(IProviderPackageInfo package, string providerName)
    {
        return package.Id.Contains(providerName, StringComparison.OrdinalIgnoreCase) ||
            package.Title.Contains(providerName, StringComparison.OrdinalIgnoreCase) ||
            package.Description.Contains(providerName, StringComparison.OrdinalIgnoreCase) ||
            package.Tags.Contains(providerName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<ProviderProcessInfo>> ListRunningProvidersAsync(
        string remoteEngineUri,
        string? providerName,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(remoteEngineUri, async client =>
        {
            var providers = new List<ProviderProcessInfo>();
            await foreach (var provider in client.ListProviderProcessAsync(providerName ?? string.Empty, cancellationToken))
            {
                providers.Add(new ProviderProcessInfo(provider.Id, provider.Name, provider.Version, provider.Pid, provider.ProviderId));
            }

            return providers;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task TerminateProviderAsync(
        string remoteEngineUri,
        string? providerName,
        int? pid,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(remoteEngineUri, providerName, pid, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync(
            remoteEngineUri,
            async client =>
            {
                await client.TerminateProviderAsync(provider.Id, cancellationToken).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProviderManual> GetProviderManualAsync(
        string remoteEngineUri,
        string providerId,
        string version,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var manualUris = await ExecuteAsync(
            remoteEngineUri,
            client => client.GetManualUrisAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var orderedUris = manualUris
            .Select(TryCreateManualBaseUri)
            .Where(uri => uri is not null)
            .Select(uri => uri!)
            .OrderByDescending(uri => uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

        var cultures = new[] { "ja", "default", "en" };
        foreach (var baseUri in orderedUris)
        {
            foreach (var culture in cultures)
            {
                var manualUri = BuildProviderManualUri(baseUri, providerId, version, culture);
                try
                {
                    using var response = await _httpClientFactory.CreateClient().GetAsync(manualUri, cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    if (response.RequestMessage?.RequestUri is not Uri resolvedUri || !IsUnderManualRoot(resolvedUri, baseUri))
                    {
                        continue;
                    }

                    var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return ParseManual(manualUri, baseUri, culture, html);
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
            }
        }

        throw new HttpRequestException("利用可能な provider マニュアル endpoint が見つかりませんでした。");
    }

    private static Uri? TryCreateManualBaseUri(string uriString)
    {
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return null;
        }

        return uri;
    }

    private static Uri BuildProviderManualUri(Uri baseUri, string providerId, string version, string culture)
    {
        var root = baseUri.AbsoluteUri.TrimEnd('/') + "/manual/provider/";
        return new Uri($"{root}{Uri.EscapeDataString(providerId)}/{Uri.EscapeDataString(version)}/{culture}/", UriKind.Absolute);
    }

    private static bool IsUnderManualRoot(Uri uri, Uri baseUri)
    {
        var manualRoot = baseUri.AbsoluteUri.TrimEnd('/') + "/manual/";
        return uri.Scheme == baseUri.Scheme &&
            uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase) &&
            uri.Port == baseUri.Port &&
            uri.AbsolutePath.StartsWith(new Uri(manualRoot).AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    private static ProviderManual ParseManual(Uri uri, Uri baseUri, string culture, string html)
    {
        var document = new HtmlAgilityPack.HtmlDocument();
        document.LoadHtml(html);

        var text = WebUtility.HtmlDecode(document.DocumentNode.InnerText).Trim();
        var links = document.DocumentNode
            .SelectNodes("//a[@href]")?
            .Select(anchor =>
            {
                var linkUri = new Uri(uri, anchor.GetAttributeValue("href", string.Empty));
                return new ManualLink(WebUtility.HtmlDecode(anchor.InnerText).Trim(), linkUri.ToString());
            })
            .Where(link => Uri.TryCreate(link.Uri, UriKind.Absolute, out var linkUri) && IsUnderManualRoot(linkUri, baseUri))
            .DistinctBy(link => link.Uri)
            .ToArray() ?? [];

        return new ProviderManual(uri.ToString(), culture, text, links);
    }

    private static async Task<TResult> ExecuteAsync<TResult>(
        string remoteEngineUri,
        Func<IRemoteEngineEx, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(remoteEngineUri, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new ArgumentException("remoteEngineUri には http または https の URI を指定してください。", nameof(remoteEngineUri));
        }

        using var channel = GrpcChannel.ForAddress(endpoint);
        using var client = await RemoteEngine.AttachAsync(channel, TimeoutIntervalMilliseconds, cancellationToken).ConfigureAwait(false);
        return await operation(client).ConfigureAwait(false);
    }
}