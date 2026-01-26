using GraphQL.Projection.Chains.TypeResolving.Contracts;
using System.Reflection;

namespace GraphQL.Projection.Chains.TypeResolving;

internal abstract class AbstractHandler : IHandler
{
    private IHandler? nextHandler;

    public IHandler AddHandler(IHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return nextHandler ??= handler;
    }

    public virtual Type? Handle(PropertyInfo request)
    {
        if (nextHandler is not null)
        {
            return nextHandler.Handle(request);
        }

        return default;
    }
}
