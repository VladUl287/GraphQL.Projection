using GraphQL.Projection.Extensions;
using GraphQL.Projection.Fabrics;
using GraphQL.Projection.Resolvers.Contracts;
using GraphQL.Projection.Strategy.Binding.Contracts;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Helpers;

public sealed class FieldBuilder : IFieldBinder
{
    private readonly TypeBuilderFactory typeBuilderFactory;
    private readonly ITypeResolver typeResolver;
    private readonly IBindingContext bindingContext;

    public FieldBuilder(TypeBuilderFactory typeBuilderFactory, ITypeResolver typeResolver, IBindingContext bindingContext)
    {
        this.typeBuilderFactory = typeBuilderFactory;
        this.typeResolver = typeResolver;
        this.bindingContext = bindingContext;
    }

    public MemberAssignment Assign(Expression parameter, Type type, GraphQLField field)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(field);

        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        var propType = typeResolver.GetPropertyType(property!.PropertyType) 
            ?? throw new Exception("Not supported type");

        if (propType.IsPrimitive())
        {
            return BindPrimitiveType(parameter, property);
        }

        var memberBind = BindComplexType(parameter, field, property, propType);

        return memberBind;
    }

    private static MemberAssignment BindPrimitiveType(Expression parameter, PropertyInfo property)
    {
        var memberAccess = Expression.MakeMemberAccess(parameter, property);
        return Expression.Bind(property, memberAccess);
    }

    private MemberAssignment BindComplexType(Expression parameter, GraphQLField field, PropertyInfo property, Type propType)
    {
        var typeBuilder = typeBuilderFactory.CreateTypeBuilder();
        var memberInit = typeBuilder.BuildType(propType, field.SelectionSet);

        var subParameter = Expression.MakeMemberAccess(parameter, property);
        var memberBind = bindingContext.Bind(subParameter, property, memberInit);
        return memberBind;
    }
}
