namespace GraphQL.Projection.Tests;

public static class Helpers
{
    public static bool SequenceEntitiesEqual(EntityField[] Aarr, EntityField[] Barr)
    {
        if (Aarr.Length != Barr.Length)
        {
            return false;
        }

        for (int i = 0; i < Aarr.Length; i++)
        {
            if (!EntitiesEqual(Aarr[i], Barr[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool EntitiesEqual(EntityField Afield, EntityField BField)
    {
        if (Afield.Name != BField.Name)
        {
            return false;
        }

        var ASubFields = Afield.SubFields.ToArray();
        var BSubFields = BField.SubFields.ToArray();

        for (int i = 0; i < ASubFields.Length; i++)
        {
            var Aitem = ASubFields[i];
            var Bitem = BSubFields[i];

            if (!EntitiesEqual(Aitem, Bitem))
            {
                return false;
            }
        }

        return true;
    }
}
