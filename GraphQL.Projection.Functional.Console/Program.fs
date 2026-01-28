open GraphQLOp
open GraphQLOp.Operations

let userQuery = 
    object' "user" [
        field "id" []
        field "name" []
    ]

let ast = interpret userQuery

printfn "AST: %A" ast