public class Crystal : Gsc {

    public Crystal(bool speedup = false, string rom = "roms/pokecrystal.gbc") : base(rom, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}