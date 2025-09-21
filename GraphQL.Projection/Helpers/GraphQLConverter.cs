using GraphQLParser.AST;

namespace GraphQL.Projection.Helpers;

public static class GraphQLConverter
{
    public static GraphQLSelectionSet? FindQuery<TEntity>(this GraphQLDocument document, IReadOnlyList<string> path)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(path);

        foreach (var definition in document.Definitions)
        {
            if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
            {
                var selectionSet = ProcessOperation<TEntity>(operation, path);

                if (selectionSet is not null)
                {
                    return selectionSet;
                }
            }
        }

        return null;
    }

    private static GraphQLSelectionSet? ProcessOperation<TEntity>(GraphQLOperationDefinition operation, IReadOnlyList<string> path)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation.SelectionSet?.FindSelectionSet(new(path));
    }

    private static GraphQLSelectionSet? FindSelectionSet(this GraphQLSelectionSet selectionSet, Queue<string> path)
    {
        if (path is { Count: 0 })
        {
            return selectionSet;
        }

        if (path.TryDequeue(out var pathStep))
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
                {
                    var fieldName = field.Name.StringValue;

                    if (fieldName.Equals(pathStep, StringComparison.OrdinalIgnoreCase))
                    {
                        var result = field.SelectionSet?.FindSelectionSet(path);

                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }
            }
        }

        return null;
    }
}
