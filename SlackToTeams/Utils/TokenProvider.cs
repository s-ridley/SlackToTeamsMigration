using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8767
#pragma warning disable IDE0290

namespace SlackToTeams.Utils {
    public class TokenProvider : IAccessTokenProvider {
        #region Fields

        private readonly IConfidentialClientApplication _app;

        #endregion
        #region Properties

        public AllowedHostsValidator AllowedHostsValidator { get; }

        #endregion
        #region Constructors

        public TokenProvider(IConfidentialClientApplication app) {
            _app = app;
        }

        #endregion
        #region Method - GetAuthorizationTokenAsync

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default) {
            string[] scopes = ["https://graph.microsoft.com/.default"];
            var result = _app.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken).Result;
            return Task.FromResult(result.AccessToken);
        }

        #endregion
    }
}
