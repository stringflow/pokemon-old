using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class DFParameters<Gb, M, T> where Gb : PokemonGame
                                    where M : Map<M, T>
                                    where T : Tile<M, T> {

    public bool PruneAlreadySeenStates = true;
    public int MaxCost = 0;
    public int NoEncounterSS = 1;
    public int RNGSS = -1;
    public string LogStart = "";
    public T[] EndTiles = null;
    public int EndEdgeSet = 0;
    public Action<DFState<M, T>> FoundCallback;
}

public class DFState<M, T> where M : Map<M, T>
                           where T : Tile<M, T> {

    public T Tile;
    public int EdgeSet;
    public int WastedFrames;
    public Action BlockedActions;
    public int APressCounter;
    public IGTResults IGT;
    public string Log;

    public override int GetHashCode() {
        unchecked {
            const int prime = 92821;
            int hash = prime + Tile.Map.Id;
            hash = hash * prime + Tile.X;
            hash = hash * prime + Tile.Y;
            hash = hash * prime + IGT.MostCommonHRA;
            hash = hash * prime + IGT.MostCommonHRS;
            hash = hash * prime + IGT.MostCommonDivider;
            return hash;
        }
    }
}

public static class DepthFirstSearch {

    public static void StartSearch<Gb, M, T>(Gb[] gbs, DFParameters<Gb, M, T> parameters, T startTile, int startEdgeSet, IGTResults initialState) where Gb : PokemonGame
                                                                                                                                                  where M : Map<M, T>
                                                                                                                                                  where T : Tile<M, T> {
        RecursiveSearch(gbs, parameters, new DFState<M, T> {
            Tile = startTile,
            EdgeSet = startEdgeSet,
            WastedFrames = 0,
            Log = parameters.LogStart,
            APressCounter = 1,
            IGT = initialState,
        }, new HashSet<int>());
    }

    private static void RecursiveSearch<Gb, M, T>(Gb[] gbs, DFParameters<Gb, M, T> parameters, DFState<M, T> state, HashSet<int> seenStates) where Gb : PokemonGame
                                                                                                                                             where M : Map<M, T>
                                                                                                                                             where T : Tile<M, T> {
        if(parameters.EndTiles != null && state.EdgeSet == parameters.EndEdgeSet && parameters.EndTiles.Any(t => t.X == state.Tile.X && t.Y == state.Tile.Y)) {
            if(parameters.FoundCallback != null) {
                parameters.FoundCallback(state);
            }
        }

        if(parameters.PruneAlreadySeenStates && !seenStates.Add(state.GetHashCode())) {
            return;
        }

        foreach(Edge<M, T> edge in state.Tile.Edges[state.EdgeSet]) {
            if(state.WastedFrames + edge.Cost > parameters.MaxCost) continue;
            if((state.BlockedActions & edge.Action) > 0) continue;
            if(edge.Action == Action.A && state.APressCounter > 0) continue;

            IGTResults results = PokemonGame.IGTCheckParallel<Gb>(gbs, state.IGT, gb => gb.Execute(edge.Action) == gb.OverworldLoopAddress, parameters.NoEncounterSS);

            DFState<M, T> newState = new DFState<M, T>() {
                Tile = edge.NextTile,
                EdgeSet = edge.NextEdgeset,
                Log = state.Log + edge.Action.LogString() + " ",
                IGT = results,
                WastedFrames = state.WastedFrames + edge.Cost,
            };

            int noEncounterSuccesses = results.TotalSuccesses;
            if(noEncounterSuccesses >= parameters.NoEncounterSS) {
                int rngSuccesses = results.RNGSuccesses(0x9);
                if(rngSuccesses >= parameters.RNGSS) {
                    newState.APressCounter = edge.Action == Action.A ? 2 : Math.Max(state.APressCounter - 1, 0);

                    Action blockedActions = state.BlockedActions;
                    if((edge.Action & Action.A) > 0) blockedActions |= Action.A;
                    else blockedActions &= ~(Action.A | Action.StartB);
                    newState.BlockedActions = blockedActions;

                    RecursiveSearch(gbs, parameters, newState, seenStates);
                }
            }
        }
    }
}