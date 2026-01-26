using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQL.Projection.Pipeline;
using GraphQL.Projection.Services;
using GraphQLApi.Console;
using GraphQLParser;
using GraphQLParser.AST;

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

ArgumentNullException.ThrowIfNull(qlField);
ArgumentNullException.ThrowIfNull(qLSelectionSet);

var queriable = document
    .BuildExecutionPlan()
    .ConvertToQueryable<UserExt>();

var pipeline = PipelineComposition.CreatePipeline(typeof(UserExt));
var queryModel = pipeline(qLSelectionSet, QueryModel.Empty);

Console.WriteLine(queryModel.Select.ToString());