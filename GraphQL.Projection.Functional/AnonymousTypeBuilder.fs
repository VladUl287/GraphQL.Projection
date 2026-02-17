module AnonymousTypeBuilder

open System
open System.Reflection.Emit
open System.Reflection

type AnonymousTypeFactory = (string * Type) list -> Type

let createAnonymousType(properties: (string * Type) list): Type =
    let assemblyName = AssemblyName("DynamicTypes")
    let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule")
    
    let typeBuilder = moduleBuilder.DefineType(
        "Dynamic_" + Guid.NewGuid().ToString("N"),
        TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.Sealed
    )
    
    let emptyCtor = typeBuilder.DefineConstructor(
        MethodAttributes.Public,
        CallingConventions.Standard,
        [||]
    )
    let emptyIl = emptyCtor.GetILGenerator()
    emptyIl.Emit(OpCodes.Ldarg_0)
    emptyIl.Emit(OpCodes.Call, typeof<obj>.GetConstructor([||]))
    emptyIl.Emit(OpCodes.Ret)
        
    for (propName, propType) in properties do
        let field = typeBuilder.DefineField(
            "_" + propName,
            propType,
            FieldAttributes.Private
        )
        
        let property = typeBuilder.DefineProperty(
            propName,
            PropertyAttributes.HasDefault,
            propType,
            null
        )
        
        // Getter
        let getter = typeBuilder.DefineMethod(
            "get_" + propName,
            MethodAttributes.Public ||| MethodAttributes.SpecialName ||| MethodAttributes.HideBySig,
            propType,
            Type.EmptyTypes
        )
        let getterIl = getter.GetILGenerator()
        getterIl.Emit(OpCodes.Ldarg_0)
        getterIl.Emit(OpCodes.Ldfld, field)
        getterIl.Emit(OpCodes.Ret)
        
        // Setter
        let setter = typeBuilder.DefineMethod(
            "set_" + propName,
            MethodAttributes.Public ||| MethodAttributes.SpecialName ||| MethodAttributes.HideBySig,
            null,
            [| propType |]
        )
        let setterIl = setter.GetILGenerator()
        setterIl.Emit(OpCodes.Ldarg_0)
        setterIl.Emit(OpCodes.Ldarg_1)
        setterIl.Emit(OpCodes.Stfld, field)
        setterIl.Emit(OpCodes.Ret)
        
        property.SetGetMethod(getter)
        property.SetSetMethod(setter)
        
    typeBuilder.CreateType()
