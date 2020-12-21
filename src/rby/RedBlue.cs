public class RedBlue : Rby {

    public RedBlue(string rom, bool speedup = false) : base(rom, speedup ? SpeedupFlags.All : SpeedupFlags.None) { }
}

public class Red : RedBlue {

    public Red(bool speedup = false, string rom = "roms/pokered.gbc") : base(rom, speedup) { }
}

public class Blue : RedBlue {

    public Blue(bool speedup = false, string rom = "roms/pokeblue.gbc") : base(rom, speedup) { }
}