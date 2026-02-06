using GraphQLApi.Console;
using GraphQLApi.Console.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static GraphQLOp;

var userQuery =
    Operations.field("user", [], default, [], [
        Operations.field("id", [], default, [], []),
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

var flattenedNodes = Operations.flatten(typeof(DeletedExternalUser), TypeSystem.defaultInspector, userQuery);

var ast = Operations.interpret(flattenedNodes);
Console.WriteLine(ast);

return;

var selector = QueryBuilder.buildSelector<User>(ast);

Console.WriteLine(selector);

var user = new User
{
    Id = 1,
    Name = "test"
};

var com = selector.Compile();
var obj = com.Invoke(user);

Console.WriteLine(JsonSerializer.Serialize(user));
Console.WriteLine(JsonSerializer.Serialize(obj));

return;

var query = new AppDatabaseContext()
    .Users
    .Select(selector);

Console.WriteLine(query.ToQueryString());

await foreach (var item in query.AsAsyncEnumerable())
{
    Console.WriteLine(JsonSerializer.Serialize(item));
}