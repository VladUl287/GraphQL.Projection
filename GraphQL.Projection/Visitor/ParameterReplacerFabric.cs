using GraphQL.Projection.Visitors;
using System.Linq.Expressions;

namespace GraphQL.Projection.Visitor;

public sealed class ParameterReplacerFabric
{
    public ParameterReplacer CreateParameterReplacer(Expression oldParameter, Expression newParameter) 
        => new ParameterReplacer(oldParameter, newParameter);
}
