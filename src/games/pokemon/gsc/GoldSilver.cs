public class GoldSilver : Gsc {

    public GoldSilver(string rom, string savFile = null, bool speedup = true) : base(rom, savFile, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}

public class Gold : GoldSilver {

    public Gold(string savFile = null, bool speedup = true) : base("roms/pokegold.gbc", savFile, speedup) { }
}

public class Silver : GoldSilver {

    public Silver(string savFile = null, bool speedup = true) : base("roms/pokesilver.gbc", savFile, speedup) { }
}