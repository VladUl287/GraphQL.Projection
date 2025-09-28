using GraphQL.Projection.Extensions;
using GraphQL.Projection.Helpers;
using GraphQL.Projection.Models;
using GraphQL.Projection.Resolvers;
using GraphQL.Projection.Strategy.Binding;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        return (document, model) =>
        {
            var parameterResolver = new ParameterResolver();
            var bindingContext = new BindingContext([new EntityStrategy(new Visitor.ParameterReplacerFabric()), new EnumerableStrategy(new ParameterResolver())]);
            var typeResolver = new TypeResolver();
            var fieldBinder = new FieldBuilder(new Fabrics.TypeBuilderFactory(typeResolver, bindingContext), typeResolver, bindingContext);
            var typeBuilder = new TypeBuilder(fieldBinder);
            var builder = new ExpressionBuilder(typeBuilder, new ParameterResolver());

            GraphQLSelectionSet? qLSelectionSet = null;
            foreach (var definition in document.Definitions)
            {
                if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
                {
                    foreach (var selection in operation.SelectionSet.Selections)
                    {
                        if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
                        {
                            qLSelectionSet = field.SelectionSet;
                        }
                    }
                    break;
                }
            }

            ArgumentNullException.ThrowIfNull(qLSelectionSet);

            var parameter = Expression.Parameter(typeof(TEntity));

            var assignements = BuildAssignements<TEntity>(parameter, qLSelectionSet);

            var result = InitilizeMember<TEntity>(assignements);

            var expression = ExpressionBuilder.BuildExpression<TEntity>(qLSelectionSet, (node) => typeBuilder.BuildType(typeof(TEntity), node),
                (expression) => parameterResolver.GetParameterExpression(expression as MemberInitExpression));

            return model with
            {
                Select = Expression.Lambda<Func<TEntity, TEntity>>(result, parameter)
            };
        };
    }

    private static MemberInitExpression InitilizeMember<TEntity>(IEnumerable<MemberAssignment> assignments)
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
        var memberInit = InitilizeMember<TEntity>(assignements);
        return Expression.Bind(property, memberInit);
    }
}