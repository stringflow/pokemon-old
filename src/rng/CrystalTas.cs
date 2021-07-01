public class CrystalTas : GscForce {

    public CrystalTas() : base("roms/pokecrystal.gbc") {
        Show();

        CacheState("intro", () => {
            GscStrat.GfSkip.Execute(this);
            GscStrat.TitleSkip.Execute(this);
            Press(Joypad.Down, Joypad.A | Joypad.Left, Joypad.B, Joypad.A);
            ClearText();
            ChooseMenuItem(1);
            ClearText();
            SetClock(3, 52);
            ClearText(Joypad.A);
            MenuPress(Joypad.A);
            Nickname();
            ClearText();
            TalkTo(6150, 7, 4, Action.Down);
            Yes();
            ClearText();
            Yes();
            ClearText();
            No();
            ClearText();
            Yes();
            ClearText();
            Yes();
            ClearText();
            MoveTo(6148, 6, 3);
            ClearText();
            Yes();
            ClearText();
            TalkTo(6, 3);
            ForceGiftDVs(0xf6ae);
            ClearText();
            Yes();
            Nickname();
            ClearText();
            MoveTo(4, 8, Action.Down);
            ClearText();
            MoveTo(6657, 17, 5);
            ClearText();
            MoveTo(6657, 17, 6);
            ClearText();
        });

        CacheState("rival1", () => {
            MoveTo(6659, 33, 7);
            ClearText();
            ForceTurn(new GscTurn("LEER"), new GscTurn("LEER"));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("LEER"));
            ForceTurn(new GscTurn("TACKLE"), new GscTurn("LEER"));
            ForceTurn(new GscTurn("TACKLE"));
            ClearText();
        });

        CacheState("interlude", () => {
            MoveTo(6149, 4, 5);
            ClearText();
            Nickname();
            ClearText();
            TalkTo(5, 2, Action.Up);
            ClearText();
            MoveTo(4, 8, Action.Down);
            ClearText();
            MoveTo(6147, 53, 9, Action.Left);
            ClearText();
            No();
            ClearText();
            MoveTo(6657, 12, 38);
        });

        CacheState("r30", () => {
            ForceEncounter(Action.Up, "POLIWAG", 4);
            ClearText();
            ForceYoloball("POKE BALL");
            ClearText();
            No();

            MoveTo(6657, 5, 24);
            ClearText();
            ForceTurn(new GscTurn("TACKLE", Crit));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("TACKLE", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit));

            MoveTo(6658, 26, 17, Action.Up);
            ClearText();
            No();
            ClearText();
        });

        CacheState("falkner", () => {
            MoveTo(2567, 4, 10);
            ClearText();
            ForceTurn(new GscTurn("SMOKESCREEN"), new GscTurn("PECK", 1));
            ForceTurn(new GscTurn("LEER"), new GscTurn("PECK", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("PECK", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("PECK", Miss));
            ForceTurn(new GscTurn("TACKLE"), new GscTurn("PECK", Miss));

            MoveTo(4, 6);
            ClearText();
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("TACKLE", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("TACKLE", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit));

            TalkTo(5, 1);
            ForceTurn(new GscTurn("TACKLE"), new GscTurn("MUD-SLAP", 1));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("MUD-SLAP", 1));
            ForceTurn(new GscTurn("TACKLE", Crit));
            ForceTurn(new GscTurn("LEER"), new GscTurn("GUST", 1));
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("GUST", 1));
            ForceTurn(new GscTurn("TACKLE", Crit));
            ClearText();
        });

        CacheState("unioncave", () => {
            MoveTo(2565, 18, 18);
            ClearText();

            TalkTo(2570, 4, 3);
            Yes();
            ClearText();

            TalkTo(2566, 2, 3);
            Buy("ESCAPE ROPE", 6, "X ATTACK", 2);

            MoveTo(2561, 7, 71);
            ClearText();
            No();
            ClearText();

            TalkTo(2052, 3, 2);
        });

        CacheState("slowpokewell", () => {
            MoveTo(808, 15, 10);
            ClearText();
            ForceTurn(new GscTurn("TACKLE", Crit), new GscTurn("TACKLE", Miss));
            ForceTurn(new GscTurn("TACKLE"));
            ForceTurn(new GscTurn("TACKLE"), new GscTurn("TACKLE", Miss));
            ForceTurn(new GscTurn("TACKLE", Crit));

            MoveTo(14, 4);
            ClearText();
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER"), new GscTurn("WRAP", Miss));
            ForceTurn(new GscTurn("EMBER", Crit));

            MoveTo(7, 6);
            ClearText();
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER", Crit));

            MoveTo(5, 3);
            ClearText();
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("POISON GAS"));
            ForceTurn(new GscTurn("EMBER"));
            Evolve();
            ClearText();
        });

        CacheState("bugsy", () => {
            MoveTo(2053, 4, 11);
            ClearText();
            ForceTurn(new GscTurn("EMBER"));
            ForceTurn(new GscTurn("EMBER"));

            MoveTo(0, 5);
            ClearText();
            ForceTurn(new GscTurn("EMBER"));

            TalkTo(5, 7);
            ForceTurn(new GscTurn("EMBER"));
            ForceTurn(new GscTurn("EMBER"));
            ForceTurn(new GscTurn("EMBER"), new GscTurn("FURY CUTTER", Miss));
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("FURY CUTTER", Miss));
            ClearText();
        });

        CacheState("ilexforest", () => {
            MoveTo(2055, 5, 11);
            ClearText();
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER", Crit | SideEffect), new GscTurn("LEER"));
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("LEER"));
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER", Crit));
            ClearText();

            TalkTo("IlexForest", 14, 31, Action.Up);
            TalkTo(15, 25, Action.Down);
            TalkTo(15, 29, Action.Down);
            TalkTo(10, 35, Action.Left);
            TalkTo(5, 28);

            MoveTo(8, 26);
            UseItem("HM01", "QUILAVA", "TACKLE");
            Cut();
            TalkTo(2820, 6, 2);
            Yes();
            ClearText();
        });

        CacheState("spinnerhell", () => {
            MoveTo(2818, 29, 30);
            RegisterItem("BICYCLE");
            UseItem("BICYCLE");
            TalkTo(2574, 1, 4);
            Yes();
            ClearText();
            MoveTo(783, 10, 47);
            UseRegisteredItem();
            MoveTo(2563, 18, 9);
            UseRegisteredItem();
            TalkTo(33, 12);
            MoveTo(783, 33, 19);
            UseRegisteredItem();
            TalkTo(2575, 7, 1);
            Deposit("TOGEPI");
            MoveTo(2562, 3, 6);
            UseRegisteredItem();
            Execute("D D D D D D D");
            MoveTo(2819, 13, 13);
            ClearText();
        });

        CacheState("whitney", () => {
            ForceTurn(new GscTurn("EMBER"), new GscTurn("CHARM"));
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("CHARM"));

            TalkTo(8, 3);
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("DOUBLESLAP", Miss));
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER"), new GscTurn("ROLLOUT", Miss));
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("ROLLOUT", Miss));
            ForceTurn(new GscTurn("EMBER", Crit), new GscTurn("ROLLOUT", Miss));
            ClearText();

            Execute("D");
            ClearText();
            TalkTo(8, 3);
        });

        CacheState("rival3", () => {
            TalkTo(2824, 5, 6);
            TalkTo(2, 4);
            MoveTo(2818, 29, 6);
            UseRegisteredItem();
            MoveTo(783, 10, 47);
            UseRegisteredItem();
            MoveTo(2563, 18, 9);
            UseRegisteredItem();
            TalkTo(2563, 35, 9);
            Yes();
            ClearText();
            Press(Joypad.Down, Joypad.Right, Joypad.A);
            ClearText();
            ClearText();
            MoveTo(781, 9, 15);
            ClearText();
            Execute("L U L L U L L U U L U U U U U U U R R U U U R R R R D D D D R R R D D D D L L L");
            ClearText();
            ForceTurn(new GscTurn("EMBER"), new GscTurn("SPITE", 2));
            ForceTurn(new GscTurn("EMBER", Crit));
            ForceTurn(new GscTurn("EMBER", Crit | SideEffect), new GscTurn("LEER"));
            ForceTurn(new GscTurn("CUT", Crit), new GscTurn("LEER"));
            ForceTurn(new GscTurn("CUT", Crit));
            No();
            ClearText();
            Yes();
            ClearText();
            ForceTurn(new GscTurn("CUT"), new GscTurn("SUPERSONIC", Miss));
            ForceTurn(new GscTurn("CUT", Crit));
            ForceTurn(new GscTurn("EMBER"));
            ClearText();
        });

        Dispose();
    }
}