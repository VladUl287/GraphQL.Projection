using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy.Helper;

public sealed class ExpressionHelper
{
    private readonly IBindingContext bindingContext;

    public ExpressionHelper(IBindingContext bindingContext)
    {
        this.bindingContext = bindingContext;
    }

    public Expression<Func<TEntity, TEntity>> GetLambdaExpression<TEntity>(IEnumerable<TreeField> fields)
    {
        var parameter = Expression.Parameter(typeof(TEntity));

        var result = GetMemberInitExpression(typeof(TEntity), parameter, fields);

        return Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
    }

    private MemberInitExpression GetMemberInitExpression(Type type, Expression parameter, IEnumerable<TreeField> fields)
    {
        var bindings = new List<MemberBinding>();
        
        foreach (var field in fields)
        {
            var property = type.GetProperty(field.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property is not null)
            {
                var bind = bindingContext.Bind(property, parameter, field, GetMemberInitExpression);

                bindings.Add(bind);
            }
        }

        return Expression.MemberInit(Expression.New(type), bindings);
    }
}
