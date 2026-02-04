open GraphQLOp.Operations
open QueryBuilder
open System.Text.Json

let userQuery = 
    field "user" [] None [] [
        field "id" [] None [] []
        //field "name" [] (Some "tset") [] []
        //field "phone" [] None [] [
        //    field "country" [] None [] []
        //    field "number" [] None [] []
        //]
    ]

let ast = interpret userQuery
printfn "AST: %A" ast

type Phone = { Country: string; Number: string; }
type User = { Id: int; Name: string; Phone: Phone }

let selector = buildSelector<User> ast
printfn "\nSelector: %A" selector

let phone = { Country = "+1"; Number = "555-1234" }

let user = { 
    Id = 1
    Name = "John Doe" 
    Phone = phone 
}

let delegat = selector.Compile()
let obj = delegat.Invoke user 

let userJson = JsonSerializer.Serialize(user)
printfn "User json: %s" userJson

let objJson = JsonSerializer.Serialize(obj)
printfn "Obj json: %s" objJson
