public class Yellow : Rby {

    public Yellow(bool speedup = false, string rom = "roms/pokeyellow.gbc") : base(rom, speedup ? SpeedupFlags.All : SpeedupFlags.None) { }

    public override void Inject(Joypad joypad) {
        CpuWrite(0xFFF5, (byte) joypad);
    }
}
