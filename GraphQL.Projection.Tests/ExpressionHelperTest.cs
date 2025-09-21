using GraphQL.Projection.Strategy.Binding.Contracts;
using GraphQL.Projection.Strategy.Helper;
using GraphQL.Projection.Tests.Types;

namespace GraphQL.Projection.Tests;

[TestFixture]
internal sealed class ExpressionHelperTest
{
    private ExpressionHelper expressionHelper;

    [SetUp]
    public void SetUp()
    {
        var strategies = new IBindingStrategy[]
        {
            new CollectionStrategy(),
            new EntityStrategy(),
            new EnumerableStrategy()
        };

        var bindingContext = new BindingContext(strategies);

        expressionHelper = new ExpressionHelper(bindingContext);
    }

    [Test]
    public void Get_Expression_Primitives_Test()
    {
        var fields = new TreeField[] {
            new() {
                Name = "id",
                Children = Array.Empty<TreeField>()
            },
            new()
            {
                Name = "name",
                Children = Array.Empty<TreeField>()
            }
        };

        var expression = expressionHelper.GetLambdaExpression<Address>(fields)
            .ToString();

        var expected = "Param_0 => new Address() {Id = Param_0.Id, Name = Param_0.Name}";

        Assert.That(expected, Is.EqualTo(expression));
    }

    [Test]
    public void Get_Expression_Entities_Test()
    {
        var fields = new TreeField[] {
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
                        Name = "id",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var expression = expressionHelper.GetLambdaExpression<Order>(fields)
            .ToString();

        var expected = "Param_0 => new Order() {Id = Param_0.Id, Address = new Address() {Id = Param_0.Address.Id}}";

        Assert.That(expected, Is.EqualTo(expression));
    }

    [Test]
    public void Get_Expression_Enumerables_Test()
    {
        var fields = new TreeField[] {
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
                        Name = "id",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            },
            new()
            {
                Name = "buildings",
                Children = [
                    new()
                    {
                        Name = "index",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var expression = expressionHelper.GetLambdaExpression<Order>(fields)
            .ToString();

        var expected = 
            "Param_0 => new Order() {" +
                "Id = Param_0.Id, " +
                "Address = new Address() {" +
                    "Id = Param_0.Address.Id" +
                "}, " +
                "Buildings = Param_0.Buildings.Select(Param_1 => new Building() {Index = Param_1.Index})" +
            "}";

        Assert.That(expected, Is.EqualTo(expression));
    }

    [Test]
    public void Get_Expression_Collections_Test()
    {
        var fields = new TreeField[] {
            new()
            {
                Name = "title",
                Children = Array.Empty<TreeField>()
            },
            new()
            {
                Name = "genres",
                Children = [
                    new()
                    {
                        Name = "name",
                        Children = Array.Empty<TreeField>()
                    }
                ]
            }
        };

        var expression = expressionHelper.GetLambdaExpression<Book>(fields)
            .ToString();

        var expected =
            "Param_0 => new Book() {" +
                "Title = Param_0.Title, " +
                "Genres = Param_0.Genres.Select(Param_1 => new Genre() {Name = Param_1.Name}).ToArray()" +
            "}";

        Assert.That(expected, Is.EqualTo(expression));
    }
}