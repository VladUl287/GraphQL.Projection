using GraphQLApi.Console;
using GraphQLApi.Console.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static GraphQLOp;

var userQuery =
    Operations.field("user", [], default, [], [
        Operations.field("id", [], default, [], [])
    ]);

var ast = Operations.interpret(userQuery);

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

var query = new AppDatabaseContext()
    .Users
    .Select(selector);

Console.WriteLine(query.ToQueryString());

await foreach (var item in query.AsAsyncEnumerable())
{
    Console.WriteLine(JsonSerializer.Serialize(item));
}