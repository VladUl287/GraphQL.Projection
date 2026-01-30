module CretaeAnonymousType

open System
open System.Reflection.Emit
open System.Reflection

let createAnonymousType (properties: (string * Type) list) : Type =
    let assemblyName = AssemblyName("DynamicTypes")
    let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule")

    let typeBuilder = moduleBuilder.DefineType("AnonymousType", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.Sealed)
    
    properties |> List.iter (fun (name, typ) ->
        let fieldBuilder = typeBuilder.DefineField("_" + name, typ, FieldAttributes.Private)
        let propBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, typ, null)
        
        // Getter
        let getter = typeBuilder.DefineMethod("get_" + name, 
            MethodAttributes.Public ||| MethodAttributes.SpecialName ||| MethodAttributes.HideBySig,
            typ, Type.EmptyTypes)
        let getterIL = getter.GetILGenerator()
        getterIL.Emit(OpCodes.Ldarg_0)
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder)
        getterIL.Emit(OpCodes.Ret)
        propBuilder.SetGetMethod(getter)
        
        // Constructor
        let ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, 
            CallingConventions.Standard, [| for (_, t) in properties -> t |])
        let ctorIL = ctor.GetILGenerator()
        ctorIL.Emit(OpCodes.Ldarg_0)
        ctorIL.Emit(OpCodes.Call, typeof<obj>.GetConstructor(Type.EmptyTypes))
        
        for i = 0 to properties.Length - 1 do
            ctorIL.Emit(OpCodes.Ldarg_0)
            ctorIL.Emit(OpCodes.Ldarg, i + 1)
            ctorIL.Emit(OpCodes.Stfld, typeBuilder.GetField("_" + fst properties.[i]))
        
        ctorIL.Emit(OpCodes.Ret)
    )
    
    typeBuilder.CreateType()