using System.Collections.Generic;
using System.Linq;

// TODO: Better documentation.
public static class Pathfinding {

    // A simple implementation of Dijkstra's algorithm. (https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm)
    public static Dictionary<T, int> Dijkstra<T>(Map<T> map, int stepCost, PermissionSet permissions, params T[] destinations) where T : Tile<T> {
        Dictionary<T, int> costs = new Dictionary<T, int>();
        Queue<T> tilesToCheck = new Queue<T>();

        foreach(T destionation in destinations) {
            costs[destionation] = 0;
            tilesToCheck.Enqueue(destionation);
        }

        while(tilesToCheck.Count > 0) {
            T tile = tilesToCheck.Dequeue();
            T[] neighbors = tile.Neighbors();
            foreach(T neighbor in neighbors) {
                if(neighbor == null) continue;
                if(!neighbor.IsPassable(permissions)) continue;
                // TODO: Add ledge hop logic.
                int newCost = costs[tile] + stepCost;
                if(!costs.ContainsKey(neighbor) || costs[neighbor] > newCost) {
                    costs[neighbor] = newCost;
                    tilesToCheck.Enqueue(neighbor);
                }
            }
        }

        return costs;
    }

    public static void GenerateEdges<T>(Map<T> map, int edgeSet, int stepCost, PermissionSet permissions, Action avaiableActions, params T[] destinations) where T : Tile<T> {
        bool gen2 = map is GscMap;
        Dictionary<T, int> costs = Dijkstra(map, stepCost, permissions, destinations);

        foreach(T tile in map.Tiles) {
            if(!costs.ContainsKey(tile)) continue;
            T[] neighbors = tile.Neighbors().Where(n => n != null && costs.ContainsKey(n)).ToArray();
            // Use the neighbor with the lowest cost as a base line.
            int minCost = neighbors.Min(n => costs[n]);
            foreach(T neighbor in neighbors) {
                Action action = tile.ActionRequired(neighbor);
                if((avaiableActions & action) > 0) {
                    int edgeCost = destinations.Contains(tile)
                                                               ? 2 * stepCost // all movement edges from the destination should be 2 * step cost
                                                               : costs[neighbor] - minCost; // Otherwise the cost of each edge depends on how close it is to the base line.
                    tile.AddEdge(edgeSet, new Edge<T> {
                        Action = action,
                        NextTile = neighbor,
                        NextEdgeset = edgeSet,
                        Cost = edgeCost,
                    });

                    if(gen2) { // Also add the _+A action
                        tile.AddEdge(edgeSet, new Edge<T> {
                            Action = action | Action.A,
                            NextTile = neighbor,
                            NextEdgeset = edgeSet,
                            Cost = edgeCost,
                        });
                    }
                }
            }

            if((avaiableActions & Action.StartB) > 0) {
                tile.AddEdge(edgeSet, new Edge<T> {
                    Action = Action.StartB,
                    NextTile = tile,
                    NextEdgeset = edgeSet,
                    Cost = gen2 ? 91 : 52,
                });
            }

            if(!gen2 && (avaiableActions & Action.A) > 0) {
                tile.AddEdge(edgeSet, new Edge<T> {
                    Action = Action.A,
                    NextTile = tile,
                    NextEdgeset = edgeSet,
                    Cost = 2,
                });
            }
        }
    }

    public static List<Action> FindPath<T>(Map<T> map, int stepCost, T start, PermissionSet permissions, params T[] destinations) where T : Tile<T> {
        Dictionary<T, int> costs = Dijkstra(map, stepCost, permissions, destinations);

        System.Console.WriteLine(map.GetType());

        T current = start;
        List<Action> path = new List<Action>();
        while(!destinations.Contains(current)) {
            // Choose the neighbor with the lowest cost and add the action to the path.
            T neighbor = current.Neighbors().Where(n => n != null && costs.ContainsKey(n)).OrderBy(n => costs[n]).First();
            path.Add(current.ActionRequired(neighbor));
            current = neighbor;
        }

        return path;
    }

    private static readonly Dictionary<Action, int[]> DebugArrows = new Dictionary<Action, int[]>() {
            { Action.Up, new int[] { 7, 1, 8, 1, 6, 2, 7, 2, 8, 2, 9, 2, 5, 3, 6, 3, 9, 3, 10, 3 }},
            { Action.Down, new int[] { 7, 14, 8, 14, 6, 13, 7, 13, 8, 13, 9, 13, 5, 12, 6, 12, 9, 12, 10, 12 }},
            { Action.Left, new int[] { 1, 7, 1, 8, 2, 6, 2, 7, 2, 8, 2, 9, 2, 5, 3, 6, 3, 9, 3, 10 }},
            { Action.Right, new int[] { 14, 7, 14, 8, 13, 6, 13, 7, 13, 8, 13, 9, 13, 5, 12, 6, 12, 9, 12, 10 }},
        };

    public static void DebugGenerateEdges<T>(Map<T> map, int stepCost, int edgeSet, PermissionSet permissions, params T[] destinations) where T : Tile<T> {
        GenerateEdges(map, edgeSet, stepCost, permissions, Action.Right | Action.Left | Action.Up | Action.Down, destinations);
        DebugDrawEdges(map, edgeSet);
    }

    public static void DebugDrawEdges<T>(Map<T> map, int edgeSet) where T : Tile<T> {
        Bitmap bitmap = map.Render();
        foreach(T tile in map.Tiles) {
            if(tile.Edges.Count == 0) continue;
            int minCost = tile.Edges[edgeSet].Min(n => n.Cost);
            foreach(Edge<T> edge in tile.Edges[edgeSet]) {
                if(edge.Cost == minCost && DebugArrows.ContainsKey(edge.Action)) {
                    int[] positions = DebugArrows[edge.Action];
                    for(int i = 0; i < positions.Length; i += 2) {
                        bitmap.SetPixel(tile.X * 16 + positions[i], tile.Y * 16 + positions[i + 1], 0xff, 0x00, 0x00);
                    }
                }
            }
        }

        bitmap.Save("debug_edges.png");
    }

    public static void DebugFindPath<T>(Map<T> map, int stepCost, T start, PermissionSet permissions, params T[] destinations) where T : Tile<T> {
        List<Action> path = FindPath(map, stepCost, start, permissions, destinations);

        Bitmap bitmap = map.Render();
        T current = start;
        foreach(Action action in path) {
            int[] positions = DebugArrows[action];
            for(int i = 0; i < positions.Length; i += 2) {
                bitmap.SetPixel(current.X * 16 + positions[i], current.Y * 16 + positions[i + 1], 0xff, 0x00, 0x00);
            }
            current = current.Neighbor(action);
        }

        bitmap.Save("debug_find_path.png");
    }
}