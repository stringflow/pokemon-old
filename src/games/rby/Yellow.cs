public class Yellow : Rby {

    public Yellow(bool speedup = false) : base("roms/pokeyellow.gbc", speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
}
