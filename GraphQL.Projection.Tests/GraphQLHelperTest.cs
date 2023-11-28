using GraphQL.Projection.Helpers;
using GraphQL.Projection.Strategy.Extensions;
using GraphQL.Projection.Tests.Types;
using GraphQLParser.Exceptions;
using System.Security.Cryptography;

namespace GraphQL.Projection.Tests;

[TestFixture]
public sealed class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Parse_Not_Correct_Query_Test()
    {
        var query = "not correct query";

        Assert.Catch(
            typeof(GraphQLSyntaxErrorException),
            new(() =>
            {
                query.ToFields<Order>();
            }));
    }

    [Test]
    public void Parse_Exact_Test()
    {
        var query = "query {" +
                "order(id: 1) {" +
                    "id," +
                    "address {" +
                        "name" +
                    "}" +
                    "user {" +
                        "id," +
                        "name," +
                    "}" +
                "}" +
            "}";

        var fields = query.ToFields<Order>();

        var expected = new EntityField[] {
            new() {
                Name = "id",
                SubFields = Enumerable.Empty<EntityField>()
            },
            new()
            {
                Name = "address",
                SubFields = [
                    new()
                    {
                        Name = "name",
                        SubFields = Enumerable.Empty<EntityField>()
                    }
                ]
            },
            new()
            {
                Name = "user",
                SubFields = [
                    new()
                    {
                        Name = "id",
                        SubFields = Enumerable.Empty<EntityField>()
                    },
                    new()
                    {
                        Name = "name",
                        SubFields = Enumerable.Empty<EntityField>()
                    }
                ]
            }
        };

        var equal = Helpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }

    [Test]
    public void Parse_Repeating_Fields_Test()
    {
        var query = "query {" +
                "order(id: 1) {" +
                    "id," +
                    "id," +
                    "address {" +
                        "name," +
                        "name," +
                    "}" +
                    "user {" +
                        "id," +
                        "name," +
                        "name," +
                    "}" +
                "}" +
            "}";

        var fields = query.ToFields<Order>();

        var expected = new EntityField[] {
            new() {
                Name = "id",
                SubFields = Enumerable.Empty<EntityField>()
            },
            new()
            {
                Name = "address",
                SubFields = [
                    new()
                    {
                        Name = "name",
                        SubFields = Enumerable.Empty<EntityField>()
                    }
                ]
            },
            new()
            {
                Name = "user",
                SubFields = [
                    new()
                    {
                        Name = "id",
                        SubFields = Enumerable.Empty<EntityField>()
                    },
                    new()
                    {
                        Name = "name",
                        SubFields = Enumerable.Empty<EntityField>()
                    }
                ]
            }
        };

        var equal = Helpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }
}