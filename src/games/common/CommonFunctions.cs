using System;

public partial class GameBoy {

    // Reads the game's font from the ROM. Each game overrides this function and implements it in its own way.
    public virtual Font ReadFont() {
        return null;
    }

    // Injects an input by overwriting the hardware register.
    // Only useful after the GameBoy polled the joypad status but before the inputs are processed.
    public virtual void Inject(Joypad joypad) {
        throw new NotImplementedException();
    }

    // Wrapper for advancing to joypad polling and injecting an input
    public virtual void Press(params Joypad[] joypads) {
        throw new NotImplementedException();
    }

    // Executes the specified actions and returns the last hit breakpoint.
    public virtual int Execute(params Action[] actions) {
        throw new NotImplementedException();
    }

    // Helper function that executes the specified string path.
    public int Execute(string path) {
        return Execute(Array.ConvertAll(path.Split(" "), e => e.ToAction()));
    }

    public int ClearText(Joypad holdInput = Joypad.None) {
        return ClearText(holdInput, int.MaxValue);
    }

    public int ClearText(int numTextBoxes) {
        return ClearText(Joypad.None, numTextBoxes);
    }

    public int ClearTextUntil(Joypad holdInput, params int[] additionalBreakpoints) {
        return ClearText(holdInput, int.MaxValue, additionalBreakpoints);
    }

    public virtual int ClearText(Joypad holdInput, int numTextBoxes, params int[] additionalBreakpoints) {
        throw new NotImplementedException();
    }

    public virtual int MoveTo(int x, int y, Action preferredDirection = Action.None) {
        throw new NotImplementedException();
    }

    // Executes the specified button presses while respecting consecutive input lag.
    public void MenuPress(Joypad joypad, bool doubleInput = false) {
        Joypad lastInput = (Joypad) CpuRead("hJoyLast");
        if((doubleInput && lastInput == joypad) || (!doubleInput && (lastInput & joypad) > 0)) {
            Press(Joypad.None);
        }
        Press(joypad);
    }
}