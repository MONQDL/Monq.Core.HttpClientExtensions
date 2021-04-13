using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Методы расширения для сборщика универсального узла приложения (<see cref="IHostBuilder"/>).
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Применить конфигурацию обработки http-запроса для <see cref="RestHttpClient"/>.
        /// </summary>
        /// <param name="hostBuilder">Сборщик универсального узла приложения.</param>
        /// <param name="setupAction">Конфигуратор базового http-клиента.</param>
        public static IHostBuilder ConfigBasicHttpService(this IHostBuilder hostBuilder, Action<BasicHttpServiceOptions> setupAction) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    config.Configure(setupAction);
                    config.AddOptions();
                    config.AddSingleton(resolver => resolver.GetRequiredService<IOptions<BasicHttpServiceOptions>>().Value);
                });
    }
}