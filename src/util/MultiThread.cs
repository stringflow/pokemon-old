using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class MultiThread {

    public delegate void MultiThreadRunFn<Gb>(Gb gb, int iterator) where Gb : GameBoy;

    public static void For<Gb>(int count, Gb[] gbs, MultiThreadRunFn<Gb> fn) where Gb : GameBoy {
        Dictionary<Gb, bool> threadsRunning = new Dictionary<Gb, bool>();
        foreach(Gb gb in gbs) {
            threadsRunning[gb] = false;
        }

        object gbSelectionLock = new object();

        Parallel.For(0, count, new ParallelOptions() { MaxDegreeOfParallelism = gbs.Length }, (iterator) => {
            Gb gb = null;
            lock(gbSelectionLock) {
                foreach(Gb gameboy in gbs) {
                    if(!threadsRunning[gameboy]) {
                        gb = gameboy;
                        threadsRunning[gb] = true;
                        break;
                    }
                }
            }

            fn(gb, iterator);
            threadsRunning[gb] = false;
        });
    }

    public static Gb[] MakeThreads<Gb>(int numThreads, string savFile = null) where Gb : GameBoy {
        Gb[] gbs = new Gb[numThreads];
        for(int i = 0; i < numThreads; i++) {
            gbs[i] = (Gb) Activator.CreateInstance(typeof(Gb), args: new object[] { savFile, true });
        }
        return gbs;
    }
}