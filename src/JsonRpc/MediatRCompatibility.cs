using System;
using System.Threading;
using System.Threading.Tasks;
using Wolverine;

namespace OmniSharp.Extensions.JsonRpc
{
    // Compatibility layer to maintain existing MediatR interfaces while using Wolverine
    namespace MediatR
    {
        // MediatR interface replacements that redirect to Wolverine
        public interface IRequest<out TResponse>
        {
        }

        public interface IRequest
        {
        }

        public interface INotification
        {
        }

        public interface IRequestHandler<in TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
        }

        public interface IRequestHandler<in TRequest>
            where TRequest : IRequest
        {
            Task Handle(TRequest request, CancellationToken cancellationToken);
        }

        public interface INotificationHandler<in TNotification>
            where TNotification : INotification
        {
            Task Handle(TNotification notification, CancellationToken cancellationToken);
        }

        public struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
        {
            public static readonly Unit Value = new Unit();

            public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

            public int CompareTo(Unit other) => 0;

            public int CompareTo(object? obj) => 0;

            public override int GetHashCode() => 0;

            public bool Equals(Unit other) => true;

            public override bool Equals(object? obj) => obj is Unit;

            public static bool operator ==(Unit left, Unit right) => true;

            public static bool operator !=(Unit left, Unit right) => false;

            public override string ToString() => "()";
        }

        // Mediator interface that delegates to Wolverine
        public interface IMediator
        {
            Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
            Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
            Task Publish(object notification, CancellationToken cancellationToken = default);
            Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
        }

        // Mediator implementation that uses Wolverine's message bus
        public class Mediator : IMediator
        {
            private readonly IMessageBus _messageBus;
            private readonly IServiceProvider _serviceProvider;

            public Mediator(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
                _messageBus = (IMessageBus?)serviceProvider.GetService(typeof(IMessageBus)) ?? throw new InvalidOperationException("IMessageBus not registered");
            }

            public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                // For now, we'll use service locator pattern to find handlers
                var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
                var handler = _serviceProvider.GetService(handlerType);
                
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for {request.GetType().Name}");
                }

                var handleMethod = handlerType.GetMethod("Handle");
                var result = await (Task<TResponse>)handleMethod!.Invoke(handler, new object[] { request, cancellationToken })!;
                return result;
            }

            public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            {
                var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
                var handler = _serviceProvider.GetService(handlerType);
                
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for {request.GetType().Name}");
                }

                var handleMethod = handlerType.GetMethod("Handle");
                await (Task)handleMethod!.Invoke(handler, new object[] { request, cancellationToken })!;
            }

            public async Task Publish(object notification, CancellationToken cancellationToken = default)
            {
                await _messageBus.PublishAsync(notification);
            }

            public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            {
                await _messageBus.PublishAsync(notification);
            }
        }
    }
}