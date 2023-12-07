namespace GraphQL.Projection.Tests.Helper;

public static class EntityHelpers
{
    public static bool SequenceEntitiesEqual(TreeField[] first, TreeField[] second)
    {
        if (Equals(first, second))
        {
            return true;
        }

        if (first.Length != second.Length)
        {
            return false;
        }

        for (int i = 0; i < first.Length; i++)
        {
            if (!first[i].EntityEqual(second[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool EntityEqual(this TreeField field, TreeField secondField)
    {
        if (field.Name != secondField.Name)
        {
            return false;
        }

        if (field.Children is null && secondField.Children is null)
        {
            return true;
        }

        if (field.Children == Enumerable.Empty<TreeField>() && secondField.Children == Enumerable.Empty<TreeField>())
        {
            return true;
        }

        var firstSubFields = field.Children?.ToArray();
        var secondSubFields = secondField.Children?.ToArray();

        if (firstSubFields is null && secondSubFields is not null ||
            secondSubFields is null && firstSubFields is not null)
        {
            return false;
        }

        return SequenceEntitiesEqual(firstSubFields, secondSubFields);
    }
}
