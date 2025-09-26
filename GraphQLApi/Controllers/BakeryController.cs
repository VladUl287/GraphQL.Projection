using GraphQL.AspNet.Attributes;
using GraphQL.AspNet.Controllers;
using GraphQL.Projection;
using GraphQL.Projection.Helpers;
using GraphQL.Projection.Pipeline;
using GraphQLApi.Database;
using GraphQLApi.Models;
using GraphQLParser;
using Microsoft.EntityFrameworkCore;

namespace GraphQLApi.Controllers;

public class BakeryController : GraphController
{
    private readonly DBContext dbContext;
    private readonly ExpressionBuilder expressionBuilder;

    public BakeryController(DBContext dbContext, ExpressionBuilder expressionBuilder)
    {
        this.dbContext = dbContext;
        this.expressionBuilder = expressionBuilder;
    }

    [QueryRoot("search")]
    public IEnumerable<User> Search(string text)
    {
        var query = dbContext.Users.AsQueryable();

        var pipeline = PipelineComposition.CreatePipeline<User>();

        var document = Parser.Parse(Context.QueryRequest.QueryText);

        query = query.Translate(document, pipeline);

        return query.ToArray();
    }

    [QueryRoot("searchPastries")]
    public async Task<IEnumerable<User>> SearchPastries(string text)
    {
        var query = Context.QueryRequest.QueryText;

        var document = Parser.Parse(query);
        var selectionSet = document.FindQuery<User>(["searchPastries"]);

        //var test = new ExpressionHelper().GetLambdaExpression<User>(queryString);

        var test = expressionBuilder.BuildExpression<User>(selectionSet);

        try
        {
            var result = await dbContext.Users
                .Select(test)
                .ToArrayAsync();

            var sql = dbContext.Users
                .Select(test)
                .ToQueryString();

            return result;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    [QueryRoot]
    public IEnumerable<User> Test()
    {
        var query = Context.QueryRequest.QueryText;

        return Enumerable.Empty<User>();
    }
}
