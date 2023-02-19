﻿using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Rubberduck.Server.LocalDb.Properties;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Rubberduck.Server.LocalDb
{

    internal class InitializeHandler : IOnLanguageServerInitialize, IJsonRpcHandler
    {
        public async Task OnInitialize(ILanguageServer server, InitializeParams request, CancellationToken cancellationToken)
        {
            Console.WriteLine("Huzzah!");
            await Task.CompletedTask;
        }
    }

    internal static class IServiceCollectionExtensions
    {
        internal static IServiceCollection ConfigureRubberduckServerApp(this IServiceCollection services, LocalDbServerCapabilities config, CancellationTokenSource cts)
        {
            return services
                .AddRubberduckServerServices(config, cts)
                .AddJsonRpcTargets()
                .AddServerProxyServices()
                .AddConsoleProxyServices()
            ;
        }

        /// <summary>
        /// Registers <c>IJsonRpcTarget</c> implementations (RPC endpoints).
        /// </summary>
        private static IServiceCollection AddJsonRpcTargets(this IServiceCollection services)
        {
            return services
            ;
        }

        /// <summary>
        /// Registers server proxy services.
        /// </summary>
        private static IServiceCollection AddServerProxyServices(this IServiceCollection services)
        {
            return services
            ;
        }

        /// <summary>
        /// Registers console proxy services.
        /// </summary>
        private static IServiceCollection AddConsoleProxyServices(this IServiceCollection services)
        {
            return services
            ;
        }

        /// <summary>
        /// Registers server-level common services as singletons.
        /// </summary>
        private static IServiceCollection AddRubberduckServerServices(this IServiceCollection services, LocalDbServerCapabilities config, CancellationTokenSource cts)
        {
            return services
                .AddJsonRpcServer(Settings.Default.JsonRpcServerName, ConfigureRPC)
            ;
        }

        private static (Stream input, Stream output) WithAsyncNamedPipeTransport(string name)
        {
            var input = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            var output = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            return (input, output);
        }

        private static void ConfigureRPC(JsonRpcServerOptions rpc)
        {
            var (input, output) = WithAsyncNamedPipeTransport(Settings.Default.JsonRpcPipeName);            
            rpc.Concurrency = Settings.Default.MaxConcurrentRequests;

            rpc.WithRequestProcessIdentifier(new ParallelRequestProcessIdentifier())
               .WithMaximumRequestTimeout(TimeSpan.FromSeconds(10))
               .WithInput(input)
               .WithOutput(output)
            ;
        }
    }
}
