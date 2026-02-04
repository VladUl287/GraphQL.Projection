module CretaeAnonymousType

open System
open System.Reflection.Emit
open System.Reflection

let createAnonymousType (properties: (string * Type) list) : Type =
    let assemblyName = AssemblyName("DynamicTypes")
    let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule")

    let typeBuilder = moduleBuilder.DefineType("AnonymousType", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.Sealed)
    
    let fieldBuilders = System.Collections.Generic.Dictionary<string, FieldBuilder>()

    properties |> List.iter (fun (name, typ) ->
        let fieldBuilder = typeBuilder.DefineField("_" + name, typ, FieldAttributes.Private)
        let propBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, typ, null)
        fieldBuilders.[name] <- fieldBuilder

        // Getter
        let getter = typeBuilder.DefineMethod("get_" + name, 
            MethodAttributes.Public ||| MethodAttributes.SpecialName ||| MethodAttributes.HideBySig,
            typ, Type.EmptyTypes)
        let getterIL = getter.GetILGenerator()
        getterIL.Emit(OpCodes.Ldarg_0)
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder)
        getterIL.Emit(OpCodes.Ret)
        propBuilder.SetGetMethod(getter)
    )

    // Constructor
    let paramTypes = [| for (_, t) in properties -> t |]
    let ctor = typeBuilder.DefineConstructor(
        MethodAttributes.Public, 
        CallingConventions.Standard, 
        paramTypes)

    let ctorIL = ctor.GetILGenerator()
    ctorIL.Emit(OpCodes.Ldarg_0)
    ctorIL.Emit(OpCodes.Call, typeof<obj>.GetConstructor(Type.EmptyTypes))
    
    for i = 0 to properties.Length - 1 do
        let (propName, _) = properties.[i]
        let fieldBuilder = fieldBuilders.[propName]

        ctorIL.Emit(OpCodes.Ldarg_0)
        ctorIL.Emit(OpCodes.Ldarg, i + 1)
        ctorIL.Emit(OpCodes.Stfld, fieldBuilder)
    
    ctorIL.Emit(OpCodes.Ret)
    
    typeBuilder.CreateType()
        
let createJsonSerializableType(properties: (string * Type) list) =
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
    
    // Add properties with public getters/setters (C# style)
    let fields = ResizeArray<FieldBuilder>()
    let ctorParams = ResizeArray<Type>()
    
    for (propName, propType) in properties do
        // Define backing field
        let field = typeBuilder.DefineField(
            "_" + propName,
            propType,
            FieldAttributes.Private
        )
        fields.Add(field)
        ctorParams.Add(propType)
        
        // Define property
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
    
    // Add parameterized constructor
    let ctor = typeBuilder.DefineConstructor(
        MethodAttributes.Public,
        CallingConventions.Standard,
        ctorParams.ToArray()
    )
    let ctorIl = ctor.GetILGenerator()
    ctorIl.Emit(OpCodes.Ldarg_0)
    ctorIl.Emit(OpCodes.Call, typeof<obj>.GetConstructor([||]))
    
    for i = 0 to ctorParams.Count - 1 do
        ctorIl.Emit(OpCodes.Ldarg_0)
        ctorIl.Emit(OpCodes.Ldarg, i + 1)
        ctorIl.Emit(OpCodes.Stfld, fields.[i])
    
    ctorIl.Emit(OpCodes.Ret)
    
    // FINISH TYPE
    let dynamicType = typeBuilder.CreateType()
    dynamicType
