using GraphQL.Projection.Fabrics;
using GraphQL.Projection.Helpers;
using GraphQL.Projection.Resolvers;
using GraphQL.Projection.Resolvers.Contracts;
using GraphQL.Projection.Strategy.Binding;
using GraphQL.Projection.Strategy.Binding.Contracts;
using GraphQL.Projection.Visitor;
using GraphQL.Projection.Visitors;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Projection.Extensions;

public static class ServicesExtensions
{
    public static void AddGraphQlProjection(this IServiceCollection services)
    {
        services.AddSingleton<ParameterReplacerFabric>();

        services.AddSingleton<IBindingContext, BindingContext>();
        services.AddSingleton<IBindingStrategy, EntityStrategy>();
        services.AddSingleton<IBindingStrategy, EnumerableStrategy>();

        services.AddSingleton<IParameterResolver, ParameterResolver>();
        services.AddSingleton<ITypeResolver, TypeResolver>();

        services.AddSingleton<IFieldBinder, FieldBuilder>();
        services.AddSingleton<ITypeBuilder, TypeBuilder>();
        services.AddSingleton<TypeBuilderFactory>();
        services.AddSingleton<ExpressionBuilder>();
    }
}
