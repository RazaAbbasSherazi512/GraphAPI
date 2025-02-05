using Microsoft.Identity.Client;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EmailService
{
    public class TokenService
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _secretId;
        private readonly string _redirectUri;
        private readonly string[] _scopes;
        private readonly string _token;

        private IPublicClientApplication _app;

        public TokenService(string clientId, string tenantId, string secretId = null, string redirectUri = "http://localhost", string[] scopes = null, string token = null)
        {
            LoadSystemDiagnosticsDiagnosticSource();
            _clientId = clientId;
            _tenantId = tenantId;
            _redirectUri = redirectUri;
            _secretId = secretId;
            _token = token;
            // Initialize the default scopes
            string[] defaultScopes = new[] { "Mail.Send" };

            _scopes = scopes != null && scopes.Any()
                ? defaultScopes.Concat(scopes).ToArray()
                : defaultScopes;
           
              
            // Initialize MSAL Public Client Application
            _app = PublicClientApplicationBuilder.Create(_clientId)
                    .WithTenantId(_tenantId)
                    .WithRedirectUri(redirectUri)
                    .Build();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_token) && GetTokenExpiry(_token) > DateTime.UtcNow)
            {
                return _token;
            }

            try
            {
                var accounts = await _app.GetAccountsAsync();
                var result = await _app.AcquireTokenSilent(_scopes, accounts.FirstOrDefault()).ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                var result = await _app.AcquireTokenInteractive(_scopes).ExecuteAsync();
                return result.AccessToken;
            }
        }

        public static DateTime GetTokenExpiry(string token)
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (expClaim != null)
            {
                var expUnix = long.Parse(expClaim);
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                return expiryDate;
            }

            throw new Exception("Expiration claim not found in token.");
        }

        private void LoadSystemDiagnosticsDiagnosticSource()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    if (args.Name.Contains("System.Diagnostics.DiagnosticSource"))
                    {
                        return Assembly.LoadFrom("System.Diagnostics.DiagnosticSource");
                    }
                    return null;
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
