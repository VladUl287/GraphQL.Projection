using GraphQL.Projection.Strategy.Extensions;
using GraphQL.Projection.Tests.Types;
using GraphQLParser.Exceptions;

namespace GraphQL.Projection.Tests;

[TestFixture]
internal sealed class GraphQLExtensionsTests
{
    [Test]
    public void Not_Correct_Query_Test()
    {
        var query = "not correct query";

        var path = Array.Empty<string>();

        Assert.Catch(
            typeof(GraphQLSyntaxErrorException),
            new(() => query.ToTree<Order>(path)));
    }
}
