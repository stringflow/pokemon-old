public class RbyTurn {

    public string Move;
    public int Flags;

    public RbyTurn(string move, int flags = 0) {
        Move = move;
        Flags = flags;

        if((Flags & 0x3f) == 0) {
            Flags |= 39;
        }
    }
}