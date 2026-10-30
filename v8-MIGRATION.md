# Migration guide to Monq.Core.HttpClientExtensions 8.0.0

This guide describes how to migrate static authentication from the configured
`Authentication:AuthenticationEndpoint` key to addresses resolved from an
application name (microservice addressing).

## Breaking changes in 8.0.0

- Target frameworks `net6.0` and `net7.0` were removed. Supported: `net8.0`,
  `net9.0`, `net10.0`.
- The configuration-based overloads `ConfigureStaticAuthentication()` and
  `ConfigureStaticAuthentication(Action<IHttpClientBuilder>)` no longer read
  the `Authentication:AuthenticationEndpoint` configuration key. Their
  signatures were kept, and they now resolve the default authentication
  service (`HostBuilderExtensions.DefaultAuthenticationApplicationName`,
  `pl-identity-service`) with the configured HTTP address resolver.
- `MissingConfigurationException` is no longer thrown for the removed
  `Authentication:AuthenticationEndpoint` key. It is now thrown when the
  remaining required configuration is missing:
  `Authentication:Client:Login` or `Authentication:Client:Password`.

## When migration is needed

Migrate only when the service relied on the address taken from
`Authentication:AuthenticationEndpoint` (usually the platform ingress URL) and
that behavior must be preserved explicitly — for example, in a local
development run outside Kubernetes.

In-cluster deployments need no code changes: keep calling
`ConfigureStaticAuthentication()` and the endpoint is resolved from the
application name.

## How to replace the configured endpoint

Before 8.0.0 the endpoint was read from configuration:

```json
{
  "Authentication": {
    "AuthenticationEndpoint": "https://identity.example.com",
    "Client": {
        "Login": "idp-client",
        "Password": "idp-client-secret"
    }
  }
}
```

```csharp
hostBuilder.ConfigureStaticAuthentication();
// the endpoint came from Authentication:AuthenticationEndpoint
```

In 8.0.0 the same behavior is explicit. If the value of
`Authentication:AuthenticationEndpoint` must be preserved, pass it directly
and remove the key:

```csharp
hostBuilder.ConfigureStaticAuthentication(new Uri("https://identity.example.com"));
```

To read the old key from your own application configuration during the
transition, build the URI where it is convenient — the library no longer does
this for you:

```csharp
hostBuilder.ConfigureStaticAuthentication(
    new Uri(builder.Configuration["Authentication:AuthenticationEndpoint"]!));
```

In-cluster deployments need no replacement at all: keep calling
`ConfigureStaticAuthentication()` and the endpoint is resolved from the
default application name.

To resolve the authentication service through a different resolver or
application name:

```csharp
hostBuilder.ConfigureStaticAuthentication(
    "identity-service",
    port: 80,
    addressResolver: myResolver);
```

## Configuration cleanup

The `Authentication` section is still required for client credentials
(`Client:Login`, `Client:Password`) and optional `RequireHttpsMetadata`.

The legacy `Authentication:EnableCaching` key was never read by this library
and does nothing here. Token caching for static authentication is not a
library option: the system token is reused until its server-provided
`expires_in` elapses, with a forced refresh on a `401` response. There is no
introspection in this library — incoming-token introspection and its cache
duration are configured in `Monq.Core.BasicDotNetMicroservice` via
`Authentication:CacheDurationSeconds` (default 300; the Duende OAuth2
introspection handler always caches, so the old `EnableCaching` switch is not
supported anywhere anymore).

Only the `Authentication:AuthenticationEndpoint` key becomes dead
configuration for services on 8.0.0. Remove it from Consul and application
configuration after **all** consumers are migrated. Note that the deprecated
`ConfigureMonqAuthentication(IServiceCollection, IConfiguration)` overload in
`Monq.Core.BasicDotNetMicroservice` still reads this key until every consumer
is migrated to the application-name overload (see
`service-discovery-MIGRATION.md` in that repository).

## Related documents

- [README.md](README.md) — current static authentication API;
- `service-discovery-MIGRATION.md` in `Monq.Core.BasicDotNetMicroservice` —
  client addressing migration and the legacy introspection overload.
