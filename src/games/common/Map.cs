using System;
using System.Collections.Generic;

public abstract class Map<T> where T : Tile<T> {

    public byte Width;
    public byte Height;

    public T[,] Tiles;

    public T this[int x, int y] {
        get {
            if(x < 0 || y < 0 || x >= Width * 2 || y >= Height * 2) return null;
            return Tiles[x, y];
        }
    }

    public abstract Bitmap Render();
}

public abstract class Tile<T> where T : Tile<T> {

    public byte X;
    public byte Y;
    public byte Collision;
    public Dictionary<int, List<Edge<T>>> Edges = new Dictionary<int, List<Edge<T>>>();

    public abstract bool IsPassable(PermissionSet permissions);
    public abstract T Right();
    public abstract T Left();
    public abstract T Up();
    public abstract T Down();

    // Returns whether the tile is a valid ledge hop.
    // The tile the function is called on is the tile that the player is standing on before the ledge hop.
    // 'source' is the ledge tile.
    // 'action' is the action required to hop the ledge.
    public abstract bool IsLedgeHop(T ledgeTile, Action action);
    public abstract int LedgeCost();

    public T Neighbor(Action action) {
        switch(action) {
            case Action.Right: return Right();
            case Action.Left: return Left();
            case Action.Up: return Up();
            case Action.Down: return Down();
            default: return null;
        }
    }

    public T[] Neighbors() {
        return new T[] { Right(), Left(), Up(), Down() };
    }

    public T Destination(Action action) {
        T neighbor = Neighbor(action);
        if(IsLedgeHop(neighbor, action)) neighbor = neighbor.Neighbor(action);
        return neighbor;
    }

    public T[] Destinations() {
        return new T[] { Destination(Action.Right), Destination(Action.Left), Destination(Action.Up), Destination(Action.Down) };
    }

    public Action ActionRequired(T tileToReach) {
        if(tileToReach.X > X) return Action.Right;
        else if(tileToReach.X < X) return Action.Left;
        else if(tileToReach.Y > Y) return Action.Down;
        else if(tileToReach.Y < Y) return Action.Up;
        else return Action.None;
    }

    public void AddEdge(int edgeSet, Edge<T> edge) {
        if(!Edges.ContainsKey(edgeSet)) {
            Edges[edgeSet] = new List<Edge<T>>();
        }
        Edges[edgeSet].Add(edge);
        Edges[edgeSet].Sort();
    }

    public Edge<T> GetEdge(int edgeSet, Action action) {
        if(!Edges.ContainsKey(edgeSet)) {
            return null;
        }

        foreach(Edge<T> edge in Edges[edgeSet]) {
            if(edge.Action == action) {
                return edge;
            }
        }

        return null;
    }

    public bool ContainsEdge(int edgeSet, Action action) {
        return GetEdge(edgeSet, action) != null;
    }

    public void RemoveEdge(int edgeSet, Action action) {
        Edge<T> edge = GetEdge(edgeSet, action);
        if(edge == null) {
            return;
        }

        Edges[edgeSet].Remove(edge);
        Edges[edgeSet].Sort();
    }

    public override string ToString() {
        return X + "," + Y;
    }
}

public class Edge<T> : IComparable<Edge<T>> where T : Tile<T> {

    public Action Action;
    public T NextTile;
    public int Cost;
    public int NextEdgeset;

    public int CompareTo(Edge<T> other) {
        return Cost - other.Cost;
    }
}

public class PermissionSet : List<byte> {

    public bool IsAllowed(byte collision) {
        return Contains(collision);
    }
}