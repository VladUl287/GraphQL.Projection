using System.Reflection;
using System.Linq.Expressions;
using GraphQL.Projection.Extensions;
using GraphQLParser.AST;

namespace GraphQL.Projection.Strategy.Helper;

public sealed class ExpressionHelper
{
    public Expression<Func<TEntity, TEntity>> GetLambdaExpression<TEntity>(GraphQLSelectionSet node)
    {
        var parameter = Expression.Parameter(typeof(TEntity));
        
        var initExpression = MemberInit(typeof(TEntity), parameter, [.. node.Selections]);

        return Expression.Lambda<Func<TEntity, TEntity>>(initExpression, parameter);
    }

    private MemberInitExpression MemberInit(Type type, Expression parameter, ASTNode[] selections)
    {
        var binds = new List<MemberAssignment>(selections.Length);

        foreach (var selection in selections)
        {
            if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
            {
                var fieldName = field.Name.StringValue;

                var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property is null)
                {
                    continue;
                }

                var propType = property.PropertyType;
                if (propType.IsPrimitive())
                {
                    var primitiveParameter = Expression.Property(parameter, property.Name);

                    var bind = Expression.Bind(property, primitiveParameter);

                    binds.Add(bind);

                    continue;
                }

                var parameterProperty = Expression.Property(parameter, property.Name);
                var memberInit = MemberInit(propType, parameterProperty, [.. field.SelectionSet?.Selections]);

                var memberBind = Expression.Bind(property, memberInit);

                binds.Add(memberBind);
            }
        }

        return Expression.MemberInit(Expression.New(type), binds);
    }
}
