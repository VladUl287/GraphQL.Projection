using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection;

internal sealed class MemberInitInfo
{
    public MemberInitInfo(Type type, Expression parameter, TreeField[] fields, List<MemberBinding> binds, int index = default, PropertyInfo? property = null)
    {
        Type = type ?? throw new ArgumentNullException();
        Binds = binds ?? throw new ArgumentNullException();
        Fields = fields ?? throw new ArgumentNullException();
        Parameter = parameter ?? throw new ArgumentNullException();

        Property = property;

        SetIndex(index);
    }

    public Type Type { get; }

    public Expression Parameter { get; }

    public TreeField[] Fields { get; }

    public List<MemberBinding> Binds { get; }

    public PropertyInfo? Property { get; }

    public int Index { get; private set; }

    public void SetIndex(int index)
    {
        if (index < 0 || index > Fields?.Length)
        {
            throw new ArgumentException("Not correct index value");
        }

        Index = index;
    }
}
