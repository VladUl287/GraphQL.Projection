using GraphQLParser.AST;

namespace GraphQL.Projection.Models;

public delegate QueryModel<TEntity> GraphQLFeatureModule<TEntity>(
    GraphQLSelectionSet doc, 
    QueryModel<TEntity> model);
