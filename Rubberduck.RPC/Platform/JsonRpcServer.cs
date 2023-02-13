﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;
using Rubberduck.RPC.Platform.Model;
using Rubberduck.RPC.Proxy.SharedServices.Server.Configuration;
using System.IO.Pipes;
using System.Linq;

namespace Rubberduck.RPC.Platform
{
    /// <summary>
    /// Represents a client-side RPC server process that can respond to server-to-client requests over a named pipe stream.
    /// </summary>
    public abstract class NamedPipeClientSideJsonRpcServer<TServerService, TOptions, TInitializeParams> : NamedPipeJsonRpcServer<TServerService, TOptions, TInitializeParams>
        where TServerService : ServerService<TOptions, TInitializeParams>
        where TOptions : SharedServerCapabilities, new()
        where TInitializeParams : class, new()
    {
        protected NamedPipeClientSideJsonRpcServer(IServiceProvider serviceProvider, IEnumerable<Type> clientProxyTypes) 
            : base(serviceProvider, clientProxyTypes)
        {
        }
    }

    /// <summary>
    /// Represents a server process that communicates over named pipes.
    /// </summary>
    /// <remarks>
    /// Implementation holds the server state for the lifetime of the host process.
    /// </remarks>
    public abstract class NamedPipeJsonRpcServer<TServerService, TOptions, TInitializeParams> : JsonRpcServer<NamedPipeServerStream, TServerService, TOptions, TInitializeParams>
        where TServerService : ServerService<TOptions, TInitializeParams>
        where TOptions : SharedServerCapabilities, new()
        where TInitializeParams : class, new()
    {
        protected NamedPipeJsonRpcServer(IServiceProvider serviceProvider, IEnumerable<Type> clientProxyTypes) 
            : base(serviceProvider, clientProxyTypes) { }
    }

    /// <summary>
    /// Represents a server process.
    /// </summary>
    /// <remarks>
    /// Implementation holds the server state for the lifetime of the host process.
    /// </remarks>
    public abstract class JsonRpcServer<TStream, TServerService, TOptions, TInitializeParams> : BackgroundService, IJsonRpcServer
        where TStream : Stream
        where TServerService : ServerService<TOptions, TInitializeParams>
        where TOptions : SharedServerCapabilities, new()
        where TInitializeParams : class, new()
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IRpcStreamFactory<TStream> _rpcStreamFactory;
        private readonly IEnumerable<Type> _clientProxyTypes;
        protected JsonRpcServer(IServiceProvider serviceProvider, IEnumerable<Type> clientProxyTypes)
        {
            _serviceProvider = serviceProvider;

            _rpcStreamFactory = serviceProvider.GetService<IRpcStreamFactory<TStream>>();
            _clientProxyTypes = clientProxyTypes;
        }

        public abstract ServerState Info { get; }

        /// <summary>
        /// Wait for a client to connect.
        /// </summary>
        protected abstract Task WaitForConnectionAsync(TStream stream, CancellationToken token);

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <remarks>
        /// Creates a new RPC stream for each new client connection, until cancellation is requested on the token.
        /// </remarks>
        protected override async Task ExecuteAsync(CancellationToken serverToken)
        {
            Console.WriteLine("Registered RPC server targets:");
            foreach (var proxy in _serviceProvider.GetService<IEnumerable<IJsonRpcTarget>>())
            {
                Console.WriteLine($" • {proxy.GetType().Name}");
            }

            Console.WriteLine("Server started. Awaiting client connection...");
            while (!serverToken.IsCancellationRequested)
            {
                try
                {
                    // our stream only buffers a single message, so we need a new one every time:
                    var stream = _rpcStreamFactory.CreateNew();

                    await WaitForConnectionAsync(stream, serverToken);
                    await SendResponseAsync(stream, serverToken);
                }
                catch (OperationCanceledException) when (Thread.CurrentThread.IsBackground)
                {
                    await Task.Yield();
                }
            }

            Console.WriteLine("Server has stopped.");
            serverToken.ThrowIfCancellationRequested();
        }

        private static readonly JsonRpcTargetOptions _targetOptions = new JsonRpcTargetOptions
        {
            MethodNameTransform = JsonRpcNameTransforms.MethodNameTransform,
            EventNameTransform = JsonRpcNameTransforms.EventNameTransform,
            AllowNonPublicInvocation = false,
            ClientRequiresNamedArguments = false,
            DisposeOnDisconnect = true,
            NotifyClientOfEvents = true,
            UseSingleObjectParameterDeserialization = true,
        };

        private static readonly JsonRpcProxyOptions _proxyOptions = new JsonRpcProxyOptions
        {
            EventNameTransform = JsonRpcNameTransforms.EventNameTransform,
            MethodNameTransform = JsonRpcNameTransforms.MethodNameTransform,
            ServerRequiresNamedArguments = true,
        };

        private async Task SendResponseAsync(Stream stream, CancellationToken token)
        {
            using (var rpc = new JsonRpc(stream))
            {
                var clientProxies = new List<object>();
                foreach (var type in _clientProxyTypes)
                {
                    clientProxies.Add(rpc.Attach(type, _proxyOptions));
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var serverProxies = scope.ServiceProvider.GetService<IEnumerable<IJsonRpcTarget>>();
                    foreach (var proxy in serverProxies)
                    {
                        proxy.Initialize(clientProxies.Select(client => client as IJsonRpcSource));
                        rpc.AddLocalRpcTarget(proxy, _targetOptions);
                    }

                    token.ThrowIfCancellationRequested();

                    rpc.StartListening();
                    await rpc.Completion;
                }
            }
        }
    }

    public static class JsonRpcNameTransforms
    {
        public static string EventNameTransform(string name)
        {
            if (RpcMethodNameMappings.IsMappedEvent(name, out var mapped))
            {
                return mapped;
            }

            var camelCased = CommonMethodNameTransforms.CamelCase(name);
            System.Diagnostics.Debug.WriteLine($"Event '{name}' is not mapped to an explicit RPC method name: method is mapped to '{camelCased}'.");

            return camelCased;
        }

        public static string MethodNameTransform(string name)
        {
            if (RpcMethodNameMappings.IsMappedEvent(name, out var mapped))
            {
                return mapped;
            }

            var camelCased = CommonMethodNameTransforms.CamelCase(name);
            System.Diagnostics.Debug.WriteLine($"Method '{name}' is not mapped to an explicit RPC method name: method is mapped to '{camelCased}'.");

            return camelCased;
        }
    }
}