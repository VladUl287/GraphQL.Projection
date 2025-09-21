//using System.Reflection;
//using System.Linq.Expressions;

//namespace GraphQL.Projection.Strategy;

//public sealed class CollectionStrategy : IBindingStrategy
//{
//    public bool AppliesTo(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);

//    public MemberBinding Bind(PropertyInfo property, Expression accessParameter, Expression bindParameter, Type type, IEnumerable<MemberBinding> bindings)
//    {
//        var elementType = property.PropertyType
//            .GetGenericArguments()
//            .FirstOrDefault() ?? throw new InvalidOperationException("Generic type for collection not founded.");

//        var select = typeof(Enumerable).GetMethods()
//            .Where(m => m.Name == nameof(Enumerable.Select))
//            .First(m => m.GetParameters().Length == 2)
//            .MakeGenericMethod(elementType, elementType);

//        var toArray = typeof(Enumerable).GetMethods()
//            .Where(m => m.Name == nameof(Enumerable.ToArray))
//            .First()
//            .MakeGenericMethod(elementType);

//        var lambdaBody = Expression.MemberInit(Expression.New(type), bindings);

//        var selectLambda = Expression.Lambda(lambdaBody, (ParameterExpression)bindParameter);

//        var memberAccess = Expression.Property(accessParameter, property);

//        var call = Expression.Call(select, memberAccess, selectLambda);

//        call = Expression.Call(toArray, call);

//        return Expression.Bind(property, call);
//    }
//}