﻿using Rubberduck.RPC.Platform;
using Rubberduck.RPC.Platform.Metadata;
using Rubberduck.RPC.Proxy.SharedServices.Server.Commands;
using Rubberduck.RPC.Proxy.SharedServices.Server.Configuration;
using Rubberduck.RPC.Proxy.SharedServices.Server.Model;
using System;
using System.Threading.Tasks;

namespace Rubberduck.RPC.Proxy.SharedServices
{
    /// <summary>
    /// Represents a client-side notifications provider.
    /// </summary>
    /// <remarks>
    /// This interface is implemented on the client side with automatic StreamJsonRpc proxies.
    /// </remarks>
    public interface IServerProxyClient<TServerCapabilities> : IJsonRpcSource
        where TServerCapabilities : SharedServerCapabilities, new()
    {
        /// <summary>
        /// A notification sent from the client to ask the server to exit its process.
        /// </summary>
        /// <remarks>
        /// The server should exit with code 0 (success) if a <see cref="Shutdown"/> request has been received before; otherwise the server process should exit with code 1 (error).
        /// </remarks>
        [LspCompliant(JsonRpcMethods.ServerProxyRequests.Shared.Server.Exit)]
        event EventHandler RequestExit;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task OnRequestExitAsync();

        /// <summary>
        /// A <c>Initialized</c> notification is sent <em>from the client to the server</em> after the client received an <c>InitializeResult</c>,
        /// but before the client is sending any other requests or notification to the server.
        /// </summary>
        /// <remarks>
        /// Per LSP an <c>Initialized</c> notification may only be sent once (assumed: <em>per client</em>).
        /// </remarks>
        [LspCompliant(JsonRpcMethods.ServerProxyRequests.Shared.Server.Initialized)]
        event EventHandler<InitializedParams> Initialized;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task OnInitializedAsync(InitializedParams parameter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task<InitializeResult<TServerCapabilities>> InitializeClientAsync(InitializeParams<TServerCapabilities> parameter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task ShutdownClientAsync(ClientShutdownParams parameter);
    }
}