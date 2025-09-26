using GraphQLParser.AST;

namespace GraphQL.Projection.Models;

public delegate QueryModel<TEntity> GraphQLFeatureModule<TEntity>(
    GraphQLDocument doc, 
    QueryModel<TEntity> model);
