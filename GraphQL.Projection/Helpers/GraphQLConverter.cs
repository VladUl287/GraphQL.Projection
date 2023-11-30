using GraphQLParser.AST;
using System.Reflection;

namespace GraphQL.Projection.Helpers;

public static class GraphQLConverter
{
    public static IEnumerable<TreeField> ConvertToTree<TEntity>(GraphQLDocument document, IReadOnlyList<string> path)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(path);

        foreach (var definition in document.Definitions)
        {
            if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
            {
                var fields = ProcessOperation<TEntity>(operation, path);

                if (fields is { Length: > 0 })
                {
                    return fields;
                }
            }
        }

        return Enumerable.Empty<TreeField>();
    }

    private static TreeField[] ProcessOperation<TEntity>(GraphQLOperationDefinition operation, IReadOnlyList<string> path)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var selectionSet = operation.SelectionSet?.FindSelectionSet(new(path));

        if (selectionSet is null)
        {
            return [];
        }

        return CreateTree(selectionSet, typeof(TEntity))
            .ToArray();
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
                    if (field.Name.StringValue.Equals(pathStep, StringComparison.OrdinalIgnoreCase))
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

    private static IEnumerable<TreeField> CreateTree(GraphQLSelectionSet selectionSet, Type entityType)
    {
        if (selectionSet?.Selections is null or [])
        {
            return Enumerable.Empty<TreeField>();
        }

        var fields = new List<TreeField>();
        var hash = new HashSet<string>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
            {
                var fieldName = field.Name.StringValue;

                if (hash.Contains(fieldName))
                {
                    continue;
                }

                if (entityType.TryGetProperty(fieldName, out var property))
                {
                    var treeField = field.MapField(property!.PropertyType);

                    fields.Add(treeField);
                    hash.Add(fieldName);
                }
            }
        }

        return fields;
    }

    private static bool TryGetProperty(this Type entityType, string fieldName, out PropertyInfo? propertyInfo)
    {
        propertyInfo = entityType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo is null && entityType.IsGenericType)
        {
            var elementType = entityType
                .GetGenericArguments()
                .FirstOrDefault();

            if (elementType is not null)
            {
                propertyInfo = elementType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
        }

        return propertyInfo is not null;
    }

    private static TreeField MapField(this GraphQLField field, Type propertyType)
    {
        var name = field.Name.StringValue;
        var chilren = CreateTree(field.SelectionSet, propertyType);

        return new TreeField
        {
            Name = name,
            Children = chilren
        };
    }

    private static PropertyInfo? CheckIfNullOrGeneric(Type entityType, string fieldName, PropertyInfo? property)
    {
        if (property is null && entityType.IsGenericType)
        {
            var elementType = entityType
                .GetGenericArguments()
                .FirstOrDefault();

            if (elementType is not null)
            {
                property = elementType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
        }

        return property;
    }
}
