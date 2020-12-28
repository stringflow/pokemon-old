using System;

public partial class Rby {

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyInput", (byte) joypad);
    }

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            do {
                RunFor(1);
                RunUntil("Joypad");
            } while((CpuRead(SYM["wd730"]) & 0x20) != 0);
            Inject(joypad);
            AdvanceFrame();
        }
    }

    public override int Execute(params Action[] actions) {
        int ret = 0;

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    Joypad joypad = (Joypad) action;
                    do {
                        RunUntil("JoypadOverworld");
                        Inject(joypad);
                        ret = Hold(joypad, SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["TryDoWildEncounter.CanEncounter"] + 6, SYM["OverworldLoopLessDelay.newBattle"] + 3);
                        if(ret == SYM["TryDoWildEncounter.CanEncounter"] + 6) {
                            return RunUntil("CalcStats");
                        } else if(ret == SYM["CollisionCheckOnLand.collision"] || ret == SYM["CollisionCheckOnWater.collision"]) {
                            return ret;
                        }

                        ret = SYM["JoypadOverworld"];
                        RunUntil(SYM["JoypadOverworld"], SYM["EnterMap"] + 0x10);
                    } while((CpuRead("wd736") & 0x40) != 0);
                    break;
                case Action.A:
                    Inject(Joypad.A);
                    AdvanceFrame(Joypad.A);
                    ret = Hold(Joypad.A, "JoypadOverworld", "PrintLetterDelay");
                    break;
                case Action.StartB:
                    Press(Joypad.Start, Joypad.B);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.PokedexFlash:
                    Press(Joypad.Start, Joypad.A, Joypad.B, Joypad.Start);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.Delay:
                    Inject(Joypad.None);
                    RunUntil("OverworldLoop");
                    ret = RunUntil("JoypadOverworld");
                    break;
                default:
                    Debug.Assert(false, "Unknown Action: {0}", action);
                    break;
            }
        }

        return ret;
    }

    public void PickupItem() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["PlaySound"]);
        RunUntil("JoypadOverworld");
    }

    public void ClearText() {
        Joypad joypad = Joypad.A;
        int ret;
        do {
            ret = RunUntil("ManualTextScroll", "HandleMenuInput", "PrintStatsBox.PrintStats", "TextCommand_PAUSE", "DisableLCD");
            joypad = joypad.Opposite();

            if(ret == SYM["ManualTextScroll"] || ret == SYM["PrintStatsBox.PrintStats"]) {
                Inject(joypad);
                AdvanceFrame(joypad);
            } else if(ret == SYM["TextCommand_PAUSE"]) {
                Inject(joypad);
                Hold(joypad, "Joypad");
            } else if (ret == SYM["DisableLCD"]) {
                // find a better breakpoint for when a battle is over?
                return;
            } else {
                RunUntil("Joypad");
            }
        } while(ret != SYM["HandleMenuInput"]);
    }

    public void UseMove(int slot, int numMoves) {
        OpenFightMenu();
        int currentSlot = CpuRead("wCurrentMenuItem") - 1;
        int difference = currentSlot - slot;
        int numSlots = difference == 0 ? 0 : slot % 2 == currentSlot % 2 ? (int)(numMoves/2) : 1;
        Joypad joypad = ((Math.Abs(difference * numMoves) + difference % numMoves) & 2) != 0 ? Joypad.Down : Joypad.Up;
        if(numSlots == 0) {
            Press(Joypad.None);
        } else if(numSlots == 1) {
            Press(joypad);
        } else if(numSlots == 2) {
            Press(joypad, Joypad.None, joypad);
        }
        Press(Joypad.A);
    }

    public void UseMove1(int numMoves = 4) {
        UseMove(0, numMoves);
    }

    public void UseMove2(int numMoves = 4) {
        UseMove(1, numMoves);
    }

    public void UseMove3(int numMoves = 4) {
        UseMove(2, numMoves);
    }

    public void UseMove4(int numMoves = 4) {
        UseMove(3, numMoves);
    }

    public void UseItem(string item) {
        BagScroll(item);
        Press(Joypad.A);
    }

    public void UseXItem(string item) {
        UseItem(item);
        RunUntil("DoneText");
        Inject(Joypad.B);
        AdvanceFrame(Joypad.B);
    }

    public void UseXAttack() {
        UseXItem("X ATTACK");
    }

    public void UseXDefense() {
        UseXItem("X DEFENSE");
    }

    public void UseXSpeed() {
        UseXItem("X SPEED");
    }

    public void UseXSpecial() {
        UseXItem("X SPECIAL");
    }

    public void UseXAccuracy() {
        UseXItem("X ACCURACY");
    }

    public void UseHealingItem(string item) {
        UseItem(item);
        Press(Joypad.None, Joypad.A, Joypad.B);
    }

    public void UsePokeFlute() {
        BagScroll("POKE FLUTE");
        Press(Joypad.A);
    }

    public void OpenFightMenu() {
        if(CpuRead("wCurrentMenuItem") == 1) Press(Joypad.Up);
        Press(Joypad.A);
        RunUntil("Joypad");
    }

    public void OpenItemBag() {
        if(CpuRead("wCurrentMenuItem") == 0) Press(Joypad.Down);
        Press(Joypad.A);
    }

    public void BagScroll(string item) {
        BagScroll(Bag.IndexOf(item));
    }

    public void BagScroll(RbyItem item) {
        BagScroll(Bag.IndexOf(item));
    }

    public void BagScroll(int targetSlot) {
        OpenItemBag();
        RunUntil("DisplayListMenuID");
        int currentSlot = CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset");
        int difference = targetSlot - currentSlot;
        int numSlots = Math.Abs(difference);

        if(difference == 0) {
            Press(Joypad.None);
        } else if(difference == -2) {
            Press(Joypad.Up, Joypad.Up | Joypad.Left);
        } else if (difference == 2) {
            Press(Joypad.Down, Joypad.Down | Joypad.Left);
        } else {
            Joypad direction = difference < 0 ? Joypad.Up : Joypad.Down;
            Joypad secondary = Joypad.Left;
            Press(direction);
            for(int i = 1; i < numSlots; i++) {
                Press(direction | secondary);
                secondary = secondary.Opposite();
            }
        }
    }
}
