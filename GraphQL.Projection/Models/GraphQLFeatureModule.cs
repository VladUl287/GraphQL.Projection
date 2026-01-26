using GraphQLParser.AST;

namespace GraphQL.Projection.Models;

public delegate QueryModel GraphQLFeatureModule(
    GraphQLSelectionSet doc,
    QueryModel model);
