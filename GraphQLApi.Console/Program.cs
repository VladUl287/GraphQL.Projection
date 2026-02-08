using static GraphQLOp;
using GraphQLApi.Console;
using System.Text.Json;
using Microsoft.FSharp.Core;

var userQuery =
    Operations.field("user", [], default, [], [
        Operations.field("id", [], default, [], []),
        Operations.field("role", [], default, [], [
            Operations.field("id", [], default, [], []),
            Operations.inlineFragment("ExternalRole", [], [
                Operations.field("source", [], default, [], [])
            ])
        ]),
        Operations.inlineFragment("ExternalUser", [], [
            Operations.field("metadata", [], default, [], []),
                Operations.inlineFragment("DeletedExternalUser", [], [
                    Operations.field("deletedAt", [], default, [], []),
                ]),
        ]),
        Operations.inlineFragment("TemporaryUser", [], [
            Operations.field("lifeTime", [], default, [], []),
        ])
    ]);

var prunedDirctivesQuery = Operations.map(
    (FSharpFunc<GraphQLProcessing.GraphQLNode, GraphQLProcessing.GraphQLNode>)Operations.pruneConditionalNodes, userQuery);

Func<GraphQLProcessing.GraphQLNode, GraphQLProcessing.GraphQLNode> flattenCarrier = (node) => 
    Operations.flattenMap(typeof(DeletedExternalUser), TypeSystem.defaultInspector, node);

var flattenedDirectivesQuery = Operations.map(FuncConvert.FromFunc(flattenCarrier), prunedDirctivesQuery);

var ast = Operations.interpret(flattenedDirectivesQuery);
Console.WriteLine(ast);

var selector = QueryBuilder.buildSelector<DeletedExternalUser>(ast);
Console.WriteLine(string.Empty);
Console.WriteLine(selector);

var user = new DeletedExternalUser
{
    Id = 1,
    Name = "test",
    Role = new()
    {
        Id = 1,
        Name = "test",
        Source = "source"
    },
    Metadata = "external metadata",
    DeletedAt = DateTime.UtcNow
};

var com = selector.Compile();
var obj = com.Invoke(user);

Console.WriteLine(string.Empty);
Console.WriteLine(JsonSerializer.Serialize(user));
Console.WriteLine(string.Empty);
Console.WriteLine(JsonSerializer.Serialize(obj));

return;

//var query = new AppDatabaseContext()
//    .Users
//    .Select(selector);

//Console.WriteLine(query.ToQueryString());

//await foreach (var item in query.AsAsyncEnumerable())
//{
//    Console.WriteLine(JsonSerializer.Serialize(item));
//}