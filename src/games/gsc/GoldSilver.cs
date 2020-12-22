public class GoldSilver : Gsc {

    public GoldSilver(string rom, bool speedup = false) : base(rom, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}

public class Gold : GoldSilver {

    public Gold(bool speedup = false, string rom = "roms/pokegold.gbc") : base(rom, speedup) { }
}

public class Silver : GoldSilver {

    public Silver(bool speedup = false, string rom = "roms/pokesilver.gbc") : base(rom, speedup) { }
}