using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class IGTState {

    public bool Success;
    public byte[] State;
    public int HRA;
    public int HRS;
    public int Divider;
}

public class IGTResults {

    public IGTState[] IGTs;

    public IGTResults(int num) {
        IGTs = new IGTState[num];
    }

    public IGTState this[int index] {
        get { return IGTs[index]; }
        set { IGTs[index] = value; }
    }

    public int NumIGTFrames {
        get { return IGTs.Length; }
    }

    public int TotalSuccesses {
        get { return IGTs.Where(x => x != null && x.Success).Count(); }
    }

    public int TotalFailures {
        get { return IGTs.Where(x => x != null && !x.Success).Count(); }
    }

    public int MostCommonHRA {
        get { return IGTs.Where(x => x != null).GroupBy(x => x.HRA).OrderByDescending(g => g.Count()).First().Key; }
    }

    public int MostCommonHRS {
        get { return IGTs.Where(x => x != null).GroupBy(x => x.HRS).OrderByDescending(g => g.Count()).First().Key; }
    }

    public int MostCommonDivider {
        get { return IGTs.Where(x => x != null).GroupBy(x => x.Divider).OrderByDescending(g => g.Count()).First().Key; }
    }

    public byte[] FirstState {
        get { return IGTs.Where(x => x != null).First().State; }
    }

    public byte[][] States {
        get { return IGTs.Select(x => x.State).ToArray(); }
    }

    // Returns the number of frames that fall into the most commonly hit RNG band.
    public int RNGSuccesses(int range) {
        List<KeyValuePair<(int, int), int>> bands = RNGBands(range).ToList();
        bands.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
        return bands.First().Value;
    }

    // TODO: Use rDiv too???
    public Dictionary<(int, int), int> RNGBands(int range) {
        Dictionary<(int, int), int> ret = new Dictionary<(int, int), int>();
        foreach(IGTState igt in IGTs) {
            if(igt.Success) {
                bool foundBand = false;
                for(int j = 0; j < ret.Count; j++) {
                    (int hra, int hrs) key = ret.ElementAt(j).Key;
                    if(MathHelper.RangeTest(key.hra, igt.HRA, range) && MathHelper.RangeTest(key.hrs, igt.HRS, range)) {
                        ret[key]++;
                        foundBand = true;
                        break;
                    }
                }

                if(!foundBand) {
                    ret[(igt.HRA, igt.HRS)] = 1;
                }
            }
        }

        return ret;
    }
}

public partial class GameBoy {

    public int OverworldLoopAddress;

    public IGTState IGTCheckFrame(byte[] state, Func<GameBoy, bool> fn) {
        IGTState ret = new IGTState();

        if(state != null) {
            LoadState(state);
            ret.Success = fn(this);
            if(ret.Success) ret.State = SaveState();
        }

        ret.HRA = CpuRead("hRandomAdd");
        ret.HRS = CpuRead("hRandomSub");
        ret.Divider = DividerState;
        return ret;
    }

    public IGTResults IGTCheck(byte[][] states, Func<GameBoy, bool> fn, int ss = 0, int ssOverwrite = -1) {
        IGTResults results = new IGTResults(states.Length);
        int successes = ssOverwrite > 0 ? ssOverwrite : states.Length;
        for(int i = 0; i < states.Length && successes >= ss; i++) {
            results[i] = IGTCheckFrame(states[i], fn);
            if(!results[i].Success) {
                successes--;
            }
        }
        return results;
    }

    public static IGTResults IGTCheckParallel<Gb>(Gb[] gbs, byte[][] states, Func<GameBoy, bool> fn, int ss = 0, int ssOverwrite = -1) where Gb : GameBoy {
        IGTResults results = new IGTResults(states.Length);
        int successes = ssOverwrite > 0 ? ssOverwrite : states.Length;
        MultiThread.For(states.Length, gbs, (gb, i) => {
            if(successes < ss) return;
            results[i] = gb.IGTCheckFrame(states[i], fn);
            if(!results[i].Success) {
                Interlocked.Decrement(ref successes);
            }
        });

        return results;
    }

    public static IGTResults IGTCheckParallel<Gb>(int numThreads, byte[][] states, Func<GameBoy, bool> fn, int ss = 0, int ssOverwrite = -1) where Gb : GameBoy {
        return IGTCheckParallel(MultiThread.MakeThreads<Gb>(numThreads), states, fn, ss, ssOverwrite);
    }
}