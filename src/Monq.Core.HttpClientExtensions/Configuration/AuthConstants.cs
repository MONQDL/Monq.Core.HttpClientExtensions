namespace Monq.Core.HttpClientExtensions
{
    internal static class AuthConstants
    {
        internal static class Authentication
        {
            public const string AuthenticationSection = "Authentication";
            public const string AuthenticationEndpoint = "AuthenticationEndpoint";
            public const string ClientName = "Client:Login";
            public const string ClientSecret = "Client:Password";

            public const string RequireHttpsMetadata = "RequireHttpsMetadata";
            public const string EnableCaching = "EnableCaching";
        }
    }
}
