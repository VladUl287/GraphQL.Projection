using GraphQL.Projection.Helpers;
using GraphQL.Projection.Tests.Helper;
using GraphQL.Projection.Tests.Types;

namespace GraphQL.Projection.Tests;

[TestFixture]
internal sealed class GraphQLConverterTests
{
    [Test]
    public void Null_Document_Test()
    {
        Assert.Catch(
            typeof(ArgumentNullException),
            new(() => GraphQLConverter.FindQuery<Order>(null, [])));
    }

    [Test]
    public void Null_Path_Test()
    {
        var document = GraphQLParser.Parser.Parse(string.Empty);

        Assert.Catch(
            typeof(ArgumentNullException),
            new(() => GraphQLConverter.FindQuery<Order>(document, null)));
    }

    [Test]
    public void Correct_Query_Test()
    {
        var query = 
            "query {" +
                "order(id: 1) {" +
                    "id," +
                    "address {" +
                        "name" +
                    "}" +
                "}" +
            "}";

        var expected = new TreeField[] {
            new() {
                Name = "id",
                Children = Array.Empty<TreeField>()
            },
            new()
            {
                Name = "address",
                Children = [
                    new()
                    {
                        Name = "name",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var document = GraphQLParser.Parser.Parse(query);

        var fields = GraphQLConverter.FindQuery<Order>(document, ["order"]);

        var equal = EntityHelpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }
    
    [Test]
    public void Ignore_Path_Case_Query_Test()
    {
        var query = 
            "query {" +
                "order(id: 1) {" +
                    "id," +
                    "address {" +
                        "name" +
                    "}" +
                "}" +
            "}";

        var expected = new TreeField[] {
            new() {
                Name = "id",
                Children = Array.Empty<TreeField>()
            },
            new()
            {
                Name = "address",
                Children = [
                    new()
                    {
                        Name = "name",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var document = GraphQLParser.Parser.Parse(query);

        var fields = GraphQLConverter.FindQuery<Order>(document, ["OrDEr"]);

        var equal = EntityHelpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }

    [Test]
    public void Repeating_Fields_Test()
    {
        var query = 
            "query {" +
                "order(id: 1) {" +
                    "id," +
                    "id" +
                "}" +
                "order(id: 1) {" +
                    "id," +
                    "id," +
                    "name" +
                "}" +
            "}";

        var expected = new TreeField[] {
            new() {
                Name = "id",
                Children = Array.Empty<TreeField>()
            }
        };

        var document = GraphQLParser.Parser.Parse(query);

        var fields = GraphQLConverter.FindQuery<Order>(document, ["order"]);

        var equal = EntityHelpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }
    
    [Test]
    public void Not_Correct_Path_Test()
    {
        var query = 
            "query {" +
                "order(id: 1) {" +
                    "id," +
                "}" +
            "}";

        var expected = Array.Empty<TreeField>();

        var document = GraphQLParser.Parser.Parse(query);

        var fields = GraphQLConverter.FindQuery<Order>(document, ["book"]);

        var equal = EntityHelpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }

    [Test]
    public void Root_Field_Equal_Model_Field_Test()
    {
        var query =
            "query {" +
                "id(name: test) {" +
                    "address {" +
                        "name" +
                    "}" +
                "}" +
            "}";

        var expected = new TreeField[] {
            new() {
                Name = "address",
                Children = [
                    new TreeField
                    {
                        Name = "name",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var document = GraphQLParser.Parser.Parse(query);

        var fields = GraphQLConverter.FindQuery<Order>(document, ["id"]);

        var equal = EntityHelpers.SequenceEntitiesEqual(fields.ToArray(), expected);

        Assert.That(equal, Is.True);
    }
}