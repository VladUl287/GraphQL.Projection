using GraphQLParser.AST;
using System.Reflection;

namespace GraphQL.Projection.Helpers;

public static class GraphQLHelper
{
    public static IEnumerable<EntityField> GetFields<TEntity>(GraphQLDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Definitions is null or { Count: 0 })
        {
            return [];
        }

        var requestedFields = new List<EntityField>();

        foreach (var definition in document.Definitions)
        {
            if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
            {
                var fields = GetEntityFields(operation.SelectionSet, typeof(TEntity));

                requestedFields.AddRange(fields);
            }
        }

        return requestedFields;
    }

    private static IEnumerable<EntityField> GetEntityFields(GraphQLSelectionSet? selectionSet, Type entityType)
    {
        if (selectionSet?.Selections is null or [])
        {
            return Enumerable.Empty<EntityField>();
        }

        var result = new List<EntityField>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is { Kind: ASTNodeKind.Field } and GraphQLField QLField)
            {
                var fieldName = QLField.Name.StringValue;
                var property = TryGetProperty(entityType, fieldName);

                if (property is not null)
                {
                    var entityField = QLField.MapField(property.PropertyType);

                    result.Add(entityField);
                }
                else
                {
                    var fields = GetEntityFields(QLField.SelectionSet, entityType);

                    result.AddRange(fields);
                }
            }
        }

        return result;
    }

    private static PropertyInfo? TryGetProperty(Type entityType, string fieldName)
    {
        var property = entityType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        property = CheckIfNullAndGeneric(entityType, fieldName, property);

        return property;
    }

    private static EntityField MapField(this GraphQLField QLField, Type propertyType)
    {
        var name = QLField.Name.StringValue;
        var subFields = GetEntityFields(QLField.SelectionSet, propertyType);

        return new EntityField()
        {
            Name = name,
            SubFields = subFields
        };
    }

    private static PropertyInfo? CheckIfNullAndGeneric(Type entityType, string fieldName, PropertyInfo? property)
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
