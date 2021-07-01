public class RedBlueForce : RbyForce {

    public RedBlueForce(string rom, bool speedup = true) : base(rom, speedup ? SpeedupFlags.All : SpeedupFlags.None) {
    }
}