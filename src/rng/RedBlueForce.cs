public class RedBlueForce : RbyForce {

    public RedBlueForce(string rom, bool speedup = false) : base(rom, speedup ? SpeedupFlags.All : SpeedupFlags.None) {
    }
}