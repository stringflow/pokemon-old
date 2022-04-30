using System;
using System.Collections.Generic;
using System.Linq;

public static class Pathfinding {

    const int BonkCost = 8;
    const int WarpCost = 100;

    public static List<Action> FindPath<M, T>(PokemonGame gb, T startTile, T endTile, Action preferredEndDirection = Action.None, params T[] additionallyBlockedTiles) where M : Map<M, T>
                                                                                                                                                                       where T : Tile<M, T> {
        Debug.Assert(gb.SaveLoaded(), "Warning: Unable to read event flags because no save is loaded!");
        byte[] collisionMap = gb.ReadCollisionMap();

        Dictionary<T, int> costs = new Dictionary<T, int>();
        Dictionary<T, bool> onBike = new Dictionary<T, bool>();
        Dictionary<T, T> previousTiles = new Dictionary<T, T>();
        Dictionary<T, Action[]> previousActions = new Dictionary<T, Action[]>();
        Queue<T> tileQueue = new Queue<T>();

        bool biking = gb.Biking();
        bool surfing = gb.Surfing();

        costs[startTile] = 0;
        onBike[startTile] = biking;
        tileQueue.Enqueue(startTile);

        while(tileQueue.Count > 0) {
            T currentTile = tileQueue.Dequeue();

            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                T neighborTile = currentTile.GetNeighbor(action);

                bool ledgeHop = neighborTile != null && currentTile.LedgeCheck(neighborTile, action);
                if(ledgeHop) {
                    neighborTile = neighborTile.GetNeighbor(action);
                }

                if(neighborTile == null || (additionallyBlockedTiles != null && Array.IndexOf(additionallyBlockedTiles, neighborTile) != -1)) continue;

                bool landCollision = !currentTile.CollisionCheckLand(gb, neighborTile, neighborTile.Map.Id == startTile.Map.Id ? collisionMap : null, action, neighborTile == endTile);

                if(!surfing) {
                    if(landCollision) continue;
                } else {
                    bool waterCollision = !currentTile.CollisionCheckWater(gb, neighborTile, neighborTile.Map.Id == startTile.Map.Id ? collisionMap : null, action, neighborTile == endTile);
                    if(waterCollision && !(!landCollision && neighborTile == endTile)) continue;
                }

                var warp = neighborTile.WarpCheck();
                bool isWarp = warp.TileToWarpTo != null;
                bool directionalWarp = warp.ActionRequired != Action.None;

                AddNewState((isWarp ? warp.TileToWarpTo : neighborTile).DoorCheck(), isWarp, directionalWarp, ledgeHop, warp.ActionRequired);
                if(directionalWarp || (isWarp && neighborTile == endTile)) AddNewState(neighborTile, false, false, false, Action.None);

                void AddNewState(T newTile, bool isWarp, bool isDirectionalWarp, bool isLedgeHop, Action directionalWarpAction) {
                    int cost = newTile.CalcStepCost(onBike[currentTile], isLedgeHop, isWarp, action);
                    if(isDirectionalWarp && directionalWarpAction != action) cost += BonkCost;

                    int newCost = costs[currentTile] + cost;
                    if(costs.ContainsKey(endTile) && newCost > costs[endTile] + WarpCost) return;

                    onBike[newTile] = onBike[currentTile] && newTile.Map.Id != currentTile.Map.Id ? newTile.Map.AllowsBiking() : onBike[currentTile];

                    if(!costs.ContainsKey(newTile) || costs[newTile] > newCost) {
                        costs[newTile] = newCost;
                        previousTiles[newTile] = currentTile;
                        previousActions[newTile] = isDirectionalWarp ? new Action[] { action, directionalWarpAction } : new Action[] { action };
                        tileQueue.Enqueue(newTile);
                    }
                }
            }
        }

        Action endAction = Action.None;

        T newEndTile = null;
        if(!costs.ContainsKey(endTile)) {
            int minCost = int.MaxValue;
            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                if(preferredEndDirection != Action.None && action != preferredEndDirection.Opposite()) continue;

                T t1 = endTile.GetNeighbor(action);
                if(t1 != null && costs.ContainsKey(t1)) {
                    int cost = costs[t1] + BonkCost;
                    if(minCost > cost) {
                        minCost = cost;
                        newEndTile = t1;
                        endAction = action.Opposite();
                    }

                    T t2 = t1.GetNeighbor(action);
                    if(t2 != null && costs.ContainsKey(t2)) {
                        cost = costs[t2] + t2.CalcStepCost(onBike[t1], false, false, action);
                        if(minCost > cost) {
                            minCost = cost;
                            newEndTile = t2;
                            endAction = action.Opposite();
                        }
                    }
                }
            }
        } else if(preferredEndDirection != Action.None) {
            endAction = preferredEndDirection;
            newEndTile = endTile.GetNeighbor(preferredEndDirection.Opposite());
        }

        if(newEndTile != null) {
            endTile = newEndTile;
        }

        List<Action> path = new List<Action>();
        T tile = endTile;
        while(tile != startTile) {
            path.AddRange(previousActions[tile].Reverse());
            tile = previousTiles[tile];
        }

        path.Reverse();
        if(endAction != Action.None) path.Add(endAction);

        return path;
    }

    // NOTE: This function explores the map in reverse. Because of this I ran into issues with directional warps and there are therefore ignored for now.
    //       Once this is fixed, it should be possible to have both exploration systems in one function instead of duplicating this code.
    public static void GenerateEdges<M, T>(PokemonGame gb, int edgeSet, T endTile, Action availableActions, params T[] additionallyBlockedTiles) where M : Map<M, T>
                                                                                                                                                 where T : Tile<M, T> {
        Debug.Assert(gb.SaveLoaded(), "Warning: Pathfinding is unable to read event flags because no save is loaded!");

        Dictionary<T, int> costs = new Dictionary<T, int>();
        Dictionary<T, bool> onBike = new Dictionary<T, bool>();
        Queue<T> tileQueue = new Queue<T>();

        Dictionary<T, Dictionary<Action, int>> actionCosts = new Dictionary<T, Dictionary<Action, int>>();
        Dictionary<T, Dictionary<Action, T>> nextTiles = new Dictionary<T, Dictionary<Action, T>>();

        costs[endTile] = 0;
        tileQueue.Enqueue(endTile);

        while(tileQueue.Count > 0) {
            T currentTile = tileQueue.Dequeue();
            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                T sourceTile = currentTile;
                T neighborTile = currentTile.GetNeighbor(action);

                var warp = currentTile.WarpCheck();
                bool isWarp = warp.TileToWarpTo != null;
                // TODO: Directional warps
                if(isWarp && warp.ActionRequired == Action.None) {
                    sourceTile = warp.TileToWarpTo;
                    neighborTile = sourceTile.GetNeighbor(action);
                }

                if(neighborTile == null) continue;

                T ledgeHopDest = neighborTile.GetNeighbor(action);
                bool isLedgeHop = ledgeHopDest != null && ledgeHopDest.LedgeCheck(neighborTile, action.Opposite());
                if(isLedgeHop) {
                    neighborTile = ledgeHopDest;
                }

                // TODO: Check if this does tile pair collision correctly
                bool landCollision = !sourceTile.CollisionCheckLand(gb, neighborTile, null, action, false);
                if(landCollision) continue;

                // TODO: Bike, Surfing
                int actionCost = neighborTile.CalcStepCost(false, isLedgeHop, isWarp, action);
                int currentCost = costs[currentTile];
                int newCost = currentCost + actionCost;
                if(!costs.ContainsKey(neighborTile) || costs[neighborTile] > newCost) {
                    costs[neighborTile] = newCost;
                    tileQueue.Enqueue(neighborTile);
                }

                if(!actionCosts.ContainsKey(neighborTile)) {
                    actionCosts[neighborTile] = new Dictionary<Action, int>();
                    nextTiles[neighborTile] = new Dictionary<Action, T>();
                }

                action = action.Opposite();
                actionCosts[neighborTile][action] = actionCost;
                nextTiles[neighborTile][action] = currentTile.DoorCheck();
            }
        }

        bool gen2 = gb is Gsc;

        foreach(T tile in costs.Keys) {
            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                if((availableActions & action) == 0 || !actionCosts[tile].ContainsKey(action)) continue;

                T dest = nextTiles[tile][action];
                int edgeCost = Math.Max((costs[dest] - costs[tile]) + actionCosts[tile][action], 0);
                tile.AddEdge(edgeSet, new Edge<M, T> {
                    Action = action,
                    NextTile = dest,
                    NextEdgeset = edgeSet,
                    Cost = edgeCost,
                });

                if(gen2) {
                    tile.AddEdge(edgeSet, new Edge<M, T> {
                        Action = action | Action.A,
                        NextTile = dest,
                        NextEdgeset = edgeSet,
                        Cost = edgeCost,
                    });
                }
            }

            if((availableActions & Action.A) > 0 && !gen2) {
                tile.AddEdge(edgeSet, new Edge<M, T> {
                    Action = Action.A,
                    NextTile = tile,
                    NextEdgeset = edgeSet,
                    Cost = 2,
                });
            }

            if((availableActions & Action.StartB) > 0) {
                tile.AddEdge(edgeSet, new Edge<M, T> {
                    Action = Action.StartB,
                    NextTile = tile,
                    NextEdgeset = edgeSet,
                    Cost = gen2 ? 91 : 52,
                });
            }
        }
    }

    private static readonly Dictionary<Action, int[]> DebugArrows = new Dictionary<Action, int[]>() {
            { Action.Up, new int[] { 7, 1, 8, 1, 6, 2, 7, 2, 8, 2, 9, 2, 5, 3, 6, 3, 9, 3, 10, 3 }},
            { Action.Down, new int[] { 7, 14, 8, 14, 6, 13, 7, 13, 8, 13, 9, 13, 5, 12, 6, 12, 9, 12, 10, 12 }},
            { Action.Left, new int[] { 1, 7, 1, 8, 2, 6, 2, 7, 2, 8, 2, 9, 3, 5, 3, 6, 3, 9, 3, 10 }},
            { Action.Right, new int[] { 14, 7, 14, 8, 13, 6, 13, 7, 13, 8, 13, 9, 12, 5, 12, 6, 12, 9, 12, 10 }},
        };

    public static void DebugDrawEdges<M, T>(PokemonGame gb, Map<M, T> map, int edgeSet) where T : Tile<M, T>
                                                                                        where M : Map<M, T> {
        bool isRed = gb.ROM.Title == "POKEMON RED";

        byte r = (byte) (isRed ? 0x00 : 0xff);
        byte g = 0x00;
        byte b = (byte) (isRed ? 0xff : 0x00);

        Bitmap bitmap = map.Render();
        foreach(T tile in map.Tiles) {
            if(!tile.Edges.ContainsKey(edgeSet)) continue;
            int minCost = tile.Edges[edgeSet].Min(n => n.Cost);
            foreach(Edge<M, T> edge in tile.Edges[edgeSet]) {
                if(edge.Cost == minCost && DebugArrows.ContainsKey(edge.Action)) {
                    int[] positions = DebugArrows[edge.Action];
                    for(int i = 0; i < positions.Length; i += 2) {
                        bitmap.SetPixel(tile.X * 16 + positions[i], tile.Y * 16 + positions[i + 1], r, g, b);
                    }
                }
            }
        }

        bitmap.Save("debug_edges.png");
    }
}