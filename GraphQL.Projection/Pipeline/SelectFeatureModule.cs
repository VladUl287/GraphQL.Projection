using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        return (selectionSet, model) =>
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var bindings = BuildAssignements<TEntity>(parameter, selectionSet);
            var result = MemberInit<TEntity>(bindings);
            var expression = Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
            return model with
            {
                Select = expression
            };
        };
    }

    private static MemberInitExpression MemberInit<TEntity>(IEnumerable<MemberAssignment> assignments)
    {
        return Expression.MemberInit(Expression.New(typeof(TEntity)), assignments);
    }

    private static List<MemberAssignment> BuildAssignements<TEntity>(ParameterExpression parameter, GraphQLSelectionSet set)
    {
        var bindings = new List<MemberAssignment>(set.Selections.Count);

        AssignMember<TEntity>(parameter, bindings, set, 0);

        return bindings;
    }

    private static void AssignMember<TEntity>(ParameterExpression parameter, List<MemberAssignment> result, GraphQLSelectionSet set, int index)
    {
        if (index >= set.Selections.Count)
            return;

        var member = set.Selections[index];
        if (member is GraphQLField field)
        {
            var assignement = Assign<TEntity>(parameter, field);
            result.Add(assignement);
            return;
        }

        AssignMember<TEntity>(parameter, result, set, index + 1);
    }

    private static MemberAssignment Assign<TEntity>(ParameterExpression parameter, GraphQLField field)
    {
        var fieldName = field.Name.StringValue;
        var property = typeof(TEntity).GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        ArgumentNullException.ThrowIfNull(property);

        var propType = property.PropertyType;

        if (propType.IsPrimitive())
        {
            var memberAccess = Expression.MakeMemberAccess(parameter, property);
            return Expression.Bind(property, memberAccess);
        }

        var assignements = BuildAssignements<TEntity>(parameter, field.SelectionSet);
        var memberInit = MemberInit<TEntity>(assignements);
        return Expression.Bind(property, memberInit);
    }
}