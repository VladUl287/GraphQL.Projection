using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using System.Collections;
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
            var bindings = BuildAssignements(typeof(TEntity), parameter, selectionSet);
            var result = MemberInit(typeof(TEntity), bindings);
            var expression = Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
            return model with
            {
                Select = expression
            };
        };
    }

    private static MemberInitExpression MemberInit(Type type, IEnumerable<MemberAssignment> assignments)
    {
        return Expression.MemberInit(Expression.New(type), assignments);
    }

    private static List<MemberAssignment> BuildAssignements(Type type, Expression parameter, GraphQLSelectionSet set)
    {
        var bindings = new List<MemberAssignment>(set.Selections.Count);

        AssignMember(type, parameter, bindings, set, 0);

        return bindings;
    }

    private static void AssignMember(Type type, Expression parameter, List<MemberAssignment> result, GraphQLSelectionSet set, int index)
    {
        if (index >= set.Selections.Count)
            return;

        var member = set.Selections[index];
        if (member is GraphQLField field)
        {
            var assignement = Assign(type, parameter, field);
            result.Add(assignement);
        }

        AssignMember(type, parameter, result, set, index + 1);
    }

    private static MemberAssignment Assign(Type type, Expression parameter, GraphQLField field)
    {
        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        ArgumentNullException.ThrowIfNull(property);

        var propType = property.PropertyType;

        if (propType.IsPrimitive())
        {
            var memberAccess = Expression.MakeMemberAccess(parameter, property);
            return Expression.Bind(property, memberAccess);
        }

        var subParameter = Expression.Property(parameter, property);

        if (propType.IsEnumerable())
        {
            var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
            var childParameter = Expression.Parameter(subEntityType, "email");
            var childAssignements = BuildAssignements(subEntityType, childParameter, field.SelectionSet);
            var childMemberInit = MemberInit(subEntityType, childAssignements);

            var selectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .MakeGenericMethod(subEntityType, childMemberInit.Type);

            var selectorLambda = Expression.Lambda(
                childMemberInit,
                childParameter
            );

            var selectCall = Expression.Call(
                selectMethod,
                Expression.PropertyOrField(parameter, property.Name), // source collection
                selectorLambda // selector
            );

            return Expression.Bind(property, selectCall);
        }

        var assignements = BuildAssignements(propType, subParameter, field.SelectionSet);
        var memberInit = MemberInit(propType, assignements);
        return Expression.Bind(property, memberInit);
    }
}