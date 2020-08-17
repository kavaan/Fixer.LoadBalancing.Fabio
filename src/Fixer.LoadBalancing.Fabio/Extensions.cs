﻿using System;
using System.Linq;
using Consul;
using Fixer.Discovery.Consul;
using Fixer.HTTP;
using Fixer.LoadBalancing.Fabio.Builders;
using Fixer.LoadBalancing.Fabio.Http;
using Fixer.LoadBalancing.Fabio.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Fixer.LoadBalancing.Fabio
{
    public static class Extensions
    {
        private const string SectionName = "fabio";
        private const string RegistryName = "loadBalancing.fabio";

        public static IFixerBuilder AddFabio(this IFixerBuilder builder, string sectionName = SectionName,
            string consulSectionName = "consul", string httpClientSectionName = "httpClient")
        {
            var fabioOptions = builder.GetOptions<FabioOptions>(sectionName);
            var consulOptions = builder.GetOptions<ConsulOptions>(consulSectionName);
            var httpClientOptions = builder.GetOptions<HttpClientOptions>(httpClientSectionName);
            return builder.AddFabio(fabioOptions, httpClientOptions,
                b => b.AddConsul(consulOptions, httpClientOptions));
        }

        public static IFixerBuilder AddFabio(this IFixerBuilder builder,
            Func<IFabioOptionsBuilder, IFabioOptionsBuilder> buildOptions,
            Func<IConsulOptionsBuilder, IConsulOptionsBuilder> buildConsulOptions,
            HttpClientOptions httpClientOptions)
        {
            var fabioOptions = buildOptions(new FabioOptionsBuilder()).Build();
            return builder.AddFabio(fabioOptions, httpClientOptions,
                b => b.AddConsul(buildConsulOptions, httpClientOptions));
        }

        public static IFixerBuilder AddFabio(this IFixerBuilder builder, FabioOptions fabioOptions,
            ConsulOptions consulOptions, HttpClientOptions httpClientOptions)
            => builder.AddFabio(fabioOptions, httpClientOptions, b => b.AddConsul(consulOptions, httpClientOptions));

        private static IFixerBuilder AddFabio(this IFixerBuilder builder, FabioOptions fabioOptions,
            HttpClientOptions httpClientOptions, Action<IFixerBuilder> registerConsul)
        {
            registerConsul(builder);
            builder.Services.AddSingleton(fabioOptions);

            if (!fabioOptions.Enabled || !builder.TryRegister(RegistryName))
            {
                return builder;
            }

            if (httpClientOptions.Type?.ToLowerInvariant() == "fabio")
            {
                builder.Services.AddTransient<FabioMessageHandler>();
                builder.Services.AddHttpClient<IFabioHttpClient, FabioHttpClient>()
                    .AddHttpMessageHandler<FabioMessageHandler>();
                builder.Services.AddHttpClient<IHttpClient, FabioHttpClient>()
                    .AddHttpMessageHandler<FabioMessageHandler>();
            }

            using (var serviceProvider = builder.Services.BuildServiceProvider())
            {
                var registration = serviceProvider.GetService<AgentServiceRegistration>();
                registration.Tags = GetFabioTags(registration.Name, fabioOptions.Service);
                builder.Services.UpdateConsulRegistration(registration);
            }

            return builder;
        }

        public static void AddFabioHttpClient(this IFixerBuilder builder, string clientName, string serviceName)
            => builder.Services.AddHttpClient<IHttpClient, FabioHttpClient>(clientName)
                .AddHttpMessageHandler(c => new FabioMessageHandler(c.GetService<FabioOptions>(), serviceName));

        private static void UpdateConsulRegistration(this IServiceCollection services,
            AgentServiceRegistration registration)
        {
            var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(AgentServiceRegistration));
            services.Remove(serviceDescriptor);
            services.AddSingleton(registration);
        }

        private static string[] GetFabioTags(string consulService, string fabioService)
        {
            var service = (string.IsNullOrWhiteSpace(fabioService) ? consulService : fabioService)
                .ToLowerInvariant();

            return new[] { $"urlprefix-/{service} strip=/{service}" };
        }
    }
}
