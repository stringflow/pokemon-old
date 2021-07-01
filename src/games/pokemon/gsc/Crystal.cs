public class Crystal : Gsc {

    public Crystal(string savFile = null, bool speedup = true) : base("roms/pokecrystal.gbc", savFile, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}