public class Yellow : Rby {

    public Yellow(bool speedup = false, string rom = "roms/pokeyellow.gbc") : base(rom, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}
