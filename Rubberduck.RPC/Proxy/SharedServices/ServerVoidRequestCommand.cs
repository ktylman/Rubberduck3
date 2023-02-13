﻿using Rubberduck.RPC.Platform.Exceptions;
using Rubberduck.RPC.Proxy.SharedServices.Abstract;
using Rubberduck.RPC.Proxy.SharedServices.Server.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rubberduck.RPC.Proxy.SharedServices
{
    public abstract class ServerVoidRequestCommand<TParameter, TOptions> : ServerCommandBase<TOptions>, IServerNotificationCommand<TParameter>
        where TParameter : class, new()
        where TOptions : class, new()
    {
        protected ServerVoidRequestCommand(IServerLogger logger, GetServerOptionsAsync<TOptions> getConfiguration, GetServerStateInfoAsync getCurrentServerState) 
            : base(logger, getConfiguration, getCurrentServerState)
        {
        }

        /// <summary>
        /// The expected <c>ServerState</c> values when executing this command.
        /// </summary>
        /// <remarks>
        /// Executing the command despite the expected server state may throw an <c>InvalidStateException</c>.
        /// Unless overriden, expected server states only include <c>ServerState.Initialized</c>.
        /// </remarks>
        protected virtual IReadOnlyCollection<ServerStatus> ExpectedServerStates { get; } = new[] { ServerStatus.Initialized, };

        /// <summary>
        /// The actual command implementation.
        /// </summary>
        /// <remarks>
        /// Server state is valid and exceptions are handled when this method is invoked.
        /// </remarks>
        protected abstract Task ExecuteInternalAsync(TParameter parameter);

        /// <summary>
        /// Throws an exception if the server is not in a valid state for this command.
        /// </summary>
        /// <exception cref="InvalidStateException"></exception>
        protected async Task ThrowOnUnexpectedServerStateAsync()
        {
            var state = (await GetCurrentServerStateInfoAsync()).Status;
            if (ExpectedServerStates.Any() && !ExpectedServerStates.Contains(state))
            {
                throw new InvalidStateException(GetType().Name, state, ExpectedServerStates.ToArray());
            }
        }

        public Func<TParameter, Task> ExecuteAction => parameter => ExecuteAsync(parameter);

        public Func<TParameter, Task<bool>> CanExecuteFunc => parameter => CanExecuteAsync(parameter);

        public virtual async Task<bool> CanExecuteAsync(TParameter parameter)
        {
            var state = (await GetCurrentServerStateInfoAsync()).Status;
            var result = ExpectedServerStates.Contains(state);

            return await Task.FromResult(result);
        }

        public async Task ExecuteAsync(TParameter parameter)
        {
            try
            {
                Logger.OnTrace($"Executing command '{Name}'.", verbose: Description);

                await ThrowOnUnexpectedServerStateAsync();
                await ExecuteInternalAsync(parameter);
            }
            catch (ApplicationException exception)
            {
                Logger.OnError(exception);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                Logger.OnError(exception);
                throw;
            }
        }

        public Task<bool> TryExecuteAsync(TParameter parameter)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ServerVoidRequestCommand<TOptions> : ServerCommandBase<TOptions>, IServerNotificationCommand
        where TOptions : class, new()
    {
        protected ServerVoidRequestCommand(IServerLogger logger, GetServerOptionsAsync<TOptions> getConfiguration, GetServerStateInfoAsync getCurrentServerState)
            : base(logger, getConfiguration, getCurrentServerState)
        {
        }

        /// <summary>
        /// The expected <c>ServerState</c> values when executing this command.
        /// </summary>
        /// <remarks>
        /// Executing the command despite the expected server state may throw an <c>InvalidStateException</c>.
        /// Unless overriden, expected server states only include <c>ServerState.Initialized</c>.
        /// </remarks>
        protected virtual IReadOnlyCollection<ServerStatus> ExpectedServerStates { get; } = new[] { ServerStatus.Initialized, };

        /// <summary>
        /// The actual command implementation.
        /// </summary>
        /// <param name="token">A request-scoped cancellation token, for cooperative request cancellation.</param>
        /// <remarks>
        /// Server state is valid and exceptions are handled when this method is invoked; implementation should periodically check the token and throw if cancellation is requested.
        /// </remarks>
        protected abstract Task ExecuteInternalAsync();

        /// <summary>
        /// Throws an exception if the server is not in a valid state for this command.
        /// </summary>
        /// <exception cref="InvalidStateException"></exception>
        protected async Task ThrowOnUnexpectedServerStateAsync()
        {
            var state = (await GetCurrentServerStateInfoAsync()).Status;
            if (ExpectedServerStates.Any() && !ExpectedServerStates.Contains(state))
            {
                throw new InvalidStateException(GetType().Name, state, ExpectedServerStates.ToArray());
            }
        }

        public Func<Task> ExecuteAction => () => ExecuteAsync();

        public Func<Task<bool>> CanExecuteFunc => () => CanExecuteAsync();

        public virtual async Task<bool> CanExecuteAsync()
        {
            var state = (await GetCurrentServerStateInfoAsync()).Status;
            var result = ExpectedServerStates.Contains(state);

            return await Task.FromResult(result);
        }

        public async Task ExecuteAsync()
        {
            try
            {
                Logger.OnTrace($"Executing command '{Name}'.", verbose: Description);

                await ThrowOnUnexpectedServerStateAsync();
                await ExecuteInternalAsync();
            }
            catch (ApplicationException exception)
            {
                Logger.OnError(exception);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                Logger.OnError(exception);
                throw;
            }
        }

        public Task<bool> TryExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}