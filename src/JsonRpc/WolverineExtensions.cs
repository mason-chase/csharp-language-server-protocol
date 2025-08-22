using System.IO.Pipelines;
using System.Reactive.Concurrency;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.JsonRpc.DryIoc;
using Wolverine;
using OmniSharp.Extensions.JsonRpc.MediatR;

namespace OmniSharp.Extensions.JsonRpc
{
    internal static class WolverineExtensions
    {
        internal static IContainer AddJsonRpcWolverine(this IContainer container)
        {
            // Register MediatR compatibility
            container.RegisterMany<RequestContext>(Reuse.Scoped);
            container.Register<IMediator, Mediator>(reuse: Reuse.Singleton);
            container.Register(typeof(IRequestHandler<,>), typeof(RequestHandler<,>));
            container.Register(typeof(IRequestHandler<,>), typeof(RequestHandlerDecorator<,>), setup: Setup.Decorator);

            // Register Wolverine (optional for future use)
            var services = new ServiceCollection();
            services.AddSingleton<IServiceProvider>(container);
            
            // For now, we'll use a stub message bus since we're maintaining MediatR compatibility
            container.RegisterInstance<IMessageBus>(new StubMessageBus());

            return container;
        }

        class RequestHandler<T, TR> : IRequestHandler<T, TR> where T : IRequest<TR>
        {
            private readonly IRequestContext _requestContext;

            public RequestHandler(IRequestContext requestContext)
            {
                _requestContext = requestContext;
            }
            public Task<TR> Handle(T request, CancellationToken cancellationToken)
            {
                return ((IRequestHandler<T, TR>) _requestContext.Descriptor.Handler).Handle(request, cancellationToken);
            }
        }

        class RequestHandlerDecorator<T, TR> : IRequestHandler<T, TR> where T : IRequest<TR>
        {
            private readonly IRequestHandler<T, TR>? _handler;
            private readonly IRequestContext? _requestContext;

            public RequestHandlerDecorator(IRequestHandler<T, TR>? handler = null, IRequestContext? requestContext = null)
            {
                _handler = handler;
                _requestContext = requestContext;
            }
            public Task<TR> Handle(T request, CancellationToken cancellationToken)
            {
                if (_requestContext == null)
                {
                    if (_handler == null)
                    {
                        throw new NotImplementedException($"No request handler was registered for type {typeof(IRequestHandler<T, TR>).FullName}");

                    }

                    return _handler.Handle(request, cancellationToken);
                }

                return ((IRequestHandler<T, TR>) _requestContext.Descriptor.Handler).Handle(request, cancellationToken);
            }
        }

        // Stub message bus for compatibility
        class StubMessageBus : IMessageBus
        {
            public Task<T> InvokeAsync<T>(object message, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
            {
                throw new NotImplementedException();
            }

            public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
            {
                throw new NotImplementedException();
            }

            public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
            {
                throw new NotImplementedException();
            }

            public ValueTask ScheduleAsync<T>(T message, DateTimeOffset executionTime, DeliveryOptions? options = null)
            {
                throw new NotImplementedException();
            }

            public ValueTask ScheduleAsync<T>(T message, TimeSpan delay, DeliveryOptions? options = null)
            {
                throw new NotImplementedException();
            }

            public Task InvokeAsync(object message, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
            {
                throw new NotImplementedException();
            }

            public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
            {
                throw new NotImplementedException();
            }

            public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
            {
                throw new NotImplementedException();
            }

            public IDestinationEndpoint EndpointFor(string endpointName)
            {
                throw new NotImplementedException();
            }

            public IDestinationEndpoint EndpointFor(Uri uri)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
            {
                throw new NotImplementedException();
            }

            public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
            {
                throw new NotImplementedException();
            }

            public string? TenantId { get; set; }
            public Guid CorrelationId { get; set; }
            public string? TimeZoneId { get; set; }
        }
    }
}