module Tests

open StaticProvider.Provided
open System.IO

let Mark() =
    File.WriteAllText("D:/mark.txt", "hey")

type S = Static<"Mark", __SOURCE_FILE__>
