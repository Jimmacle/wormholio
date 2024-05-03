using System.Text;
using System.Text.Json;
using System.Web;

namespace Wormholio.Esi;

public sealed class EsiClient
{
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public EsiClient(string clientId, string secret, string redirectUri)
    {
        _clientId = clientId;
        _secret = secret;
        _redirectUri = redirectUri;
    }

    // https://docs.esi.evetech.net/docs/sso/web_based_sso_flow.html
    public string GenerateOAuthUrl(IEnumerable<string> scopes)
    {
        var query = QueryString.Create(new KeyValuePair<string, string?>[]
        {
            new("response_type", "code"),
            new("redirect_uri", _redirectUri),
            new("client_id", _clientId),
            new("scope", string.Join(" ", scopes)),
            new("state", Guid.NewGuid().ToString())
        });

        return $"https://login.eveonline.com/v2/oauth/authorize{query}";
    }

    // https://docs.esi.evetech.net/docs/sso/web_based_sso_flow.html
    public async Task<EsiTokens> GetTokens(string code)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_secret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://login.eveonline.com/v2/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code
            }),
            Headers =
            {
                { "Authorization", $"Basic {credentials}"},
                { "Host", "login.eveonline.com" }
            }
        };

        var response = await _httpClient.SendAsync(request);
        var tokens = await response.Content.ReadFromJsonAsync<EsiTokensResponse>(JsonOptions);
        return new EsiTokens(tokens.AccessToken, tokens.RefreshToken, DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn));
    }

    private record EsiTokensResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}

public sealed record EsiTokens(string AccessToken, string RefreshToken, DateTimeOffset Expires);