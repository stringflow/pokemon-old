using System;
using System.Collections.Generic;
using static RedBlueForce;

public class BlueTas : RedBlueForce {

    // TODO:
    //  - TAS menu execution
    //  - TAS instant text execution (this is challenging)
    //  - PickupItemAt?
    //  - Automatic fly menus
    //  - Better pathfinding
    //    > Make it take hidden sprites into account
    //    > Make it take moved sprites into account
    //    > TalkTo is implemented in not a very smart way, need to rethink how to do it
    //    > Being able to specify which direction you want to talk to an npc from (ie dig rocket)
    //    > https://gunnermaniac.com/pokeworld?local=149#3/1 current code talks from the right because it tries to avoid bonks at all cost, needs refactoring
    //    > Directional warps
    //  - Better NPC support (being able to specify how they should move)
    //  - Auto de-surfing

    public BlueTas() : base("roms/pokeblue.gbc", true) {
        Record("blue-tas"); // NOTE: Record requires ffmpeg.exe to be in PATH, it will output to movies/video.mp4, movies/audio.mp3, stitch the two together and save to movies/blue-tas.mp4
        //Show();

        new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame).Execute(this);
        ClearText(Joypad.A);
        Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        ClearText(Joypad.A);
        Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        ClearText(Joypad.A); // Journey begins!

        SetOptions(0, 1, 1);

        MoveTo("PalletTown", 10, 1); // Oak cutscene
        ClearText();

        TalkTo(7, 3);
        Press(Joypad.A);
        ClearText();
        Press(Joypad.A);
        Press(Joypad.None, Joypad.A, Joypad.Start); // TODO: Nickname function?
        ForceGiftDVs(0xe178);
        ClearText(); // Squirtle received

        MoveTo(5, 6);
        ClearText();

        // RIVAL1
        ForceTurn(new RbyTurn("TAIL WHIP"), new RbyTurn("GROWL", Miss));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("GROWL", Miss));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("GROWL", Miss));
        ForceTurn(new RbyTurn("TACKLE"));

        ClearText(); // sneaky joypad call
        Map.Sprites.Remove(5, 10); // https://gunnermaniac.com/pokeworld?map=40#5/10
        Map.Sprites.Remove(4, 3); // https://gunnermaniac.com/pokeworld?local=40#4/3

        MoveTo("ViridianCity", 29, 19);
        ClearText(); // Receive parcel
        MoveTo("OaksLab", 4, 2);
        Press(Joypad.Right, Joypad.A); // give parcel
        ClearText();
        ClearText(); // sneaky joypad call

        TalkTo("ViridianMart", 1, 5);
        Buy("POKE BALL", 4);

        MoveTo("Route22", 33, 12);
        ForceEncounter(Action.Up, 8, 0xf6ef);
        ForceYoloball();
        ClearText();
        Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start); // nido nickname

        Maps["ViridianCity"].Sprites.Remove(18, 9); // https://gunnermaniac.com/pokeworld?local=1#18/9
        MoveTo("ViridianForest", 2, 21);

        // L5 PIKACHU
        ForceEncounter(Action.Up, 9, 0x0000);
        ClearText();
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("THUNDERSHOCK", 38));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("THUNDERSHOCK", 1));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("GROWL", Miss));

        // WEEDLE GUY
        TalkTo(2, 19);
        ForceTurn(new RbyTurn("TACKLE", Crit), new RbyTurn("STRING SHOT", Miss));
        ForceTurn(new RbyTurn("TACKLE", Crit), new RbyTurn("STRING SHOT", Miss));
        ForceTurn(new RbyTurn("TACKLE", Crit), new RbyTurn("STRING SHOT", Miss));
        ForceTurn(new RbyTurn("TACKLE", Crit), new RbyTurn("STRING SHOT", Miss));

        MoveTo(Maps["Route2"][6, 2]);
        ForceEncounter(Action.Right, 4, 0xffff);
        ClearText();
        ForceYoloball();
        ClearText();
        Press(Joypad.B); // pidgey caught

        // BROCK
        TalkTo(Maps["PewterGym"][4, 1]);
        ForceTurn(new RbyTurn("BUBBLE"), new RbyTurn("TACKLE", Miss));
        ForceTurn(new RbyTurn("BUBBLE"), new RbyTurn("TACKLE", Miss));
        ForceTurn(new RbyTurn("BUBBLE", Crit | 38), new RbyTurn("TACKLE", Miss));
        ForceTurn(new RbyTurn("BUBBLE"), new RbyTurn("TACKLE", Crit | 38));
        ChooseMenuItem(1);
        ClearText();
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("TACKLE", 38));

        TalkTo("PewterMart", 1, 5);
        Sell("TM34", 1);
        Buy("ESCAPE ROPE", 7);

        // ROUTE 3 TRAINER 1
        MoveTo("Route3", 11, 6);
        ClearText();
        ForceTurn(new RbyTurn("LEER"), new RbyTurn("TACKLE", 38));
        MoveSwap("LEER", "HORN ATTACK");
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("TACKLE", 38));
        ForceTurn(new RbyTurn("HORN ATTACK"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("STRING SHOT", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("TACKLE", 39));
        ForceTurn(new RbyTurn("HORN ATTACK"));

        // ROUTE 3 TRAINER 2
        TalkTo(14, 4);
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("LEER", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK"));

        // ROUTE 3 TRAINER 3
        TalkTo(19, 5);
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("HARDEN"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));
        ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("HARDEN"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));

        // ROUTE 3 TRAINER 4
        TalkTo(24, 6);
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("TACKLE", 38));
        ForceTurn(new RbyTurn("HORN ATTACK"));
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("HARDEN"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));

        MoveTo("MtMoon1F", 2, 3);
        PickupItem(); // moonstone

        // MOON ROCKET
        TalkTo("MtMoonB2F", 11, 16);
        ForceTurn(new RbyTurn("POISON STING"), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("SUPERSONIC", Miss));
        ForceTurn(new RbyTurn("TACKLE"));

        // SUPER NERD
        MoveTo(13, 8);
        ClearText();
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("POUND", 1));
        ForceTurn(new RbyTurn("HORN ATTACK"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("SCREECH", Miss));
        ForceTurn(new RbyTurn("POISON STING"), new RbyTurn("SCREECH", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("SMOG", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));

        Evolve(); // evolution
        Press(Joypad.Up, Joypad.A);
        ClearText();
        Press(Joypad.A);
        ClearText(); // helix fossil picked up

        Map.Sprites.Remove(13, 6); // https://gunnermaniac.com/pokeworld?local=61#13/6
        MoveTo("Route4", 72, 14);
        ForceEncounter(Action.Right, 9, 0x0000);
        ClearText();
        ForceYoloball();
        ClearText();
        Press(Joypad.B); // sandshrew caught

        TalkTo("CeruleanPokecenter", 3, 2);
        Press(Joypad.A);
        ClearText(); // healed at center

        MoveTo("CeruleanGym", 4, 10);

        PartySwap("SQUIRTLE", "NIDORINO");
        UseItem("MOON STONE", "NIDORINO");

        // MISTY MINION
        MoveTo(5, 3);
        ClearText();
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));

        // MISTY
        TalkTo(4, 2);
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("WATER GUN", Miss));
        ForceTurn(new RbyTurn("POISON STING"));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("BUBBLEBEAM", 20));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("WATER GUN", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("WATER GUN", Miss));

        MoveTo("BikeShop", 2, 6);
        UseItem("TM11", "NIDOKING", "TACKLE");

        TalkTo(6, 3);
        Press(Joypad.B);
        ClearText(); // got instant text

        // RIVAL 2
        MoveTo("CeruleanCity", 21, 6);
        ClearText();
        ClearText(); // sneaky joypad call
        ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("GUST", 1));
        MoveSwap("HORN ATTACK", "BUBBLEBEAM");
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("POISON STING", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("HORN ATTACK", Crit));

        // NUGGET BRIDGE #1
        TalkTo("Route24", 11, 31);
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));

        // NUGGET BRIDGE #2
        TalkTo(10, 28);
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit), new RbyTurn("SAND-ATTACK", Miss));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // NUGGET BRIDGE #3
        TalkTo(11, 25);
        ForceTurn(new RbyTurn("BUBBLEBEAM"), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // NUGGET BRIDGE #4
        TalkTo(10, 22);
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit), new RbyTurn("SAND-ATTACK", Miss));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // NUGGET BRIDGE #5
        TalkTo(11, 19);
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // NUGGET BRIDGE #5
        MoveTo(10, 15);
        ClearText();
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // HIKER
        MoveTo("Route25", 14, 7);
        ClearText();
        ForceTurn(new RbyTurn("BUBBLEBEAM"));

        // LASS
        TalkTo(18, 8);
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        // JR TRAINER
        MoveTo(24, 6);
        ClearText();
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit), new RbyTurn("TAIL WHIP", Miss));
        TeachLevelUpMove(3);
        ForceTurn(new RbyTurn("BUBBLEBEAM"));

        // ODDISH GIRL
        TalkTo(37, 4);
        MoveSwap("BUBBLEBEAM", "THRASH");
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("SAND-ATTACK", Miss));
        ForceTurn(new RbyTurn("THRASH"));

        TalkTo("BillsHouse", 6, 5);
        Press(Joypad.A);
        ClearText();
        TalkTo(1, 4);
        TalkTo(4, 4);
        UseItem("ESCAPE ROPE"); // escape rope out of bill's house

        TalkTo("BikeShop", 6, 3);
        Press(Joypad.B);
        ClearText(); // got instant text

        // DIG ROCKET
        Maps["CeruleanCity"].Sprites.Remove(27, 12);
        MoveTo("CeruleanCity", 30, 9);
        Press(Joypad.Up, Joypad.A); ClearText();
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        MoveTo("UndergroundPathNorthSouth", 4, 4);
        PickupItem();

        // ROUTE 6 #1
        MoveTo("Route6", 11, 29);
        Press(Joypad.A); ClearText();
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("SAND-ATTACK", Miss));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("SAND-ATTACK", Miss));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("SAND-ATTACK", Miss));

        // ROUTE 6 #2
        MoveTo(10, 31);
        ClearText();
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        MoveTo("VermilionCity", 18, 30);
        ClearText();

        // RIVAL 3
        MoveTo("SSAnne2F", 37, 8);
        ClearText();
        ForceTurn(new RbyTurn("THRASH", Crit), new RbyTurn("QUICK ATTACK", Crit | 20));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH", Crit));

        Map.Sprites.Remove(36, 4);
        TalkTo("SSAnneCaptainsRoom", 4, 2); // hm02 received

        MoveTo("SSAnne1F", 27, 0);
        ClearText();
        ClearText(); // watch cutscene

        MoveTo("VermilionCity", 15, 17);
        UseItem("HM01", "SANDSHREW");
        Cut();

        MoveTo("VermilionGym", 4, 9);
        Press(Joypad.Left);
        ForceCan();
        Press(Joypad.Right);
        ForceCan();

        Execute("U U U U R U U U");
        Press(Joypad.A);
        ClearText();
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("GROWL", Miss));
        ForceTurn(new RbyTurn("THRASH", Crit));

        Execute("D D"); MoveTo("VermilionCity", 15, 19);
        Cut();
        TalkTo("PokemonFanClub", 3, 1);
        Press(Joypad.A);
        ClearText();
        UseItem("ESCAPE ROPE"); // Escape rope to cerulean

        TalkTo("BikeShop", 6, 3);

        MoveTo("CeruleanCity", 13, 26);
        ItemSwap("POKE BALL", "BICYCLE");
        UseItem("TM24", "NIDOKING", "HORN ATTACK");
        UseItem("BICYCLE");

        MoveTo(19, 27);
        Cut();
        MoveTo("Route9", 4, 8);
        Cut();

        // 4 TURN THRASH
        TalkTo(13, 10);
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        // BUG CATCHER
        TalkTo(40, 8);
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        // POKEMANIAC #1
        TalkTo("RockTunnel1F", 23, 7);
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("THUNDERBOLT"));

        // POKEMANIAC #2
        TalkTo("RockTunnelB1F", 26, 30);
        ForceTurn(new RbyTurn("THUNDERBOLT"));

        // ODDISH GIRL
        TalkTo(14, 28);
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        // HIKER
        TalkTo(6, 10);
        ForceTurn(new RbyTurn("THUNDERBOLT"), new RbyTurn("SELFDESTRUCT", Miss));
        ForceTurn(new RbyTurn("THUNDERBOLT"), new RbyTurn("SELFDESTRUCT", Miss));
        ForceTurn(new RbyTurn("THUNDERBOLT"), new RbyTurn("SELFDESTRUCT", Miss));

        // PIDGEY GIRL
        TalkTo("RockTunnel1F", 22, 24);
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("QUICK ATTACK", 38));

        // GAMBLER
        TalkTo("Route8", 46, 13);
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit), new RbyTurn("EMBER", Miss));

        MoveTo("UndergroundPathWestEast", 47, 2);
        UseItem("BICYCLE");
        MoveTo(21, 4);
        PickupItem();

        MoveTo("Route7", 5, 14);
        UseItem("BICYCLE");

        TalkTo("CeladonMart2F", 7, 3);
        Buy("TM07", 2);

        TalkTo("CeladonMart4F", 5, 6);
        Buy("POKE DOLL", 2);

        TalkTo("CeladonMartRoof", 12, 2);
        ChooseMenuItem(0); // fresh water
        ClearText();

        TalkTo("CeladonMart5F", 5, 4);
        Buy("X SPEED", 3);

        TalkTo("CeladonMartElevator", 3, 0);
        ChooseMenuItem(0);

        Execute("L D D");
        MoveTo("CeladonCity", 8, 14);
        UseItem("BICYCLE");

        MoveTo("Route16", 34, 10);
        Press(Joypad.Up);
        Cut();
        MoveTo("Route16", 25, 4);
        MoveTo("Route16", 17, 4);
        UseItem("BICYCLE");
        TalkTo("Route16FlyHouse", 2, 3); // fly received

        MoveTo("Route16", 7, 6);
        ItemSwap("HELIX FOSSIL", "POKE DOLL");
        UseItem("TM07", "NIDOKING", "LEER");
        UseItem("HM02", "PIDGEY");
        Fly(Joypad.Down, 3);

        // RIVAL 4
        MoveTo("PokemonTower2F", 15, 5);
        ClearText();
        ForceTurn(new RbyTurn("HORN DRILL"), new RbyTurn("SAND-ATTACK"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH", Crit));

        Map.Sprites.Remove(14, 5);

        // CHANNELER #1
        TalkTo("PokemonTower4F", 15, 7);
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

        MoveTo(13, 10);
        PickupItem();

        MoveTo("PokemonTower5F", 11, 9);
        ClearText(); // heal pad

        // CHANNELER #2
        MoveTo("PokemonTower6F", 15, 5);
        ClearText();
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("NIGHT SHADE"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("NIGHT SHADE"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("NIGHT SHADE"));
        ForceTurn(new RbyTurn("THRASH"), new RbyTurn("NIGHT SHADE"));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

        // CHANNELER #3
        TalkTo("PokemonTower6F", 9, 5);
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

        MoveTo(6, 7);
        PickupItem();
        Execute("D"); MoveTo(10, 16);
        ClearText();
        UseItem("POKE DOLL"); // escape ghost

        // ROCKET #1
        MoveTo("PokemonTower7F", 10, 11);
        ClearText();
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH", Crit));

        // ROCKET #2
        MoveTo(10, 9);
        ClearText();
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit));

        // ROCKET #3
        MoveTo(10, 7);
        ClearText();
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH"));

        TalkTo(10, 3);
        MoveTo(2, 1);
        Press(Joypad.Right, Joypad.A);
        ClearText(); // Pokeflute received

        MoveTo("LavenderTown", 7, 10);
        Fly(Joypad.Down, 1);

        UseItem("BICYCLE");

        MoveTo("Route16", 27, 10);
        ItemSwap("POKE DOLL", "ELIXER");
        UseItem("POKE FLUTE");
        RunAway();

        Execute("L"); MoveTo("Route17", 8, 120);
        PickupItem();

        MoveTo("Route18", 32, 8);
        MoveTo("Route18", 40, 8);
        UseItem("BICYCLE");

        MoveTo("FuchsiaCity", 18, 20); Press(Joypad.Up);
        Cut();
        MoveTo(16, 12);
        Cut();
        MoveTo("SafariZoneGate", 3, 2);
        ClearText();
        Press(Joypad.A);
        ClearText();
        ClearText(); // sneaky joypad call

        UseItem("BICYCLE");
        MoveTo("SafariZoneWest", 19, 6);
        PickupItem();

        MoveTo("SafariZoneSecretHouse", 3, 4);
        Press(Joypad.A);
        ClearText();
        MoveTo("SafariZoneWest", 3, 4);
        UseItem("ESCAPE ROPE");
        Fly(Joypad.Down, 1);

        UseItem("BICYCLE");

        // JUGGLER #1
        TalkTo("FuchsiaGym", 7, 8);
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", 39));
        ForceTurn(new RbyTurn("THRASH", Crit));

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
        ForceTurn(new RbyTurn("ELIXER", 0), new RbyTurn("SELFDESTRUCT", Miss));

        MoveTo("FuchsiaCity", 5, 28);
        UseItem("BICYCLE");

        MoveTo("WardensHouse", 2, 4);
        Press(Joypad.A);
        ClearText();

        MoveTo("FuchsiaCity", 27, 28);
        Fly(Joypad.None, 0);

        MoveTo(4, 13);
        ItemSwap("NUGGET", "X SPEED");
        UseItem("HM03", "SQUIRTLE");
        UseItem("RARE CANDY", "NIDOKING");
        Surf();

        MoveTo(8, 3, 4); Execute("R");

        MoveTo("PokemonMansion3F", 10, 6);
        Press(Joypad.Up);
        ActivateMansionSwitch();

        MoveTo(16, 13); Execute(Action.Down);
        FallDown(); // TODO: remove this
        MoveTo("PokemonMansionB1F", 18, 26);
        Press(Joypad.Up);
        ActivateMansionSwitch();

        MoveTo(12, 19); // Outsmart pathfinding as it can't see the doors
        MoveTo(20, 4);
        Press(Joypad.Up);
        ActivateMansionSwitch();
        MoveTo(5, 12);
        PickupItem();
        UseItem("ESCAPE ROPE");
        Fly(Joypad.Down, 3);

        UseItem("BICYCLE");
        MoveTo("Route7Gate", 3, 4);
        ClearText();
        MoveTo("Route7", 18, 10);
        UseItem("BICYCLE");

        Maps[10].Sprites.Remove(18, 22); // https://gunnermaniac.com/pokeworld?local=10#18/22
        Maps[181].Warps.Remove(16, 10); // https://gunnermaniac.com/pokeworld?local=181#16/10 hmm
        MoveTo("SilphCo5F", 14, 3);
        Press(Joypad.Left); PickupItem();

        // ARBOK TRAINER
        TalkTo(8, 16);
        ForceTurn(new RbyTurn("HORN DRILL"));

        Execute("R U D");
        MoveTo(20, 16);
        PickupItem();
        MoveTo(9, 15);
        Execute("U D");
        MoveTo(9, 13);
        Execute("L");
        UnlockDoor();
        MoveTo("SilphCo3F", 18, 9);
        Press(Joypad.Left);
        UnlockDoor();
        MoveTo("SilphCo7F", 5, 2);
        Execute("L L");
        ClearText();

        // SILPH RIVAL
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit), new RbyTurn("QUICK ATTACK", 15));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        // SILPH ROCKET
        TalkTo("SilphCo11F", 3, 16);
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));

        Maps["SilphCo11F"].Warps.Remove(5, 5); // https://gunnermaniac.com/pokeworld?local=235#5/5 hmm
        MoveTo(6, 14);
        UnlockDoor();
        Execute("U");
        ClearText();

        // SILPH GIO
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        UseItem("ELIXER", 0);
        UseItem("ESCAPE ROPE");

        Fly(Joypad.Down, 2);

        UseItem("BICYCLE");
        MoveTo("CinnabarGym", 15, 8);
        BlaineQuiz(Joypad.A);
        MoveTo(10, 2); Press(Joypad.Up);
        BlaineQuiz(Joypad.B);
        MoveTo(9, 8); Press(Joypad.Up);
        BlaineQuiz(Joypad.B);
        MoveTo(9, 14); Press(Joypad.Up);
        BlaineQuiz(Joypad.B);
        MoveTo(1, 14);
        BlaineQuiz(Joypad.A);
        MoveTo(1, 8);
        BlaineQuiz(Joypad.B);

        // BLAINE
        TalkTo(3, 4);
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        UseItem("ESCAPE ROPE");
        Fly(Joypad.Down, 4);
        UseItem("BICYCLE");
        MoveTo(35, 31);
        Cut();
        MoveTo("CeladonGym", 1, 4);
        Cut();

        // BEAUTY
        Execute("R");
        ClearText();
        ForceTurn(new RbyTurn("HORN DRILL"));

        // ERIKA
        Execute("R");
        Press(Joypad.Up, Joypad.None, Joypad.Up, Joypad.A); // turn frame
        ClearText();
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit));

        MoveTo(5, 6);
        Cut();
        MoveTo("CeladonCity", 12, 28);
        Fly(Joypad.Down, 1);

        Map.Sprites.Remove(34, 4);

        UseItem("BICYCLE");

        // SABRINA
        TalkTo("SaffronGym", 9, 8);
        ForceTurn(new RbyTurn("THRASH"));
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit));
        ForceTurn(new RbyTurn("THRASH", Crit), new RbyTurn("PSYWAVE", 26));

        MoveTo(1, 5);
        UseItem("ESCAPE ROPE");
        Fly(Joypad.Up, 1);

        UseItem("BICYCLE");

        // RHYHORN
        MoveTo("ViridianGym", 15, 5);
        ClearText();
        ForceTurn(new RbyTurn("BUBBLEBEAM"));

        // BLACKBELT
        Execute("U"); MoveTo(45, 10, 4);
        ClearText();
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

        // GIOVANNI
        MoveTo("ViridianCity", 32, 8);
        TalkTo("ViridianGym", 2, 1);
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

        MoveTo("ViridianCity", 32, 8);
        ItemSwap("S.S.TICKET", "MAX ELIXER");
        UseItem("HM04", "SANDSHREW");
        UseItem("TM27", "NIDOKING", "THRASH");
        UseItem("BICYCLE");

        // VIRIDIAN RIVAL
        MoveTo(33, 29, 5);
        ClearText();
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("TAIL WHIP", Miss));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("THUNDERBOLT"));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("FISSURE"));

        MoveTo(193, 4, 2);
        ClearText();
        MoveTo(34, 7, 139);
        UseItem("BICYCLE");
        MoveTo(7, 136);
        ClearText();
        MoveTo(9, 119);
        ClearText();
        MoveTo(10, 105);
        ClearText();
        Execute("U");
        Surf();
        MoveTo(10, 96);
        ClearText();
        MoveTo(7, 85);
        ClearText();
        MoveTo(8, 72); Execute("U");
        UseItem("BICYCLE");
        MoveTo(12, 56);
        ClearText();
        MoveTo(5, 35);
        ClearText();
        MoveTo(108, 8, 16);

        Strength();
        MoveTo(5, 14);
        PushBoulder(Joypad.Down);
        Execute("D L D");
        for(int i = 0; i < 4; i++) { PushBoulder(Joypad.Right); Execute("R"); }
        Press(Joypad.Down, Joypad.Down); Execute("R");
        for(int i = 0; i < 2; i++) { PushBoulder(Joypad.Up); Execute("U"); }
        Execute("L U");
        for(int i = 0; i < 7; i++) { PushBoulder(Joypad.Right); Execute("R"); }
        Execute("D R");
        PushBoulder(Joypad.Up); Execute("U");
        PushBoulder(Joypad.Up);
        Execute("L L U U R");
        PushBoulder(Joypad.Right);
        Execute("U R R");
        PushBoulder(Joypad.Down);
        MoveTo(194, 0, 9);

        Strength();
        MoveTo(5, 14);
        PushBoulder(Joypad.Left);
        Execute("U L L");
        PushBoulder(Joypad.Down); Execute("D");
        PushBoulder(Joypad.Down);
        Execute("R D D");
        PushBoulder(Joypad.Left); Execute("L");
        PushBoulder(Joypad.Left);

        MoveTo(198, 23, 6);
        Strength();
        MoveTo(22, 4);
        for(int i = 0; i < 2; i++) { PushBoulder(Joypad.Up); Execute("U"); }
        Execute("R U");
        for(int i = 0; i < 16; i++) { PushBoulder(Joypad.Left); Execute("L"); }
        Execute("U L");
        PushBoulder(Joypad.Down);
        Execute("R D D");
        for(int i = 0; i < 4; i++) { PushBoulder(Joypad.Left); Execute("L"); }
        Execute("U L");
        for(int i = 0; i < 3; i++) { PushBoulder(Joypad.Down); Execute("D"); }
        Execute("L D");
        PushBoulder(Joypad.Right); Execute("U");

        MoveTo(21, 15);
        PushBoulder(Joypad.Right);
        MoveTo(23, 15);
        FallDown();

        Strength();
        UseItem("ELIXER", "NIDOKING");
        UseItem("BICYCLE");
        Execute("D R R U");
        for(int i = 0; i < 14; i++) { PushBoulder(Joypad.Left); Execute("L"); }

        MoveTo(174, 15, 8);

        // TODO: PC functions
        Press(Joypad.A);
        ClearText();
        ChooseMenuItem(0);
        ClearText();
        for(int i = 0; i < 3; i++) {
            ChooseMenuItem(1);
            ChooseMenuItem(1);
            ChooseMenuItem(0);
            ClearText();
        }
        MenuPress(Joypad.B);
        MenuPress(Joypad.B);

        // LORELEI
        MoveTo("LoreleisRoom", 4, 2); Press(Joypad.Right, Joypad.None, Joypad.Right, Joypad.A);
        ClearText();
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        // BRUNO
        Execute("U U");
        MoveTo("BrunosRoom", 4, 2); Press(Joypad.Right, Joypad.None, Joypad.Right, Joypad.A);
        ClearText();
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        // AGATHA
        Execute("U U");
        MoveTo("AgathasRoom", 4, 2); Press(Joypad.Right, Joypad.None, Joypad.Right, Joypad.A);
        ClearText();
        ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("HYPNOSIS", Miss));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("FISSURE"));

        UseItem("MAX ELIXER", "NIDOKING");

        // LANCE
        Execute("U U");
        ClearText();
        MoveTo("LancesRoom", 5, 1);
        ClearText();
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("THUNDERBOLT", Crit), new RbyTurn("SUPERSONIC", Miss));
        ForceTurn(new RbyTurn("HORN DRILL"));

        // CHAMPION
        Execute("U");
        ClearText();
        ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("MIRROR MOVE", Miss));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("FISSURE"));
        ForceTurn(new RbyTurn("BUBBLEBEAM"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));
        ForceTurn(new RbyTurn("HORN DRILL"));

        ClearText();

        Dispose();
    }
}