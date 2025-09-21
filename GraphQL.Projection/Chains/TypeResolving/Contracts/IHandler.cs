using System.Reflection;

namespace GraphQL.Projection.Chains.TypeResolving.Contracts;

internal interface IHandler
{
    IHandler AddHandler(IHandler handler);

    Type? Handle(PropertyInfo property);
}
