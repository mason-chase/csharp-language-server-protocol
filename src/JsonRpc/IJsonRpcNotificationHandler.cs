using OmniSharp.Extensions.JsonRpc.MediatR;

namespace OmniSharp.Extensions.JsonRpc
{
    public interface IJsonRpcNotificationHandler<in TNotification> : IRequestHandler<TNotification, Unit>, IJsonRpcHandler
        where TNotification : IRequest<Unit>
    {
    }
}
