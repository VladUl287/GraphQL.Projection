using System.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy.Helper;

public sealed class ExpressionHelper
{
    private readonly IBindingContext bindingContext;

    public ExpressionHelper(IBindingContext bindingContext)
    {
        this.bindingContext = bindingContext;
    }

    public Expression<Func<TEntity, TEntity>> GetLambdaExpression<TEntity>(TreeField[] fields)
    {
        var parameter = Expression.Parameter(typeof(TEntity));

        var result = GetMemberInitExpression(typeof(TEntity), parameter, fields);

        return Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
    }

    private MemberInitExpression GetMemberInitExpression(Type type, Expression parameter, TreeField[] fields)
    {
        var stack = new Stack<MemberInitInfo>();
        
        stack.Push(new(type, parameter, fields, [], 0));

        while (stack.Count > 0)
        {
            var skipInitialization = false;
            var info = stack.Peek();

            for (var i = info.Index; i < info.Fields.Length; i++)
            {
                var field = info.Fields[i];

                var property = info.Type.GetProperty(field.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property is null)
                {
                    continue;
                }

                var propertyType = property.PropertyType;

                if (propertyType.IsPrimitive || propertyType.IsEnum || propertyType == typeof(string) || propertyType == typeof(Guid))
                {
                    var propertyParameter = Expression.Property(info.Parameter, property.Name);

                    var bind = Expression.Bind(property, propertyParameter);

                    info.Binds.Add(bind);
                }
                else if (propertyType.IsClass && propertyType.GetInterface(nameof(IEnumerable)) is null)
                {
                    var propertyParameter = Expression.Property(info.Parameter, property.Name);

                    stack.Push(new(propertyType, propertyParameter, field.Children, [], 0, property));

                    info.SetIndex(i + 1);

                    skipInitialization = true;

                    break;
                }
                else if (propertyType.IsGenericType)
                {
                    var elementType = propertyType.GenericTypeArguments[0];

                    var propertyParameter = Expression.Parameter(elementType);

                    stack.Push(new(elementType, propertyParameter, field.Children, [], 0, property));

                    info.SetIndex(i + 1);

                    skipInitialization = true;

                    break;
                }
            }

            if (skipInitialization)
            {
                continue;
            }

            if (stack.Count <= 1)
            {
                break;
            }

            var current = stack.Pop();
            var previous = stack.Peek();

            if (current.Property is not null)
            {
                var bind = bindingContext.Bind(current.Property, previous.Parameter, current.Parameter, current.Type, current.Binds);

                previous.Binds.Add(bind);
            }
        }

        var result = stack.Pop();

        return Expression.MemberInit(Expression.New(result.Type), result.Binds);
    }

    private sealed class MemberInitInfo
    {
        public MemberInitInfo(Type type, Expression parameter, TreeField[] fields, List<MemberBinding> binds, int index, PropertyInfo? property = null)
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
}
