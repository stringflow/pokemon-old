using System;
using System.IO;

public partial class PokemonGame : GameBoy {

    public PokemonGame(string rom, string savFile = null, SpeedupFlags speedupFlags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, savFile, speedupFlags) {
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

    public virtual byte[] ReadCollisionMap() {
        throw new NotImplementedException();
    }

    public virtual bool Surfing() {
        throw new NotImplementedException();
    }

    public virtual bool Biking() {
        throw new NotImplementedException();
    }

    public (Joypad Direction, int Amount) CalcScroll(int current, int target, int max, bool wrapping) {
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

    public bool SaveLoaded() {
        return CpuRead("wPlayerName") != 0x00;
    }
}