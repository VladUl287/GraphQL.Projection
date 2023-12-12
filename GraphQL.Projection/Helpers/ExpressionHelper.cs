using System.Reflection;
using System.Linq.Expressions;
using GraphQL.Projection.Extensions;

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

        var initExpression = GetMemberInitExpression(typeof(TEntity), parameter, fields);

        return Expression.Lambda<Func<TEntity, TEntity>>(initExpression, parameter);
    }

    private MemberInitExpression GetMemberInitExpression(Type type, Expression parameter, TreeField[] fields)
    {
        var stack = new Stack<MemberInitInfo>();
        
        stack.Push(new(type, parameter, fields, []));

        while (stack.Count > 0)
        {
            var info = stack.Peek();

            var skipInitialization = AddBinds(stack, info, false);

            if (skipInitialization)
            {
                continue;
            }

            if (stack.Count <= 1)
            {
                break;
            }

            AddBubbleBind(stack);
        }

        var result = stack.Pop();

        return Expression.MemberInit(Expression.New(result.Type), result.Binds);
    }

    private static bool AddBinds(Stack<MemberInitInfo> stack, MemberInitInfo info, bool skipInitialization)
    {
        for (var i = info.Index; i < info.Fields.Length; i++)
        {
            var field = info.Fields[i];

            var property = info.Type.GetProperty(field.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property is null)
            {
                continue;
            }

            var propType = property.PropertyType;

            if (propType.IsPrimitive())
            {
                var bind = GetPrimitiveBind(info, property);

                info.Binds.Add(bind);

                continue;
            }

            var typeParameter = GetTypeParameter(info, property, propType);

            if (typeParameter is not null)
            {
                stack.Push(new(typeParameter.Value.Type, typeParameter.Value.Parameter, field.Children, [], 0, property));

                info.SetIndex(i + 1);

                skipInitialization = true;

                break;
            }
        }

        return skipInitialization;
    }

    private static MemberAssignment GetPrimitiveBind(MemberInitInfo initInfo, PropertyInfo property)
    {
        var primitiveParameter = Expression.Property(initInfo.Parameter, property.Name);

        return Expression.Bind(property, primitiveParameter);
    }

    private static (Type Type, Expression Parameter)? GetTypeParameter(MemberInitInfo info, PropertyInfo property, Type propType)
    {
        return propType switch
        {
            { IsGenericType: true } => GetGenericParameter(propType),
            { IsClass: true } => GetClassParameter(info, property, propType),
            _ => null
        };
    }

    private static (Type propType, MemberExpression) GetClassParameter(MemberInitInfo info, PropertyInfo property, Type propType)
    {
        return (propType, Expression.Property(info.Parameter, property.Name));
    }

    private static (Type, ParameterExpression) GetGenericParameter(Type propType)
    {
        var type = propType.GenericTypeArguments[0];

        return (type, Expression.Parameter(type));
    }

    private void AddBubbleBind(Stack<MemberInitInfo> stack)
    {
        if (stack.Count <= 1)
        {
            return;
        }

        var current = stack.Pop();
        var previous = stack.Peek();

        if (current.Property is not null)
        {
            var bind = bindingContext.Bind(current.Property, previous.Parameter, current.Parameter, current.Type, current.Binds);

            previous.Binds.Add(bind);
        }
    }
}
