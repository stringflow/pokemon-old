using System;
using System.Collections.Generic;

public abstract class Map<M, T> where M : Map<M, T>
                                where T : Tile<M, T> {

    public int Id;
    public byte Width;
    public byte Height;

    public T[,] Tiles;
    public Connection<M, T>[] Connections;

    public T this[int x, int y] {
        get {
            if(x < 0 || y < 0 || x >= Width * 2 || y >= Height * 2) return null;
            return Tiles[x, y];
        }
    }

    public abstract Bitmap Render();
    public abstract bool AllowsBiking();
}

public abstract class Connection<M, T> where M : Map<M, T>
                                       where T : Tile<M, T> {

    public ushort Source;
    public ushort Destination;
    public byte Length;
    public byte Width;
    public byte YAlignment;
    public byte XAlignment;
    public ushort Window;

    public abstract M GetDestinationMap();
}

public abstract class Tile<M, T> where M : Map<M, T>
                                 where T : Tile<M, T> {

    public byte X;
    public byte Y;
    public byte Collision;
    public M Map;

    public Dictionary<int, List<Edge<M, T>>> Edges = new Dictionary<int, List<Edge<M, T>>>();

    public T GetNeighbor(Action action) {
        Connection<M, T> connection = null;
        if(action == Action.Right && X == Map.Width * 2 - 1) connection = Map.Connections[0];
        if(action == Action.Left && X == 0) connection = Map.Connections[1];
        if(action == Action.Down && Y == Map.Height * 2 - 1) connection = Map.Connections[2];
        if(action == Action.Up && Y == 0) connection = Map.Connections[3];

        int xd;
        int yd;
        if(connection != null) {
            if(action == Action.Down || action == Action.Up) {
                xd = (X + connection.XAlignment) & 0xff;
                yd = connection.YAlignment;
            } else {
                xd = connection.XAlignment;
                yd = (Y + connection.YAlignment) & 0xff;
            }

            return connection.GetDestinationMap()[xd, yd];
        } else {
            xd = X;
            yd = Y;
            switch(action) {
                case Action.Right: xd++; break;
                case Action.Left: xd--; break;
                case Action.Down: yd++; break;
                case Action.Up: yd--; break;
            }
            return Map[xd, yd];
        }
    }


    public abstract int CalcStepCost(bool onBike, bool ledgeHop, bool warp, Action action);
    public abstract bool LedgeCheck(T ledgeTile, Action action);
    public abstract (T TileToWarpTo, Action ActionRequired) WarpCheck();
    public abstract bool IsDoorTile();
    public abstract bool CollisionCheckLand(PokemonGame gb, T dest, byte[] overworldMap, Action action, bool allowTrainerVision);
    public abstract bool CollisionCheckWater(PokemonGame gb, T dest, byte[] overworldMap, Action action, bool allowTrainerVision);

    public T DoorCheck() {
        return IsDoorTile() ? GetNeighbor(Action.Down) : (T) this;
    }

    public void AddEdge(int edgeSet, Edge<M, T> edge) {
        if(!Edges.ContainsKey(edgeSet)) {
            Edges[edgeSet] = new List<Edge<M, T>>();
        }
        Edges[edgeSet].Add(edge);
        Edges[edgeSet].Sort();
    }

    public Edge<M, T> GetEdge(int edgeSet, Action action) {
        if(!Edges.ContainsKey(edgeSet)) {
            return null;
        }

        foreach(Edge<M, T> edge in Edges[edgeSet]) {
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
        Edge<M, T> edge = GetEdge(edgeSet, action);
        if(edge == null) {
            return;
        }

        Edges[edgeSet].Remove(edge);
        Edges[edgeSet].Sort();
    }

    public override string ToString() {
        return Map.Id + "#" + X + "/" + Y;
    }
}

public class Edge<M, T> : IComparable<Edge<M, T>> where M : Map<M, T>
                                                  where T : Tile<M, T> {

    public Action Action;
    public T NextTile;
    public int Cost;
    public int NextEdgeset;

    public int CompareTo(Edge<M, T> other) {
        return Cost - other.Cost;
    }
}

public class PermissionSet : List<byte> {

    public bool IsAllowed(byte collision) {
        return Contains(collision);
    }
}