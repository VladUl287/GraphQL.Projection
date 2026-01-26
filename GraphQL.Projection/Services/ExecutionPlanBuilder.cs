using GraphQL.Projection.Functors;
using GraphQL.Projection.Nodes;
using GraphQLParser.AST;

namespace GraphQL.Projection.Services;

public static class ExecutionPlanBuilder
{
    public static GraphQLOp<ObjectNode> BuildExecutionPlan(this GraphQLDocument document)
    {
        var operation = document.Definitions
            .OfType<GraphQLOperationDefinition>()
            .FirstOrDefault();

        if (operation?.SelectionSet == null)
            throw new InvalidOperationException("No selection set found");

        return ConvertSelectionSet(operation.SelectionSet);
    }

    private static GraphQLOp<ObjectNode> ConvertSelectionSet(GraphQLSelectionSet selectionSet)
    {
        var selections = new List<GraphQLOp<FieldNode>>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field)
            {
                selections.Add(ConvertField(field));
            }
        }

        // Create Object operation
        return new GraphQLOp<ObjectNode>.Object(
            "root",
            [],
            //selections,
            objNode => objNode);
    }

    private static GraphQLOp<FieldNode> ConvertField(GraphQLField field)
    {
        return new GraphQLOp<FieldNode>.Field(
            field.Name.StringValue,
            fieldNode => fieldNode);
    }
}
