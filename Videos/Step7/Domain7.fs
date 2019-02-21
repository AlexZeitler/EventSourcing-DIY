namespace Step7.Domain

open Step7.Infrastructure

type Truck = Truck of System.Guid

type Flavour =
| Vanilla
| Strawberry

type Event =
| Truck_added_to_fleet of Truck
| Truck_already_in_fleet of Truck
| Flavour_sold of Truck * Flavour
| Flavour_restocked of Truck * Flavour * int
| Flavour_went_out_of_stock of Truck * Flavour
| Flavour_was_not_in_stock of Truck * Flavour



module Projections =

  let project projection events =
    events |> List.fold projection.Update projection.Init

  let soldOfFlavour flavour state =
    state
    |> Map.tryFind flavour
    |> Option.defaultValue 0

  let private updateSoldFlavours state event =
    match event with
    | Flavour_sold (_,flavour) ->
        state
        |> soldOfFlavour flavour
        |> fun portions -> state |> Map.add flavour (portions + 1)

    | _ ->
        state

  let soldFlavours : Projection<Map<Flavour,int>, Event> =
    {
      Init = Map.empty
      Update = updateSoldFlavours
    }

  let restock flavour number stock =
    stock
    |> Map.tryFind flavour
    |> Option.defaultValue 0
    |> fun portions -> stock |> Map.add flavour (portions + number)

  let updateFlavoursInStock stock event =
    match event with
    | Flavour_sold (_, flavour) ->
        stock |> restock flavour -1

    | Flavour_restocked (_, flavour, portions) ->
        stock |> restock flavour portions

    | _ ->
        stock

  let flavoursInStock : Projection<Map<Flavour, int>, Event> =
    {
      Init = Map.empty
      Update = updateFlavoursInStock
    }

  let stockOf flavour stock =
    stock
    |> Map.tryFind flavour
    |> Option.defaultValue 0


module Behaviour =

  open Projections

  let sellFlavour truck flavour events =
    let stock =
      events
      |> project flavoursInStock
      |> stockOf flavour

    match stock with
    | 0 -> [Flavour_was_not_in_stock (truck,flavour)]
    | 1 -> [Flavour_sold (truck,flavour) ; Flavour_went_out_of_stock (truck, flavour)]
    | _ -> [Flavour_sold (truck,flavour)]


  let restock truck flavour portions events =
    [ Flavour_restocked (truck,flavour,portions) ]


  let addTruckToFleet truck events =
    [Truck_added_to_fleet truck]
