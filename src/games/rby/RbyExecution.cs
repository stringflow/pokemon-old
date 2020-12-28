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
}