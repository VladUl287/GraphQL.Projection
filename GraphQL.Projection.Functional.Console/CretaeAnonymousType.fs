module CretaeAnonymousType

open System
open System.Reflection.Emit
open System.Reflection

let createDynamicType() =
    let assemblyName = AssemblyName("DynamicTypes")
    let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule")
    let typeBuilder = moduleBuilder.DefineType("DynamicResult", TypeAttributes.Public)
    
    typeBuilder.DefineField("Id", typeof<int>, FieldAttributes.Public) |> ignore
    typeBuilder.DefineField("Name", typeof<string>, FieldAttributes.Public) |> ignore
    
    let phoneTypeBuilder = moduleBuilder.DefineType("DynamicPhone", TypeAttributes.Public)
    phoneTypeBuilder.DefineField("Country", typeof<string>, FieldAttributes.Public) |> ignore
    phoneTypeBuilder.DefineField("Number", typeof<string>, FieldAttributes.Public) |> ignore
    let phoneType = phoneTypeBuilder.CreateType()
    
    typeBuilder.DefineField("Phone", phoneType, FieldAttributes.Public) |> ignore
    
    typeBuilder.CreateType()

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
