using System;

public enum Action {

    None = 0,
    A = 0x1,
    StartB = 0x2,
    PokedexFlash = 0x4,
    Select = 0x8,
    Right = 0x10,
    Left = 0x20,
    Up = 0x40,
    Down = 0x80,
}

public static class ActionFunctions {

    public static BiDictionary<Action, string> Actions = new BiDictionary<Action, string>();

    static ActionFunctions() {
        Actions[Action.None] = "";
        Actions[Action.A] = "A";
        Actions[Action.StartB] = "S_B";
        Actions[Action.PokedexFlash] = "S_A_B_S";
        Actions[Action.Select] = "SEL";
        Actions[Action.Right] = "R";
        Actions[Action.Left] = "L";
        Actions[Action.Up] = "U";
        Actions[Action.Down] = "D";
        Actions[Action.Right | Action.A] = "A+R";
        Actions[Action.Left | Action.A] = "A+L";
        Actions[Action.Up | Action.A] = "A+U";
        Actions[Action.Down | Action.A] = "A+D";
    }

    public static string LogString(this Action action) {
        return Actions[action];
    }

    public static Action ToAction(this string action) {
        return Actions[action];
    }

    public static Action Opposite(this Action action) {
        switch(action & ~Action.A) {
            case Action.Right: return Action.Left;
            case Action.Left: return Action.Right;
            case Action.Up: return Action.Down;
            case Action.Down: return Action.Up;
            default: return action;
        }
    }

    public static Action[] PathToActions(string path) {
        return Array.ConvertAll(path.Split(" "), e => e.ToAction());
    }

    public static string ActionsToPath(Action[] actions) {
        return string.Join(" ", Array.ConvertAll(actions, e => e.LogString()));
    }

    public static Action FromSpriteDirection(byte dir) {
        switch(dir) {
            case 0x0: return Action.Down;
            case 0x4: return Action.Up;
            case 0x8: return Action.Left;
            case 0xc: return Action.Right;
            default: return Action.None;
        }
    }
}