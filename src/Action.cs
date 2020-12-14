public enum Action {

    None = 0,
    A = 0x1,
    StartB = 0x2,
    Right = 0x10,
    Left = 0x20,
    Up = 0x40,
    Down = 0x80,
}

public static class ActionFunctions {

    public static string LogString(this Action action) {
        switch(action) {
            case Action.Right: return "R";
            case Action.Left: return "L";
            case Action.Up: return "U";
            case Action.Down: return "D";
            case Action.Right | Action.A: return "A+R";
            case Action.Left | Action.A: return "A+L";
            case Action.Up | Action.A: return "A+U";
            case Action.Down | Action.A: return "A+D";
            case Action.StartB: return "S_B";
            case Action.A: return "A";
            default: return "?";
        }
    }
}