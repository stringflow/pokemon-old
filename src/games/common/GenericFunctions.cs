using System;

// TODO: FIND A BETTER FILE NAME LOL
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

    // Executes the specified button presses while respecting consecutive input lag.
    public void MenuPress(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            if(CpuRead("hJoyLast") == (byte) joypad) {
                Press(Joypad.None);
            }
            Press(joypad);
        }
    }

    public virtual void ClearText(bool holdDuringText, params Joypad[] joypads) {
        throw new NotImplementedException();
    }

    public virtual int WalkTo(int x, int y) {
        throw new NotImplementedException();
    }
}