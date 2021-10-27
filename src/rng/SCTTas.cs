public class SCTTas : RedBlueForce {

    public SCTTas() : base("roms/pokered.gbc", true) {
        // This is an implementation of the tauros section of MrWint's movie (http://tasvideos.org/7131S.html)
        // For documentation of the code, please check the blue nido file.
        Record("red-sct");

        CacheState("bk2", () => {
            PlayBizhawkMovie("bizhawk/red-sct.bk2", 187398);
        });

        CacheState("safari", () => {
            MoveTo(218, 24, 30);
            ForceEncounter(Action.Up, 9, 0xc6ca);
            ForceSafariYoloball();
            ClearText();
            Yes();
            Press(Joypad.None, Joypad.A, Joypad.Start);

            PickupItemAt("SafariZoneWest", 19, 7, Action.Down);

            TalkTo("SafariZoneSecretHouse", 3, 3);
            MoveTo("SafariZoneWest", 3, 4);
            UseItem("TM07", "TAUROS");
            UseItem("ESCAPE ROPE");
            PartySwap("TAUROS", "CLEFABLE");
        });

        CacheState("koga", () => {
            Fly("FuchsiaCity");
            UseItem("BICYCLE");

            // JUGGLER #1
            TalkTo("FuchsiaGym", 7, 8);
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP"), new RbyTurn("PSYBEAM", Crit | 12));
            ForceTurn(new RbyTurn("STOMP", Crit));

            // JUGGLER #2
            MoveTo(1, 7);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            // KOGA
            TalkTo(4, 10);
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("TACKLE", Miss), new RbyTurn("SELFDESTRUCT", Miss));

            MoveTo("FuchsiaCity", 5, 28);
            ItemSwap("TM07", "ELIXER");
            UseItem("HM03", "SQUIRTLE", "BUBBLE");
            UseItem("MAX ELIXER", "TAUROS");
            UseItem("BICYCLE");
            TalkTo("WardensHouse", 2, 3);
            MoveTo("FuchsiaCity", 27, 28);
        });

        CacheState("mansion", () => {
            Fly("PalletTown");

            MoveTo(4, 13, Action.Down);
            Surf();

            MoveTo("CinnabarIsland", 4, 4);

            TalkTo("PokemonMansion3F", 10, 5, Action.Up);
            ActivateMansionSwitch();

            MoveTo(16, 14);
            FallDown(); // TODO: look into not having to do this
            PickupItemAt("PokemonMansionB1F", 19, 25);
            TalkTo(18, 25, Action.Up);
            ActivateMansionSwitch();

            TalkTo(20, 3, Action.Up);
            ActivateMansionSwitch();
            MoveTo(5, 12);
            UseItem("HM04", "TAUROS", "TAIL WHIP");
            UseItem("TM14", "TAUROS", "TACKLE");
            PickupItem(); // secret key
            UseItem("ESCAPE ROPE");
        });

        CacheState("silph", () => {
            UseItem("BICYCLE");
            MoveTo("Route7Gate", 3, 4);
            ClearText();
            MoveTo("Route7", 18, 10);
            UseItem("BICYCLE");

            PickupItemAt("SilphCo5F", 12, 3);

            // ARBOK TRAINER
            TalkTo(8, 16);
            ForceTurn(new RbyTurn("HORN DRILL"));

            PickupItemAt(21, 16);
            TalkTo(7, 13);
            TalkTo("SilphCo3F", 17, 9);

            // SILPH RIVAL
            MoveTo("SilphCo7F", 3, 2, Action.Left);
            ClearText();
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));

            // SILPH ROCKET
            TalkTo("SilphCo11F", 3, 16);
            ForceTurn(new RbyTurn("BLIZZARD"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("BLIZZARD"));

            // SILPH GIOVANNI
            TalkTo(6, 13, Action.Up);
            MoveTo(6, 13);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("BLIZZARD"));
            ForceTurn(new RbyTurn("ELIXER", "TAUROS"), new RbyTurn("SCRATCH", 25));
            ForceTurn(new RbyTurn("HORN DRILL"));

            UseItem("ESCAPE ROPE");
        });

        CacheState("erika", () => {
            UseItem("BICYCLE");
            MoveTo(18, 18, 10);
            UseItem("BICYCLE");

            // SABRINA
            TalkTo("SaffronGym", 9, 8);
            ForceTurn(new RbyTurn("STOMP"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            SkipLevelUpMove();
            ForceTurn(new RbyTurn("STRENGTH", Crit));
            ForceTurn(new RbyTurn("STRENGTH", Crit), new RbyTurn("RECOVER", Miss));

            MoveTo(1, 5);
            UseItem("ESCAPE ROPE");

            UseItem("BICYCLE");
            CutAt(35, 32);
            CutAt("CeladonGym", 2, 4);

            // BEAUTY
            MoveTo(3, 4);
            ClearText();
            ForceTurn(new RbyTurn("STOMP", Crit));

            // ERIKA
            TalkTo(4, 3);
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP", Crit));
            CutAt(5, 7);
            MoveTo("CeladonCity", 12, 28);
        });

        CacheState("blaine", () => {
            Fly("CinnabarIsland");
            UseItem("BICYCLE");
            TalkTo("CinnabarGym", 15, 7, Action.Up);
            BlaineQuiz(Joypad.A);
            TalkTo(10, 1, Action.Up);
            BlaineQuiz(Joypad.B);
            TalkTo(9, 7, Action.Up);
            BlaineQuiz(Joypad.B);
            TalkTo(9, 13, Action.Up);
            BlaineQuiz(Joypad.B);
            TalkTo(1, 13, Action.Up);
            BlaineQuiz(Joypad.A);
            TalkTo(1, 7, Action.Up);
            BlaineQuiz(Joypad.B);

            // BLAINE
            TalkTo(3, 3);
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            UseItem("ESCAPE ROPE");
        });

        CacheState("giovanni", () => {
            Fly("ViridianCity");
            UseItem("BICYCLE");

            // RHYHORN
            MoveTo("ViridianGym", 15, 5);
            ClearText();
            ForceTurn(new RbyTurn("BLIZZARD"));

            // BLACKBELT
            MoveTo(10, 4);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STRENGTH", Crit));

            // GIOVANNI
            MoveTo("ViridianCity", 32, 8);
            TalkTo("ViridianGym", 2, 1);
            ForceTurn(new RbyTurn("BLIZZARD"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
            ForceTurn(new RbyTurn("BLIZZARD", Crit));

            MoveTo("ViridianCity", 32, 8);
        });

        CacheState("viridan-rival", () => {
            UseItem("BICYCLE");

            // VIRIDIAN RIVAL
            MoveTo("Route22", 29, 5);
            ClearText();
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
            ForceTurn(new RbyTurn("ELIXER", "TAUROS"), new RbyTurn("STOMP", 5));
            ForceTurn(new RbyTurn("BLIZZARD"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("STOMP", Crit));
            ForceTurn(new RbyTurn("STRENGTH", Crit), new RbyTurn("REFLECT"));
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("victory-road", () => {
            MoveTo("Route22Gate", 4, 2, Action.Up);
            ClearText();
            MoveTo("Route23", 7, 139);
            ItemSwap("HELIX FOSSIL", "RARE CANDY");
            UseItem("TM27", "TAUROS", "STOMP");
            UseItem("BICYCLE");

            MoveTo(7, 136, Action.Up);
            ClearText();
            MoveTo(9, 119, Action.Up);
            ClearText();
            MoveTo(10, 105, Action.Up);
            ClearText();
            MoveTo(10, 104, Action.Up);
            Surf();
            MoveTo(10, 96, Action.Up);
            ClearText();
            MoveTo(7, 85, Action.Up);
            ClearText();
            MoveTo(8, 71, Action.Up);
            UseItem("BICYCLE");
            MoveTo(12, 56, Action.Up);
            ClearText();
            MoveTo(5, 35, Action.Up);
            ClearText();

            MoveTo("VictoryRoad1F", 8, 17);
            Strength();
            MoveTo(5, 14);
            PushBoulder(Joypad.Down);
            Execute("D L D");
            PushBoulder(Joypad.Right, 4);
            Execute("R D R");
            PushBoulder(Joypad.Up, 2);
            Execute("U L U");
            PushBoulder(Joypad.Right, 7);
            Execute("R D R");
            PushBoulder(Joypad.Up, 2);
            Execute("L L U U R");
            PushBoulder(Joypad.Right);
            Execute("U R R");
            PushBoulder(Joypad.Down);
            MoveTo("VictoryRoad2F", 0, 8);

            Strength();
            MoveTo(5, 14);
            PushBoulder(Joypad.Left);
            Execute("U L L");
            PushBoulder(Joypad.Down, 2);
            Execute("R D D");
            PushBoulder(Joypad.Left, 2);

            MoveTo("VictoryRoad3F", 23, 7);
            Strength();
            MoveTo(22, 4);
            PushBoulder(Joypad.Up, 2);
            Execute("U R U");
            PushBoulder(Joypad.Left, 16);
            Execute("L U L");
            PushBoulder(Joypad.Down);
            Execute("R D D");
            PushBoulder(Joypad.Left, 4);
            Execute("L U L");
            PushBoulder(Joypad.Down, 3);
            Execute("D L D");
            PushBoulder(Joypad.Right);
            Execute("U");

            MoveTo(21, 15, Action.Right);
            PushBoulder(Joypad.Right);
            Execute("R R");
            FallDown();

            Strength();
            UseItem("BICYCLE");
            Execute("D R R U");
            PushBoulder(Joypad.Left, 14);

            TalkTo("IndigoPlateauLobby", 15, 8, Action.Up);

            // TODO: PC functions
            ChooseMenuItem(0);
            ClearText();
            for(int i = 0; i < 4; i++) {
                ChooseMenuItem(1);
                ChooseMenuItem(1);
                ChooseMenuItem(0);
                ClearText();
            }
            MenuPress(Joypad.B);
            MenuPress(Joypad.B);
        });

        CacheState("lorelei", () => {
            // LORELEI
            MoveTo("IndigoPlateauLobby", 8, 0);
            TalkTo("LoreleisRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("STRENGTH", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("bruno", () => {
            // BRUNO
            Execute("U U U");
            TalkTo("BrunosRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("BLIZZARD"));
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
            ForceTurn(new RbyTurn("BLIZZARD"));
            SkipLevelUpMove();
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("agatha", () => {
            UseItem("RARE CANDY", "TAUROS");

            // AGATHA
            Execute("U U U");
            TalkTo("AgathasRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("ELIXER", "TAUROS"), new RbyTurn("SCREECH"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
        });

        CacheState("lance", () => {
            // LANCE
            Execute("U U U");
            MoveTo("LancesRoom", 6, 2);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("BLIZZARD", Crit), new RbyTurn("SUPERSONIC", Miss));
            ForceTurn(new RbyTurn("BLIZZARD", Crit));
        });

        CacheState("champion", () => {
            // CHAMPION
            Execute("L U U U");
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("STRENGTH", Crit), new RbyTurn("RECOVER", Miss));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            ClearText();
        });

        Dispose();
    }
}
