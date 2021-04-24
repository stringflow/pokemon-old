using System;
using System.Collections.Generic;
using System.Linq;

namespace Pokemon
{
    // TODO: Better documentation.
    public static class Pathfinding
    {

        public static Dictionary<T, List<(Action, int)>> Dijkstra<T>(Map<T> map, int stepCost, PermissionSet permissions, params T[] destinations) where T : Tile<T>
        {
            Dictionary<T, int> costs = new Dictionary<T, int>();
            Queue<T> tilesToCheck = new Queue<T>();

            // A modified implementation of Dijkstra's algorithm. (https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm)
            // Each tile is given a value representing the distance from destination. (also called the tile's 'cost')
            // Meaning the goal tile is 0, the four tiles around it have the value 17, their neighbors have the value 34, etc.
            // The goal tiles are the starting points for this implementation of the  algorithm. (It kind of searches in reverse)
            foreach (T destination in destinations)
            {
                costs[destination] = 0;
                tilesToCheck.Enqueue(destination);
            }

            // While there are tiles to check in the queue.
            while (tilesToCheck.Count > 0)
            {
                // Dequeue the first tile in the queue.
                T current = tilesToCheck.Dequeue();
                T[] neighbors = current.Neighbors();
                for (int i = 0; i < neighbors.Length; i++)
                {
                    T neighbor = neighbors[i];
                    if (neighbor == null) continue;

                    Action action = (Action)(0x10 << i);
                    // Ledge hops have to be checked in reverse since the goal tiles are our starting points.
                    // In case of a ledge hop, 'current' would be the tile the player stands on upon completing the hop,
                    // 'neighbor' would be the ledge tile, and 'ledgeHopDest' would be the tile the player starts the hop on. 
                    // (it's called dest in this code because again, working in reverse)
                    T ledgeHopDest = neighbor.Neighbor(action);
                    // Checks if this actually is a ledge hop.
                    bool ledgeHop = ledgeHopDest != null && ledgeHopDest.IsLedgeHop(neighbor, action.Opposite());
                    if (ledgeHop)
                    {
                        // If it is, the 'ledgeHopDest' is our new destination for this edge.
                        neighbor = ledgeHopDest;
                    }
                    else if (!neighbor.IsPassable(current, permissions)) continue; // If it isn't a ledge hop, check if the tile is passable given the permissions provided.
                                                                                   // Ledge hops can skip this step as they are always passable.

                    // The cost of reaching the neighboring tile is the cost of the current tile plus the cost of the step. (either the ledge hop cost or the step cost)
                    int newCost = costs[current] + (ledgeHop ? current.LedgeCost() : stepCost);
                    // If the neighboring tile has never been explored yet or the cost of reaching the tile using this route is lower than the previous fastest path...
                    if (!costs.ContainsKey(neighbor) || costs[neighbor] > newCost)
                    {
                        // register the new cost and put the tile into the queue to check its neighbors.
                        costs[neighbor] = newCost;
                        tilesToCheck.Enqueue(neighbor);
                    }
                }
            }

            // After building up the grid of costs, the exact edge costs need to be figured out.
            // If you were to use the grid of costs as edge costs, ledge hops would never be the fastest option since
            // it doesn't take into account the consequences of the route multiple steps down the road.
            Dictionary<T, List<(Action, int)>> edgeCosts = new Dictionary<T, List<(Action, int)>>();

            // For each tile that was reached by the code above...
            foreach (T tile in costs.Keys)
            {
                edgeCosts[tile] = new List<(Action, int)>();
                // Find all the neighbors that were also reached by the code above.
                T[] neighbors = tile.Destinations().Where(n => n != null && costs.ContainsKey(n)).ToArray();
                foreach (T neighbor in neighbors)
                {
                    Action action = tile.ActionRequired(neighbor);
                    // edgeCost = (thereCost - hereCost) + stepCost
                    // So if the tiles on the path have costs of 51 then 34 (approaching destination), it'd be (34 - 51) + 17 = 0.
                    int edgeCost = (costs[neighbor] - costs[tile]) + (Math.Abs(neighbor.X - tile.X) > 1 || Math.Abs(neighbor.Y - tile.Y) > 1 ? tile.LedgeCost() : stepCost);
                    edgeCosts[tile].Add((action, edgeCost));
                }
            }

            return edgeCosts;
        }

        public static void GenerateEdges<T>(Map<T> map, int edgeSet, int stepCost, PermissionSet permissions, Action avaiableActions, params T[] destinations) where T : Tile<T>
        {
            bool gen2 = map is GscMap;
            Dictionary<T, List<(Action, int)>> edges = Dijkstra(map, stepCost, permissions, destinations);

            foreach (T tile in edges.Keys)
            {
                foreach ((Action Action, int Cost) edge in edges[tile])
                {
                    if ((avaiableActions & edge.Action) > 0)
                    {
                        T dest = tile.Destination(edge.Action);
                        dest = dest.WarpCheck();
                        tile.AddEdge(edgeSet, new Edge<T>
                        {
                            Action = edge.Action,
                            NextTile = dest,
                            NextEdgeset = edgeSet,
                            Cost = edge.Cost,
                        });

                        if (gen2)
                        { // Also add the A+_ action
                            tile.AddEdge(edgeSet, new Edge<T>
                            {
                                Action = edge.Action | Action.A,
                                NextTile = dest,
                                NextEdgeset = edgeSet,
                                Cost = edge.Cost,
                            });
                        }
                    }
                }

                if ((avaiableActions & Action.StartB) > 0)
                {
                    tile.AddEdge(edgeSet, new Edge<T>
                    {
                        Action = Action.StartB,
                        NextTile = tile,
                        NextEdgeset = edgeSet,
                        Cost = gen2 ? 91 : 52,
                    });
                }

                if (!gen2 && (avaiableActions & Action.A) > 0)
                {
                    tile.AddEdge(edgeSet, new Edge<T>
                    {
                        Action = Action.A,
                        NextTile = tile,
                        NextEdgeset = edgeSet,
                        Cost = 2,
                    });
                }
            }
        }

        public static List<Action> FindPath<T>(Map<T> map, T start, int stepCost, PermissionSet permissions, params T[] destinations) where T : Tile<T>
        {
            Dictionary<T, List<(Action Action, int Cost)>> edges = Dijkstra(map, stepCost, permissions, destinations);

            T current = start;
            List<Action> path = new List<Action>();
            while (!destinations.Contains(current))
            {
                // Choose the neighbor with the lowest cost and add the action to the path.
                Action action = edges[current].OrderBy(edge => edge.Cost).First().Action;
                path.Add(action);
                current = current.Destination(action);
            }

            return path;
        }

        private static readonly Dictionary<Action, int[]> DebugArrows = new Dictionary<Action, int[]>() {
            { Action.Up, new int[] { 7, 1, 8, 1, 6, 2, 7, 2, 8, 2, 9, 2, 5, 3, 6, 3, 9, 3, 10, 3 }},
            { Action.Down, new int[] { 7, 14, 8, 14, 6, 13, 7, 13, 8, 13, 9, 13, 5, 12, 6, 12, 9, 12, 10, 12 }},
            { Action.Left, new int[] { 1, 7, 1, 8, 2, 6, 2, 7, 2, 8, 2, 9, 3, 5, 3, 6, 3, 9, 3, 10 }},
            { Action.Right, new int[] { 14, 7, 14, 8, 13, 6, 13, 7, 13, 8, 13, 9, 12, 5, 12, 6, 12, 9, 12, 10 }},
        };

        public static void DebugGenerateEdges<T>(Map<T> map, int stepCost, int edgeSet, PermissionSet permissions, params T[] destinations) where T : Tile<T>
        {
            GenerateEdges(map, edgeSet, stepCost, permissions, Action.Right | Action.Left | Action.Up | Action.Down, destinations);
            DebugDrawEdges(map, edgeSet);
        }

        public static void DebugDrawEdges<T>(Map<T> map, int edgeSet) where T : Tile<T>
        {
            Bitmap bitmap = map.Render();
            foreach (T tile in map.Tiles)
            {
                if (tile.Edges.Count == 0) continue;
                int minCost = tile.Edges[edgeSet].Min(n => n.Cost);
                foreach (Edge<T> edge in tile.Edges[edgeSet])
                {
                    if (edge.Cost == minCost && DebugArrows.ContainsKey(edge.Action))
                    {
                        int[] positions = DebugArrows[edge.Action];
                        for (int i = 0; i < positions.Length; i += 2)
                        {
                            bitmap.SetPixel(tile.X * 16 + positions[i], tile.Y * 16 + positions[i + 1], 0xff, 0x00, 0x00);
                        }
                    }
                }
            }

            bitmap.Save("debug_edges.png");
        }

        public static void DebugFindPath<T>(Map<T> map, T start, int stepCost, PermissionSet permissions, params T[] destinations) where T : Tile<T>
        {
            List<Action> path = FindPath(map, start, stepCost, permissions, destinations);

            Bitmap bitmap = map.Render();
            T current = start;
            foreach (Action action in path)
            {
                int[] positions = DebugArrows[action];
                for (int i = 0; i < positions.Length; i += 2)
                {
                    bitmap.SetPixel(current.X * 16 + positions[i], current.Y * 16 + positions[i + 1], 0xff, 0x00, 0x00);
                }
                current = current.Destination(action);
            }

            bitmap.Save("debug_find_path.png");
        }
    } 
}