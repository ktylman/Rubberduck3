﻿using Rubberduck.RPC.Proxy.SharedServices.Abstract;
using Rubberduck.RPC.Proxy.SharedServices.Server.Configuration;
using Rubberduck.RPC.Proxy.SharedServices.Server.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rubberduck.RPC.Proxy.SharedServices.Server.Commands
{
    /// <summary>
    /// A base implementation for an <c>Initialize</c> command that should be implemented in all Rubberduck.Server projects.
    /// </summary>
    /// <typeparam name="TServerOptions">The specific class type of the server configuration options.</typeparam>
    public abstract class InitializeCommand<TServerOptions, TInitializeParams> : ServerRequestCommand<TInitializeParams, InitializeResult<TServerOptions>, TServerOptions>
        where TServerOptions : SharedServerCapabilities, new()
        where TInitializeParams : class, new()
    {
        public InitializeCommand(IServerLogger logger, GetServerOptionsAsync<TServerOptions> getConfiguration, GetServerStateInfoAsync getServerState)
            : base(logger, getConfiguration, getServerState)
        {
        }

        public override string Description { get; } = "A command that handles the (normally) first request from the client to the server.";

        /// <summary>
        /// Processes the initialization request by adding the client info to server state, and gets to configures server settings accordingly with client capabilities.
        /// </summary>
        /// <param name="client">The client initialization parameters.</param>
        /// <returns>The server configuration.</returns>
        protected abstract Task<TServerOptions> ExecuteInternalAsync(TInitializeParams clientConfig, TServerOptions serverConfig, CancellationToken token);

        protected override IReadOnlyCollection<ServerStatus> ExpectedServerStates { get; } = new[] { ServerStatus.Started };

        protected sealed override async Task<InitializeResult<TServerOptions>> ExecuteInternalAsync(TInitializeParams parameter, CancellationToken token)
        {
            var initialConfig = await GetConfigurationAsync.Invoke();
            var serverConfig = await ExecuteInternalAsync(parameter, initialConfig, token);

            var info = await GetCurrentServerStateInfoAsync.Invoke();

            return await Task.FromResult(new InitializeResult<TServerOptions>
            {
                ServerInfo = new InitializeResult<TServerOptions>.ServerInformation
                {
                    Name = info.Name,
                    ProcessId = info.ProcessId,
                    StartTimestamp = info.StartTime.GetValueOrDefault(),
                    Version = info.Version
                },
                Capabilities = serverConfig
            });
        }
    }
}
