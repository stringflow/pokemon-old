using System.IO;

public static class YellowMoonBackup {

    public static void Start() {
        Yellow[] gbs = MultiThread.MakeThreads<Yellow>(8, "baseSaves/yellow_moon_backup.sav");
        Yellow gb = gbs[0];
        gb.Show();
        new RbyIntroSequence().Execute(gb);

        RbyMap map1 = gb.Maps[60];
        RbyMap map2 = gb.Maps[61];

        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, map2[12, 9], Action.Up | Action.Down | Action.Left | Action.Right | Action.A);

        map1[21, 16].GetEdge(0, Action.Down).NextTile = map2[21, 17];
        map1[20, 17].GetEdge(0, Action.Right).NextTile = map2[21, 17];

        for(int x = 0xd; x <= 0x12; x++) {
            for(int y = 0x10; y <= 0x11; y++) {
                map1[x, y].RemoveEdge(0, Action.A);
                map1[x, y].RemoveEdge(0, Action.Down);
            }
        }

        for(int x = 0xd; x <= 0x12; x++) {
            for(int y = 0x10; y <= 0x11; y++) {
                map1[x, y].RemoveEdge(0, Action.A);
                map1[x, y].RemoveEdge(0, Action.Down);
            }
        }

        for(int x = 0x11; x <= 0x17; x++) {
            map2[x, 0x1f].RemoveEdge(0, Action.A);
        }

        IGTResults initialState = Yellow.IGTCheckParallel(gbs, new RbyIntroSequence(), 60);

        StreamWriter writer = new StreamWriter("moon.txt");
        writer.AutoFlush = true;

        DFParameters<Yellow, RbyMap, RbyTile> parameters = new DFParameters<Yellow, RbyMap, RbyTile>() {
            PruneAlreadySeenStates = true,
            MaxCost = 20,
            NoEncounterSS = 60,
            RNGSS = 56,
            EndTiles = new RbyTile[] { map2[12, 9], },
            FoundCallback = state => writer.WriteLine(state.Log),
            EndEdgeSet = 0,
        };

        DepthFirstSearch.StartSearch(gbs, parameters, gb.Tile, 0, initialState);
    }
}