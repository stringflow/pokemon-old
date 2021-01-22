using System;
using System.Linq;
using System.Collections.Generic;

public class DFParameters<Gb, T> where Gb : GameBoy
                                 where T : Tile<T> {

    public bool PruneAlreadySeenStates = true;
    public int MaxCost = 0;
    public int NoEncounterSS = 1;
    public int EncounterSS = 0;
    public string LogStart = "";
    public T[] EndTiles = null;
    public int EndEdgeSet = 0;
    public Func<Gb, bool> EncounterCallback = null;
    public Action<DFState<T>> FoundCallback = null;
}

public class DFState<T> where T : Tile<T> {

    public T Tile;
    public int EdgeSet;
    public int WastedFrames;
    public Action BlockedActions;
    public IGTResults IGT;
    public string Log;

    public override int GetHashCode() {
        unchecked {
            const int prime = 92821;
            int hash = prime + Tile.X;
            hash = hash * prime + Tile.Y;
            hash = hash * prime + Tile.Collision;
            hash = hash * prime + IGT.MostCommonHRA;
            hash = hash * prime + IGT.MostCommonHRS;
            hash = hash * prime + IGT.MostCommonDivider;
            return hash;
        }
    }
}

public static class DepthFirstSearch {

    public static void StartSearch<Gb, T>(Gb[] gbs, DFParameters<Gb, T> parameters, T startTile, int startEdgeSet, byte[][] states) where Gb : GameBoy
                                                                                                                                    where T : Tile<T> {
        IGTResults initialState = new IGTResults(states.Length);
        for(int i = 0; i < states.Length; i++) {
            initialState[i] = new IGTState();
            initialState[i].State = states[i];
            initialState[i].HRA = -1;
            initialState[i].HRS = -1;
            initialState[i].Divider = -1;
        }

        RecursiveSearch(gbs, parameters, new DFState<T> {
            Tile = startTile,
            EdgeSet = startEdgeSet,
            WastedFrames = 0,
            Log = parameters.LogStart,
            BlockedActions = Action.A,
            IGT = initialState,
        }, new HashSet<int>());
    }

    private static void RecursiveSearch<Gb, T>(Gb[] gbs, DFParameters<Gb, T> parameters, DFState<T> state, HashSet<int> seenStates) where Gb : GameBoy
                                                                                                                                    where T : Tile<T> {
        if(parameters.EndTiles != null && state.EdgeSet == parameters.EndEdgeSet && parameters.EndTiles.Any(t => t.X == state.Tile.X && t.Y == state.Tile.Y)) {
            if(parameters.FoundCallback != null) parameters.FoundCallback(state);
            else Console.WriteLine(state.Log);
        }

        if(parameters.PruneAlreadySeenStates && !seenStates.Add(state.GetHashCode())) {
            return;
        }

        byte[][] states = state.IGT.States;

        foreach(Edge<T> edge in state.Tile.Edges[state.EdgeSet]) {
            if(state.WastedFrames + edge.Cost > parameters.MaxCost) continue;
            if((state.BlockedActions & edge.Action) > 0) continue;

            IGTResults results = GameBoy.IGTCheckParallel<Gb>(gbs, states, gb => gb.Execute(edge.Action) == gb.OverworldLoopAddress, parameters.EncounterCallback == null ? parameters.NoEncounterSS : 0);

            DFState<T> newState = new DFState<T>() {
                Tile = edge.NextTile,
                EdgeSet = edge.NextEdgeset,
                Log = state.Log + edge.Action.LogString() + " ",
                IGT = results,
                WastedFrames = state.WastedFrames + edge.Cost,
            };

            int noEncounterSuccesses = results.TotalSuccesses;
            if(parameters.EncounterCallback != null) {
                int encounterSuccesses = results.TotalFailures;
                for(int i = 0; i < results.NumIGTFrames && encounterSuccesses >= parameters.EncounterSS; i++) {
                    gbs[0].LoadState(results.States[i]);
                    if(parameters.EncounterCallback(gbs[0])) encounterSuccesses++;
                }

                if(encounterSuccesses >= parameters.EncounterSS) {
                    if(parameters.FoundCallback != null) parameters.FoundCallback(newState);
                    else Console.WriteLine(state.Log);
                }
            }

            if(noEncounterSuccesses >= parameters.NoEncounterSS) {
                Action blockedActions = state.BlockedActions;

                if(edge.Action == Action.A) blockedActions |= Action.StartB;
                if((edge.Action & Action.A) > 0) blockedActions |= Action.A;
                else blockedActions &= ~(Action.A | Action.StartB);

                newState.BlockedActions = blockedActions;
                RecursiveSearch(gbs, parameters, newState, seenStates);
            }
        }
    }
}