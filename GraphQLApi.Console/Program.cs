using GraphQL.Projection;
using GraphQLApi.Console;
using GraphQLParser;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

var query = """
    query {
      search {
        id
      }
    }
    """;

var document = Parser.Parse(query);

GraphQLSelectionSet? qLSelectionSet = null;
GraphQLField? qlField = null;

foreach (var definition in document.Definitions)
{
    if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
    {
        foreach (var selection in operation.SelectionSet.Selections)
        {
            if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
            {
                qlField = field;
                qLSelectionSet = field.SelectionSet;
            }
        }
        break;
    }
}

var parameter = Expression.Parameter(typeof(UserExt));
var property = typeof(UserExt)
    .GetProperty("test", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
var expression = Expression.Property(parameter, property);

ArgumentNullException.ThrowIfNull(qlField);
ArgumentNullException.ThrowIfNull(qLSelectionSet);
