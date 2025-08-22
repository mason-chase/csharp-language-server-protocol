namespace OmniSharp.Extensions.JsonRpc
{
    public interface IJsonRpcMessage
    {
    }

    public interface IJsonRpcRequest<TResponse> : IJsonRpcMessage
    {
    }

    public interface IJsonRpcRequest : IJsonRpcMessage
    {
    }

    public interface IJsonRpcNotification : IJsonRpcMessage
    {
    }
}