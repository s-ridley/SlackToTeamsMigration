using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8767

namespace STMigration.Utils {
    public class TokenProvider : IAccessTokenProvider {

        private readonly IConfidentialClientApplication _app;

        public TokenProvider(IConfidentialClientApplication app) {
            _app = app;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default) {
            string[] scopes = ["https://graph.microsoft.com/.default"];
            var result = _app.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken).Result;
            return Task.FromResult(result.AccessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }
    }
}
