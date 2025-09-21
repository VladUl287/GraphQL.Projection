using GraphQL.Projection.Helpers;
using GraphQL.Projection.Resolvers.Contracts;
using GraphQL.Projection.Strategy.Binding.Contracts;

namespace GraphQL.Projection.Fabrics;

public sealed class TypeBuilderFactory(ITypeResolver typeResolver, IBindingContext bindingContext)
{
    public ITypeBuilder CreateTypeBuilder() => new TypeBuilder(new FieldBuilder(this, typeResolver, bindingContext));
}
