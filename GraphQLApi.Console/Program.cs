using GraphQL.Projection.Models;
using GraphQL.Projection.Pipeline;
using GraphQLApi.Console;
using GraphQLParser;

var query = """
    query {
      search {
        id
      }
    }
    """;

var pipeline = PipelineComposition.CreatePipeline<UserExt>();

var document = Parser.Parse(query);

var queryModel = pipeline(document, QueryModel<UserExt>.Empty);

Console.WriteLine(queryModel.Select.ToString());