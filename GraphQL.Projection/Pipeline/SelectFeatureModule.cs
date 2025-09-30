using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQL.Projection.Resolvers;
using GraphQLParser.AST;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Pipes;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        return (document, model) =>
        {
            GraphQLSelectionSet? qLSelectionSet = null;
            GraphQLField? qlField = null;
            foreach (var definition in document.Definitions)
            {
                if (definition is { Kind: ASTNodeKind.OperationDefinition } and GraphQLOperationDefinition operation)
                {
                    foreach (var selection in operation.SelectionSet.Selections)
                    {
                        if (selection is { Kind: ASTNodeKind.Field } and GraphQLField field)
                        {
                            qlField = field;
                            qLSelectionSet = field.SelectionSet;
                        }
                    }
                    break;
                }
            }

            ArgumentNullException.ThrowIfNull(qlField);
            ArgumentNullException.ThrowIfNull(qLSelectionSet);

            var test = Build<TEntity>(qlField).Run();
            var select = test.ThrowIfFail();

            return model with
            {
                Select = select
            };
        };
    }

    public static Eff<Expression<Func<TEntity, TEntity>>> Build<TEntity>(GraphQLField field)
    {
        var result = Validate(field)
            .SelectMany(validated => CreateParameter<TEntity>())
            .SelectMany(parameter => BuildBindings<TEntity>(parameter, field))
            .SelectMany(bindings => CreateMemberInit<TEntity>(bindings))
            .SelectMany(memberInit => CreateLambda<TEntity>(memberInit));

        return result;
    }


    private static Eff<Expression<Func<TEntity, TEntity>>> CreateLambda<TEntity>(MemberInitExpression memberInit)
    {
        return Prelude.Eff(() =>
        {
            var parameter = new ParameterResolver().GetParameterExpression(memberInit);
            return Expression.Lambda<Func<TEntity, TEntity>>(memberInit, parameter);
        });
    }

    private static Eff<GraphQLField> Validate(GraphQLField field)
    {
        return Prelude
            .EffMaybe(() => field is not null ? Fin<GraphQLField>.Succ(field) : Fin<GraphQLField>.Fail(Error.New("Field is null")))
            .SelectMany((notNull) =>
            {
                return notNull.Name.IsNull() ?
                    Prelude.FailEff<GraphQLField>("Field name cannot be empty") :
                    Prelude.SuccessEff(notNull);
            });
    }

    private static Eff<Seq<MemberBinding>> BuildBindings<TEntity>(ParameterExpression parameter, GraphQLField field)
    {
        var fields = field.SelectionSet.Selections.Where(c => c is GraphQLField).Select(c => c as GraphQLField).ToArray();
        return fields
            .Map(selection => CreateBinding<TEntity>(parameter, selection))
            .Sequence()
            .SelectMany(bindings => bindings.IsDefault()
                ? Prelude.FailEff<Seq<MemberBinding>>("No valid bindings created")
                : Prelude.SuccessEff<Seq<MemberBinding>>(bindings.ToSeq()));
    }

    private static Eff<MemberBinding> CreateBinding<TEntity>(
        ParameterExpression parameter,
        GraphQLField selection)
    {
        return FindProperty<TEntity>(selection.Name.StringValue)
            .SelectMany(property => CreatePropertyBinding(parameter, selection, property));
    }

    private static Eff<PropertyInfo> FindProperty<TEntity>(string propertyName)
    {
        var property = typeof(TEntity).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return Eff<PropertyInfo>.Success(property)
            .SelectMany(prop => prop is null
                ? Prelude.FailEff<PropertyInfo>($"Property '{propertyName}' not found on User")
                : Prelude.SuccessEff(prop));
    }

    private static Eff<MemberBinding> CreatePropertyBinding(
        ParameterExpression parameter,
        GraphQLField selection,
        PropertyInfo property)
    {
        var type = property.PropertyType.IsPrimitive()
            ? CreateSimplePropertyAccess(parameter, property)
            : CreateNestedExpression(parameter, selection, property);

        return type.SelectMany(expression => Prelude.Eff(() => (MemberBinding)Expression.Bind(property, expression)));
    }

    private static Eff<Expression> CreateSimplePropertyAccess(ParameterExpression parameter, PropertyInfo property) =>
        Prelude.Eff(() => (Expression)Expression.MakeMemberAccess(parameter, property));

    private static Eff<Expression> CreateNestedExpression(ParameterExpression parameter, GraphQLField selection, PropertyInfo property)
    {
        return Prelude.Eff(() => Expression.MakeMemberAccess(parameter, property))
            .SelectMany(propertyAccess =>
                Prelude.Eff(() => property.PropertyType)
                    .SelectMany(nestedType =>
                        BuildNestedSelector(propertyAccess, selection, nestedType)));
    }

    private static Eff<Expression> BuildNestedSelector(
        Expression propertyAccess,
        GraphQLField field,
        Type nestedType)
    {
        return field.SelectionSet.Selections.Where(c => c is GraphQLField).Select(c => c as GraphQLField)
            .Map(selection => CreateNestedBinding(propertyAccess, selection, nestedType))
            .Sequence()
            .SelectMany(bindings =>
                Prelude.Eff(() => nestedType.GetConstructors().First())
                    .SelectMany(constructor =>
                        Prelude.Eff(() => Expression.New(constructor))
                            .SelectMany(newExpr =>
                                Prelude.Eff(() => (Expression)Expression.MemberInit(newExpr, bindings)))));
    }

    private static Eff<MemberBinding> CreateNestedBinding(
            Expression parentAccess,
            GraphQLField selection,
            Type parentType)
    {
        return Prelude.Eff(() => parentType.GetProperty(selection.Name.StringValue))
            .SelectMany(prop => prop is null
                ? Prelude.FailEff<PropertyInfo>($"Property '{selection.Name}' not found on {parentType.Name}")
                : Prelude.SuccessEff(prop))
            .SelectMany(property =>
            {
                var prop = selection.SelectionSet.Selections.Count == 0
                    ? Prelude.Eff(() => (Expression)Expression.Property(parentAccess, property))
                    : BuildNestedSelector(Expression.Property(parentAccess, property), selection, property.PropertyType);
                return prop.SelectMany(expression => Prelude.Eff(() => (MemberBinding)Expression.Bind(property, expression)));
            });
    }

    private static Eff<MemberInitExpression> CreateMemberInit<TEntity>(Seq<MemberBinding> bindings)
    {
        return Eff<ConstructorInfo>.Success(typeof(TEntity).GetConstructors().FirstOrDefault())
            .SelectMany(constructor =>
            {
                if (constructor is null)
                {
                    return Prelude.FailEff<ConstructorInfo>("No constructor found for object type");
                }
                return Prelude.SuccessEff(constructor);
            })
            .SelectMany(constructor => Eff<NewExpression>.Success(Expression.New(constructor)))
            .SelectMany(newExpr => Eff<MemberInitExpression>.Success(Expression.MemberInit(newExpr, bindings)));
    }

    private static Eff<ParameterExpression> CreateParameter<TEntity>() => Prelude.Eff(() => Expression.Parameter(typeof(TEntity)));
}