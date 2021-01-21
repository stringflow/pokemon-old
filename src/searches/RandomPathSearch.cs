using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

public class RandomSearchParameters<T> where T : Tile<T> {

    public List<byte[][]> StateList;
    public int ClusterSize;
    public int SS;
    public int NumPathsToFind;
    public int StartEdgeSet;
    public T StartTile;
    public T[] EndTiles;
    public Func<GameBoy, Action[], bool> ExecutionCallback;
    public Action<int, Action[], int> FoundCallback = null;
}

public class RandomPathResult {

    public int StatesIndex;
    public Action[] Actions;
    public IGTResults[] Results;
}

// TODO: 
//   - Figure out how to implement >0 cost edges without them being stacked at the beginning
//   - Implement an encounter search? currently it's no encounter only

public static class RandomPathSearch {

    public static ConcurrentBag<RandomPathResult> StartSearch<Gb, T>(int numThreads, RandomSearchParameters<T> parameters) where Gb : GameBoy
                                                                                                                           where T : Tile<T> {
        return StartSearch(MultiThread.MakeThreads<Gb>(numThreads), parameters);
    }

    public static ConcurrentBag<RandomPathResult> StartSearch<Gb, T>(Gb[] gbs, RandomSearchParameters<T> parameters) where Gb : GameBoy
                                                                                                                     where T : Tile<T> {
        ConcurrentBag<RandomPathResult> ret = new ConcurrentBag<RandomPathResult>();

        int pathsFound = 0;
        bool[] threadsRunning = new bool[gbs.Length];

        for(int i = 0; i < gbs.Length; i++) {
            Thread t = new Thread(idx => {
                int threadIndex = (int) idx;
                threadsRunning[threadIndex] = true;
                ParallelSearch(ret, gbs[threadIndex], parameters, ref pathsFound);
                threadsRunning[threadIndex] = false;
            });
            t.Start(i);
        }

        while(!threadsRunning.All(b => !b)) {
            Thread.Sleep(10);
        }

        return ret;
    }

    private static void ParallelSearch<Gb, T>(ConcurrentBag<RandomPathResult> list, Gb gb, RandomSearchParameters<T> parameters, ref int pathsFound) where Gb : GameBoy
                                                                                                                                                     where T : Tile<T> {
        Random random = new Random();
        int igtFrames = parameters.StateList[0].Length;
        IGTResults[] results = new IGTResults[parameters.ClusterSize];
        int statesIndex;
        int successes;
        while(pathsFound < parameters.NumPathsToFind) {
            Action[] actions = GenerateRandomPath(random, parameters.StartEdgeSet, parameters.StartTile, parameters.EndTiles).ToArray();
            statesIndex = random.Next(parameters.StateList.Count - parameters.ClusterSize + 1);
            successes = igtFrames * parameters.ClusterSize;

            for(int i = 0; i < parameters.ClusterSize && successes >= parameters.SS; i++) {
                results[i] = gb.IGTCheck(parameters.StateList[statesIndex + i], gb => parameters.ExecutionCallback(gb, actions), parameters.SS, successes);
                successes -= results[i].TotalFailures;
            }

            if(successes >= parameters.SS) {
                if(parameters.FoundCallback != null) parameters.FoundCallback(statesIndex, actions, successes);
                if(parameters.NumPathsToFind > 0) {
                    Interlocked.Increment(ref pathsFound);
                    list.Add(new RandomPathResult {
                        StatesIndex = statesIndex,
                        Actions = actions,
                        Results = results,
                    });
                }
            }
        }
    }

    public static List<Action> GenerateRandomPath<T>(Random random, int edgeSet, T startTile, params T[] endTiles) where T : Tile<T> {
        List<Action> path = new List<Action>();
        bool canA = false;
        T current = startTile;
        while(Array.IndexOf(endTiles, current) == -1) {
            Edge<T>[] edges = current.Edges[edgeSet].Where(e => e.Cost == 0 && (canA || (e.Action & Action.A) == 0)).ToArray();
            Edge<T> edge = edges[random.Next(edges.Length)];
            path.Add(edge.Action);
            current = edge.NextTile;
            edgeSet = edge.NextEdgeset;

            canA = (edge.Action & Action.A) == 0;
        }

        return path;
    }
}