module Result

let traverse f list =
    let rec loop acc = function
        | [] -> Ok (List.rev acc)
        | x::xs ->
            match f x with
            | Ok x' -> loop (x'::acc) xs
            | Error e -> Error e
    loop [] list
