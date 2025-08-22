using System.Threading;
using System.Threading.Tasks;
using Wolverine;

namespace OmniSharp.Extensions.JsonRpc
{
    // Wolverine-style handler interfaces that match the MediatR pattern
    public interface IJsonRpcHandler
    {
    }

    public interface IWolverineRequestHandler<in TRequest, TResponse> : IJsonRpcHandler
        where TRequest : IJsonRpcRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public interface IWolverineRequestHandler<in TRequest> : IJsonRpcHandler
        where TRequest : IJsonRpcRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken);
    }

    public interface IWolverineNotificationHandler<in TNotification> : IJsonRpcHandler
        where TNotification : IJsonRpcNotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    // Adapter to make Wolverine handlers work with the existing infrastructure
    public static class WolverineHandlerAdapter
    {
        public static async Task<TResponse> HandleAsync<TRequest, TResponse>(
            IWolverineRequestHandler<TRequest, TResponse> handler,
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : IJsonRpcRequest<TResponse>
        {
            return await handler.Handle(request, cancellationToken);
        }

        public static async Task HandleAsync<TRequest>(
            IWolverineRequestHandler<TRequest> handler,
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : IJsonRpcRequest
        {
            await handler.Handle(request, cancellationToken);
        }

        public static async Task HandleAsync<TNotification>(
            IWolverineNotificationHandler<TNotification> handler,
            TNotification notification,
            CancellationToken cancellationToken)
            where TNotification : IJsonRpcNotification
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}