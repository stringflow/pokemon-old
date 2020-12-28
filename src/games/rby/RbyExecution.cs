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
            } else if(ret == SYM["DisableLCD"]) {
                // find a better breakpoint for when a battle is over?
                return;
            } else {
                RunUntil("Joypad");
            }
        } while(ret != SYM["HandleMenuInput"]);
    }

    public void UseMove(int slot) {
        OpenFightMenu();
        SelectMenuItem(slot);
        Press(Joypad.A);
    }

    public void UseMove1() {
        UseMove(0);
    }

    public void UseMove2() {
        UseMove(1);
    }

    public void UseMove3() {
        UseMove(2);
    }

    public void UseMove4() {
        UseMove(3);
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
        SelectListItem(Bag.IndexOf(item));
    }

    public void BagScroll(RbyItem item) {
        SelectListItem(Bag.IndexOf(item));
    }

    public void SelectMenuItem(int target) {
        MenuScroll(target, CpuRead("wCurrentMenuItem"), CpuRead("wMaxMenuItem"), CpuRead("wMenuWrappingEnabled") > 0);
    }

    public void SelectListItem(int target) {
        MenuScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);
    }

    private void MenuScroll(int target, int current, int max, bool wrapping) {
        RunUntil("HandleMenuInput_.getJoypadState");
        if((CpuRead(0xfff6 + (this is Yellow ? 4 : 0)) & 0x02) > 0) { // Battle menu
            current--;
            max = CpuRead("wNumMovesMinusOne");
            wrapping = true;
        }
        Joypad input;
        int amount;
        if(!wrapping) {
            input = target < current ? Joypad.Up : Joypad.Down;
            amount = Math.Abs(current - target);
        } else {
            input = target > current ? Joypad.Down : Joypad.Up;
            amount = Math.Abs(current - target);
            if(amount > max / 2) {
                amount = max - amount + 1;
                input ^= (Joypad) 0xc0;
            }
        }

        for(int i = 0; i < amount; i++) {
            MenuPress(input);
        }
    }
}
