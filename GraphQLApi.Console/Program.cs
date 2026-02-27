using GraphQLApi.Console.Data;
using Microsoft.EntityFrameworkCore;
using static GraphQLSystem;
using static GraphQLOp;
using GraphQL.Projection;

var userQuery =
    Operations.field("user", [
            new ArgumentNode("filter", ValueNode.NewObjectValue([
                //new Tuple<string, ValueNode>("products", ValueNode.NewObjectValue([
                //    new Tuple<string, ValueNode>("none", ValueNode.NewObjectValue([
                //        new Tuple<string, ValueNode>("number", ValueNode.NewIntValue(12))
                //    ])),
                //])),
                //new Tuple<string, ValueNode>("products", ValueNode.NewObjectValue([
                //    new Tuple<string, ValueNode>("number", ValueNode.NewIntValue(12))
                //])),
                //new Tuple<string, ValueNode>("active", ValueNode.NewBooleanValue(true)),
                //new Tuple<string, ValueNode>("name", ValueNode.NewStringValue("test"))

                new Tuple<string, ValueNode>("name", ValueNode.NewObjectValue([
                    new Tuple<string, ValueNode>("endsWith",  ValueNode.NewStringValue("test"))
                ]))
                
                //new Tuple<string, ValueNode>("OR", ValueNode.NewListValue([
                //    ValueNode.NewObjectValue([
                //        //new Tuple<string, ValueNode>("id", ValueNode.NewIntValue(1)),
                //        new Tuple<string, ValueNode>("id",  ValueNode.NewObjectValue([
                //            new Tuple<string, ValueNode>("gte", ValueNode.NewIntValue(5)),
                //            //new Tuple<string, ValueNode>("lte", ValueNode.NewIntValue(10)),
                //        ])),
                //    ]),
                //    //ValueNode.NewObjectValue([
                //    //    new Tuple<string, ValueNode>("name", ValueNode.NewStringValue("test")),
                //    //]),
                //    ValueNode.NewObjectValue([
                //        new Tuple<string, ValueNode>("products", ValueNode.NewObjectValue([
                //             new Tuple<string, ValueNode>("number", ValueNode.NewIntValue(12))
                //        ])),
                //    ]),
                //]))
            ]))
        ], default, [], [
        Operations.field("id", [], default,
        //[
        //    new DirectiveNode("@skip",
        //    [
        //        new ArgumentNode("if", ValueNode.BooleanValue.NewBooleanValue(true))
        //    ])
        //]
        []
        ,
        []),
        Operations.field("createdAt", [], default, [], []),
        //Operations.field("role", [], default, [], [
        //    Operations.field("name", [], default, [], []),
        //    //Operations.inlineFragment("ExternalRole", [], [
        //    //    Operations.field("source", [], default, [], [])
        //    //])
        //]),
        //Operations.field("products", [], default, [], [
        //    Operations.field("number", [], default, [], []),
        //    Operations.field("createdAt", [], default, [], []),
        //    Operations.field("variants", [], default, [], [
        //        Operations.field("type", [], default, [], []),
        //        Operations.field("size", [], default, [], []),
        //    ]),
        //]),
        //Operations.inlineFragment("ExternalUser", [], [
        //    Operations.field("metadata", [], default, [], []),
        //        Operations.inlineFragment("DeletedExternalUser", [], [
        //            Operations.field("deletedAt", [], default, [], []),
        //        ]),
        //]),
        //Operations.inlineFragment("TemporaryUser", [], [
        //    Operations.field("lifeTime", [], default, [], []),
        //])
    ]);

//var prunedDirctivesQuery = Operations.map(
//    (FSharpFunc<GraphQLProcessing.GraphQLNode, GraphQLProcessing.GraphQLNode>)Operations.pruneConditionalNodes, userQuery);

//Func<GraphQLProcessing.GraphQLNode, GraphQLProcessing.GraphQLNode> flattenCarrier = (node) => 
//    Operations.flattenMap(typeof(DeletedExternalUser), TypeSystem.defaultInspector, node);

//var flattenedDirectivesQuery = Operations.map(FuncConvert.FromFunc(flattenCarrier), prunedDirctivesQuery);

//var ast = Operations.interpret(userQuery);
//Console.WriteLine(ast);

//var selector = SelectorBuilder.buildSelector<User>(ast);
//Console.WriteLine(string.Empty);
//Console.WriteLine(selector);

//var user = new DeletedExternalUser
//{
//    Id = 1,
//    Name = "test",
//    Role = new()
//    {
//        Id = 1,
//        Name = "test",
//        Source = "source"
//    },
//    Metadata = "external metadata",
//    DeletedAt = DateTime.UtcNow
//};

//var com = selector.Compile();
////var obj = com.Invoke(user);

//Console.WriteLine(string.Empty);
//Console.WriteLine(JsonSerializer.Serialize(user));
//Console.WriteLine(string.Empty);
//Console.WriteLine(JsonSerializer.Serialize(obj));

var query = new AppDatabaseContext()
    .Users
    .ProjectTo(userQuery)
    ;

Console.WriteLine(query.ToQueryString());

//await foreach (var item in query.AsAsyncEnumerable())
//{
//    Console.WriteLine(JsonSerializer.Serialize(item));
//}