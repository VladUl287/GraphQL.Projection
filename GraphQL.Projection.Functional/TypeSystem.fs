module TypeSystem
open System
open System.Collections
open System.Collections.Generic

type TypeInspector = {
    IsPrimitive: Type -> bool
    IsCollection: Type -> bool
    IsSubtypeOf: Type -> string -> bool
    GetElementType: Type -> Type option
}

let defaultInspector: TypeInspector =
    let rec isSubtypeOf (typ: Type) (typeName: string): bool =
        typ <> null && (
            typ.Name = typeName || isSubtypeOf typ.BaseType typeName ||
            typ.GetInterfaces() 
                |> Array.exists (fun iface -> isSubtypeOf iface typeName)
        )

    let rec isPrimitive (typ: Type): bool = 
        typ.IsPrimitive || typ.IsEnum || typ = typeof<string> || typ = typeof<Guid> ||
        typ = typeof<DateTime> || typ = typeof<DateTimeOffset> || typ = typeof<TimeSpan> || 
        typ = typeof<DateOnly> || typ = typeof<TimeOnly>  || typ = typeof<decimal> || (
            typ.IsGenericType && typ.GetGenericTypeDefinition() = typeof<Nullable<_>> && 
            typ.GetGenericArguments() 
                |> Array.tryItem 0 
                |> Option.exists isPrimitive 
        )

    let isCollection (typ: Type): bool = typ.IsAssignableTo(typeof<IEnumerable>)

    let rec getElementType (typ: Type): Type option =
        match typ with
        | t when t.IsArray -> Some (t.GetElementType())
        | t when t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> -> 
            t.GetGenericArguments() 
                |> Array.tryItem 0
        | t ->
            t.GetInterfaces() 
                |> Array.tryPick getElementType

    {
        IsPrimitive = isPrimitive
        IsSubtypeOf = isSubtypeOf
        IsCollection = isCollection
        GetElementType = getElementType
    }