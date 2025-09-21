using GraphQLParser.AST;
using System.Linq.Expressions;

namespace GraphQL.Projection.Helpers;

public sealed class TypeBuilder : ITypeBuilder
{
    private readonly IFieldBinder fieldBinder;

    public TypeBuilder(IFieldBinder fieldBinder)
    {
        this.fieldBinder = fieldBinder;
    }

    public MemberInitExpression BuildType(Type type, GraphQLSelectionSet selectionSet)
    {
        var binds = new List<MemberAssignment>(selectionSet.Selections.Count);
        var parameter = Expression.Parameter(type);

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field)
            {
                var memberBind = fieldBinder.Assign(parameter, type, field);
                binds.Add(memberBind);
            }
        }

        return Expression.MemberInit(Expression.New(type), binds);
    }
}
