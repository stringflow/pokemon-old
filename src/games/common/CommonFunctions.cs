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

    public void ClearText(Joypad holdInput = Joypad.None) {
        ClearText(holdInput, int.MaxValue);
    }

    public void ClearText(int numTextBoxes) {
        ClearText(Joypad.None, numTextBoxes);
    }

    public virtual void ClearText(Joypad holdInput, int numTextBoxes) {
        throw new NotImplementedException();
    }

    public virtual int WalkTo(int x, int y) {
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

    public (Joypad Input, int Amount) CalcScroll(int target, int current, int max, bool wrapping) {
        if((CpuRead(0xfff6 + (this is Yellow ? 4 : 0)) & 0x02) > 0) {
            // The move selection is its own thing for some reason, so the input values are wrong have to be adjusted.
            current--;
            max = CpuRead("wNumMovesMinusOne");
            wrapping = true;
        }

        // The input is either Up or Down depending on whether the 'target' slot is above or below the 'current' slot.
        Joypad scrollInput = target < current ? Joypad.Up : Joypad.Down;
        // The number of inputs needed is the distance between the 'current' slot and the 'target' slot.
        int amount = Math.Abs(current - target);

        // If the menu wraps around, the number of inputs should never exceed half of the menus size.
        if(wrapping && amount > max / 2) {
            // If it does exceed, going the other way is fewer inputs.
            amount = max - amount + 1;
            scrollInput ^= (Joypad) 0xc0; // Switch to the other button. This is achieved by XORing the value by 0xc0.
                                          // (Joypad.Down) 01000000 xor 11000000 = 10000000 (Joypad.Up)
                                          // (Joypad.Up)   10000000 xor 11000000 = 01000000 (Joypad.Down)
        }

        if(amount == 0) {
            scrollInput = Joypad.None;
        }

        return (scrollInput, amount);
    }
}