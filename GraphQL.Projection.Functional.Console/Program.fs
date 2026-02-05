open GraphQLOp.Operations
open QueryBuilder
open System.Text.Json
open System.Linq

let userQuery = 
    field "user" [] None [] [
        field "id" [] None [] []
        field "name" [] (Some "tset") [] []
        field "phone" [] None [] [
            field "country" [] None [] []
            field "number" [] None [] []
        ]
        field "languages" [] None [] []
        field "achievements" [] None [] [
            field "id" [] None [] []
            field "name" [] None [] []
            field "description" [] (Some "desc") [] []
        ]
    ]

let ast = interpret userQuery
printfn "AST: %A" ast

type Achievement = { Id: int; Name: string; Description: string; }
type Phone = { Country: string; Number: string; }
type User = { Id: int; Name: string; Phone: Phone; Languages: string list; Achievements: Achievement list }

let selector = buildSelector<User> ast
printfn "\nSelector: %A" selector

let phone = { Country = "+1"; Number = "555-1234" }

let user = { 
    Id = 1
    Name = "John Doe" 
    Phone = phone
    Languages = ["ru"; "en"; "fr"]
    Achievements = [
        { Id = 1; Name = "test"; Description = "description" }
        { Id = 2; Name = "test2"; Description = "description2" }
    ]
}

let delegat = selector.Compile()
let obj = delegat.Invoke user 

let userJson = JsonSerializer.Serialize(user)
printfn "\nUser json: %s" userJson

let objJson = JsonSerializer.Serialize(obj)
printfn "\nObj json: %s" objJson
