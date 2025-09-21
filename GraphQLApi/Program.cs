using GraphQL;
using GraphQL.AspNet.Configuration;
using GraphQL.Projection.Extensions;
using GraphQL.Server.Ui.Playground;
using GraphQLApi.Database;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddDbContext<DBContext>(ServiceLifetime.Transient);

    builder.Services.AddGraphQL();
    builder.Services.AddGraphQlProjection();
}

var app = builder.Build();
{
    app.UseGraphQL();
    app.UseGraphQLPlayground("/",
        new PlaygroundOptions
        {
            GraphQLEndPoint = "/graphql",
            SubscriptionsEndPoint = "/graphql",
        });
}

app.Run();
