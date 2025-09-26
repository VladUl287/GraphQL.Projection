using GraphQL.Projection.Helpers;
using GraphQL.Projection.Models;
using GraphQL.Projection.Resolvers;
using GraphQL.Projection.Strategy.Binding;
using GraphQLParser.AST;
using System.IO;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        return (document, model) =>
        {
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

            var expression = builder.BuildExpression<TEntity>(qLSelectionSet);

            return model with
            {
                Select = expression
            };
        };
    }
}