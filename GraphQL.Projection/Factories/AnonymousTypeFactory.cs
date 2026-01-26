using System.Reflection;
using System.Reflection.Emit;

namespace GraphQL.Projection.Factories;

public static class AnonymousTypeFactory
{
    public static Type CreateType(string[] propNames)
    {
        var asmName = new AssemblyName("DynamicProjections");
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType("DynamicProjection", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        foreach (var name in propNames)
        {
            var field = typeBuilder.DefineField("_" + name, typeof(object), FieldAttributes.Private);
            var prop = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, typeof(object), null);
            var getter = typeBuilder.DefineMethod("get_" + name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(object), Type.EmptyTypes);

            var getterIL = getter.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, field);
            getterIL.Emit(OpCodes.Ret);
            prop.SetGetMethod(getter);

            var setter = typeBuilder.DefineMethod("set_" + name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [typeof(object)]);
            var setterIL = setter.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, field);
            setterIL.Emit(OpCodes.Ret);
            prop.SetSetMethod(setter);

        }

        return typeBuilder.CreateType();
    }
}
