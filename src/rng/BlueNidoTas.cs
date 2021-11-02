public class BlueNidoTas : RedBlueForce {

    // TODO:
    //  - TAS menu execution
    //  - TAS instant text execution (this is challenging)
    //  - Better NPC support (being able to specify how they should move)
    //  - Better pathfinding
    //    > Make pathfinding consider turn frames (last moon room/post underground elixer house)

    public BlueNidoTas() : base("roms/pokeblue.gbc", true) {
        // NOTE: Record requires ffmpeg.exe to be in PATH, it will output to movies/video.mp4, movies/audio.mp3, stitch the two together and save to movies/blue-tas.mp4
        //       If only a black window shows up, change https://github.com/stringflow/pokemon/blob/main/src/gfx/Renderer.cs#L77 to SDL2RenderContext.
        Record("blue-tas");
        //Show();

        /*
            Note: you may start after the playback of an existing bk2:
                Parameter #1: file path to the bk2 file
                Parameter #2: frame count to playback up to [Optional, if omitted the entire bk2 will be played back]
        */
        //PlayBizhawkMovie("example.bk2", 12345);

        /*
            CacheState allows you to avoid rerunning earlier segments. It will save the state of the emulator after the block of code has been executed.
            If that state already exists, the block of code will be skipped.
                Parameter #1: the name of the segment [the save state file is named after this string]
                Parameter #2: the block of code for this segment

            You can force execution of all subsequent CacheState blocks by calling 'ClearCache();'
        */
        CacheState("rival1", () => {
            new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame).Execute(this);

            /*
                ClearText clears all textboxes until user input is required.
                    Parameter #1: a button that is to be held while text is printing [Optional, if omitted no button will be held]

                    Note #1: Textboxes will be cleared with the opposite of the specified hold button (if hold is 'A', 'B' will be used and vice versa)
                    Note #2: If text speed is not set to fast yet, you want to specify a hold button. You have to be mindful of the action after ClearText
                             in order to avoid consecutive input lag. In the example below, ClearText will be interrupted by the menu that let's you choose between
                             a new nickname or preset trainer names, which we want to select new nickname with the 'A' button. Because of that we want the final input
                             from ClearText to be 'B'. The final input will be to clear the final textbox, therefore the held button of 'A' is chosen. (see note #1)
            */
            ClearText(Joypad.A);
            /*
                Presses the specified buttons as soon as the input is polled from the game.
                Multiple button presses can be specified and they will be executed in sequiental order.
                In the example below:
                  Joypad.A - Hit 'A' on NEW NAME
                  Joypad.None - Skip 1 input read to respect consecutive input lag
                  Joypad.A - Hit 'A' on the nicknaming screen to select the character "A"
                  Joypad.Start - Confirm the nickname "A"
            */
            Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
            ClearText(Joypad.A);
            Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
            ClearText(Joypad.A); // Journey begins!

            /*
                Sets the in-game options. Note: This function currently only works in the overworld, and will not work on the new-game-screen.
                The parameter is a bit field of the options you want to set.
            */
            SetOptions(Fast | Off | Set);

            /*
                Moves to the specified coordinates via pathfinding.
                    Parameter #1: map name or map id [Optional, if not specified the current map is assumed]
                    Parameter #2: x coordinate
                    Parameter #3: y coordinate
                    Parameter #4: preferred facing direction after the movement [Optional, if not specified any facing direction may be used]
                Therefore the parameters below will pathfind to https://gunnermaniac.com/pokeworld?local=0#10/1.
            */
            MoveTo("PalletTown", 10, 1); // Oak cutscene
            ClearText();

            /*
                Pathfinds in such a way to face the specified tile (or NPC), then presses 'A' to interact, and calls 'ClearText'.
                    Parameter #1: map name or map id [Optional, if not specified the current map is assumed]
                    Parameter #2: x coordinate 
                    Parameter #3: y coordinate
                    Parameter #4: direction to talk from [Optional, if not specified any facing direction may be used]
                    Note: Coordinates must be of the tile you want to interact with, not the tile the player should end up at.
                Therefore the parameters below will pathfind and then interact with https://gunnermaniac.com/pokeworld?local=40#7/3.
            */
            TalkTo(7, 3);
            /*
                Chooses Yes on Yes/No boxes.
            */
            Yes();
            ClearText();
            Yes();
            Press(Joypad.None, Joypad.A, Joypad.Start); // TODO: Nickname function?
            /*
                Modifies the RNG to produce the specified DVs when receiving a gift pokemon.
                The DVs is a single 16-bit integer where every 4 bits represents one stat, in the order: Attack, Defense, Speed, Special.
                Therefore the parameters below will produce 14 dv attack, 1 dv defense, 7 dv speed, and 8 dv special.
            */
            ForceGiftDVs(0xe178);
            ClearText(); // Squirtle received

            MoveTo(5, 6);
            ClearText();

            // RIVAL1
            /*
                Modifies the RNG to produce the specified battle RNG.
                    Parameter #1: player's turn
                    Parameter #2: opponent's turn [Optional, may be omitted if the opponent will not get a turn]
                    Parameter #3: whether or not a speed tie should be won or lost [Optional, speed tie will be won if omitted]

                For defining turns:
                    Parameter #1: name of the move *OR* name of the item that will be used
                    Parameter #2: Bitfield flags [Lower 6 bits will be the damage roll (one based, 1-39) *OR* Psywave damage,
                                                  for the rest see https://github.com/stringflow/pokemon/blob/main/src/rng/RedBlueForce.cs#L20-L24]

                Notes:
                    - Thrash/Petal Dance will be 4 turns by default.
                    - If the opponent will not get a turn, but may use a priority move, the opponent's turn may still be omitted and a non-priority move will be forced.
                    - Trainer AI is ignored for now.
            */
            ForceTurn(new RbyTurn("TAIL WHIP"), new RbyTurn("GROWL", Miss));
            ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("GROWL", Miss));
            ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("GROWL", Miss));
            ForceTurn(new RbyTurn("TACKLE"));
            ClearText(); // sneaky joypad call
        });

        CacheState("parcel", () => {
            MoveTo("ViridianCity", 29, 19);
            ClearText(); // Receive parcel
            TalkTo("OaksLab", 5, 2, Action.Right); // give parcel
        });

        CacheState("nidoran", () => {
            TalkTo("ViridianMart", 1, 5);
            /*
                Buys the specified items and quantities from the currently open mart.
                Item names should always be followed by quantities.

                For example: 
                    Buy("POKE BALL", 4, "ANTIDOTE", 2, "REPEL", 5);
                    Would buy 4 pokeballs, followed by 2 antidotes, followed by 5 repels.
            */
            Buy("POKE BALL", 4);

            MoveTo("Route22", 33, 12);
            /*
                Modifies the RNG to produce a specified encounter.
                    Parameter #1: the action that should be executed prior to the encounter
                    Parameter #2: the index of the encounter slot that should appear [zero based, 0-9]
                    Parameter #3: the dvs of the encounter
                Therefore the parameters below will produce a L3 Nidoran Male with 0xf6ef DVs after moving up.
            */
            ForceEncounter(Action.Up, 8, 0xf6ef);
            /*
                Modifies the RNG to guarantee a yoloball catch.
                    Parameter #1: name of the ball that is to be thrown
            */
            ForceYoloball("POKE BALL");
            ClearText();
            Yes();
            Press(Joypad.None, Joypad.A, Joypad.Start); // nido nickname
        });

        CacheState("viridianforest", () => {
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
            ForceYoloball("POKE BALL");
            ClearText();
            No(); // pidgey caught
        });

        CacheState("brock", () => {
            // BROCK
            TalkTo(Maps["PewterGym"][4, 1]);
            ForceTurn(new RbyTurn("BUBBLE"), new RbyTurn("TACKLE", Miss));
            ForceTurn(new RbyTurn("BUBBLE"));
            ForceTurn(new RbyTurn("BUBBLE", Crit | 38), new RbyTurn("SCREECH", Miss));
            ForceTurn(new RbyTurn("BUBBLE"), new RbyTurn("TACKLE", Crit | 38));
            SendOut("NIDORANM");
            ForceTurn(new RbyTurn("TACKLE"), new RbyTurn("TACKLE", 38));
        });

        CacheState("route3", () => {
            TalkTo("PewterMart", 1, 5);
            /*
                Works exactly like Buy.
            */
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
            ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("TAIL WHIP", Miss));
            ForceTurn(new RbyTurn("HORN ATTACK", Crit), new RbyTurn("TAIL WHIP", Miss));
            ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("LEER", Miss));
            ForceTurn(new RbyTurn("HORN ATTACK", Crit));

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
        });

        CacheState("mtmoon", () => {
            /*
                Works like TalkTo, but picks up the item at the specified coordinates instead.
            */
            PickupItemAt("MtMoon1F", 2, 2); // moonstone

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

            /*
                Skips past the evolution.
            */
            Evolve(); // evolution
            TalkTo(13, 6);
            Yes();
            ClearText(); // helix fossil picked up
        });

        CacheState("misty", () => {
            MoveTo("Route4", 72, 14);
            ForceEncounter(Action.Right, 9, 0x0000);
            ClearText();
            ForceYoloball("POKE BALL");
            ClearText();
            No(); // sandshrew caught

            TalkTo("CeruleanPokecenter", 3, 2);
            Yes();
            ClearText(); // healed at center

            MoveTo("CeruleanGym", 4, 10);

            /*
                Swaps pokemon #1 with pokemon #2.
            */
            PartySwap("NIDORINO", "SQUIRTLE");
            /*
                Uses the specified item.
                    Parameter #1: name of the item
                    Parameter #2: name of the pokemon to use the item on [Optional]
                    Parameter #3: name of the move to use the item on [Optional, currently only used for teaching TMs/HMs]
            */
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
        });

        CacheState("nuggetbridge", () => {
            MoveTo("BikeShop", 2, 6);
            UseItem("TM11", "NIDOKING", "TACKLE");

            TalkTo(6, 3);
            No();
            ClearText(); // got instant text

            // RIVAL 2
            MoveTo("CeruleanCity", 21, 6, Action.Up);
            ClearText();
            ForceTurn(new RbyTurn("HORN ATTACK"), new RbyTurn("GUST", 1));
            MoveSwap("HORN ATTACK", "BUBBLEBEAM");
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("POISON STING", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("HORN ATTACK", Crit));

            // NUGGET BRIDGE #1
            TalkTo("Route24", 11, 31);
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("BUBBLEBEAM"));

            // NUGGET BRIDGE #2
            TalkTo(10, 28);
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

            // NUGGET BRIDGE #3
            TalkTo(11, 25);
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

            // NUGGET BRIDGE #4
            TalkTo(10, 22);
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
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
            TalkTo(18, 8, Action.Down);
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));

            // JR TRAINER
            MoveTo(24, 6);
            ClearText();
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            TeachLevelUpMove("POISON STING");
            ForceTurn(new RbyTurn("BUBBLEBEAM"));

            // ODDISH GIRL
            TalkTo(37, 4);
            MoveSwap("BUBBLEBEAM", "THRASH");
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));

            TalkTo("BillsHouse", 6, 5, Action.Right);
            Yes();
            ClearText();
            TalkTo(1, 4);
            TalkTo(4, 4);
            UseItem("ESCAPE ROPE"); // escape rope out of bill's house
        });

        CacheState("ssanne", () => {
            TalkTo("BikeShop", 6, 3);
            No();
            ClearText(); // got instant text

            // DIG ROCKET
            MoveTo("CeruleanCity", 30, 9);
            ClearText();
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));

            PickupItemAt("UndergroundPathNorthSouth", 3, 4); // full restore

            // ROUTE 6 #1
            TalkTo("Route6", 11, 30, Action.Down);
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));

            // ROUTE 6 #2
            MoveTo(10, 31);
            ClearText();
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));

            MoveTo("VermilionCity", 18, 30);
            ClearText();

            // RIVAL 3
            MoveTo("SSAnne2F", 37, 8, Action.Up);
            ClearText();
            ForceTurn(new RbyTurn("THRASH", Crit), new RbyTurn("QUICK ATTACK", Crit | 20));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH", Crit));

            TalkTo("SSAnneCaptainsRoom", 4, 2); // hm02 received

            MoveTo("VermilionDock", 14, 2);
            ClearText();
            ClearText(); // watch cutscene
        });

        CacheState("surge", () => {
            MoveTo("VermilionCity", 15, 17, Action.Down);
            UseItem("HM01", "SANDSHREW");
            Cut();

            MoveTo("VermilionGym", 4, 9);
            Press(Joypad.Left);
            ForceCan();
            Press(Joypad.Right);
            ForceCan();

            // SURGE
            TalkTo(5, 1);
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH", Crit));
        });

        CacheState("rocktunnel", () => {
            CutAt("VermilionCity", 15, 18);
            TalkTo("PokemonFanClub", 3, 1);
            Yes();
            ClearText();
            UseItem("ESCAPE ROPE"); // Escape rope to cerulean

            TalkTo("BikeShop", 6, 3);

            MoveTo("CeruleanCity", 13, 26);
            ItemSwap("POKE BALL", "BICYCLE");
            UseItem("TM24", "NIDOKING", "HORN ATTACK");
            UseItem("BICYCLE");

            /*
                Works like TalkTo, but uses Cut.
            */
            CutAt(19, 28);
            CutAt("Route9", 5, 8);

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
            TalkTo("RockTunnel1F", 23, 8);
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
        });

        CacheState("celadon", () => {
            // GAMBLER
            TalkTo("Route8", 46, 13);
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

            MoveTo("UndergroundPathWestEast", 47, 2);

            UseItem("BICYCLE");
            PickupItemAt(21, 5, Action.Down); // elixer

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

            MoveTo("CeladonCity", 8, 14);
            UseItem("BICYCLE");

            CutAt("Route16", 34, 9);
            MoveTo("Route16", 17, 4);
            UseItem("BICYCLE");
            TalkTo("Route16FlyHouse", 2, 3); // fly received

            MoveTo("Route16", 7, 6);
            ItemSwap("HELIX FOSSIL", "POKE DOLL");
            UseItem("TM07", "NIDOKING", "LEER");
            UseItem("HM02", "PIDGEY");
            Fly("LavenderTown");
        });

        CacheState("pokemontower", () => {
            // RIVAL 4
            MoveTo("PokemonTower2F", 15, 5);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH", Crit));

            // CHANNELER #1
            TalkTo("PokemonTower4F", 15, 7);
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));

            PickupItemAt(12, 10); // elixer

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

            PickupItemAt(6, 8); // rare candy
            MoveTo(10, 16);
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
            TalkTo(3, 1);
            ClearText(); // Pokeflute received

            MoveTo("LavenderTown", 7, 10);
            Fly("CeladonCity");
        });

        CacheState("safari", () => {
            UseItem("BICYCLE");

            MoveTo("Route16", 27, 10);
            ItemSwap("POKE DOLL", "ELIXER");
            UseItem("POKE FLUTE");
            RunAway();

            PickupItemAt("Route17", 8, 121); // max elixer

            MoveTo("Route18", 40, 8);
            UseItem("BICYCLE");

            CutAt("FuchsiaCity", 18, 19);
            CutAt(16, 11);
            MoveTo("SafariZoneGate", 3, 2);
            ClearText();
            Yes();
            ClearText();
            ClearText(); // sneaky joypad call

            UseItem("BICYCLE");
            PickupItemAt("SafariZoneWest", 19, 7, Action.Down); // gold teeth

            TalkTo("SafariZoneSecretHouse", 3, 3);
            MoveTo("SafariZoneWest", 3, 4);
            UseItem("ESCAPE ROPE");
        });

        CacheState("koga", () => {
            Fly("FuchsiaCity");
            UseItem("BICYCLE");

            // JUGGLER #1
            TalkTo("FuchsiaGym", 7, 8);
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH"));
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
            ForceTurn(new RbyTurn("ELIXER", "NIDOKING"), new RbyTurn("SELFDESTRUCT", Miss));

            MoveTo("FuchsiaCity", 5, 28);
            UseItem("BICYCLE");

            TalkTo("WardensHouse", 2, 3);

            MoveTo("FuchsiaCity", 27, 28);
        });

        CacheState("mansion", () => {
            Fly("PalletTown");
            MoveTo(4, 13, Action.Down);
            ItemSwap("NUGGET", "X SPEED");
            UseItem("HM03", "SQUIRTLE");
            UseItem("RARE CANDY", "NIDOKING");
            Surf();

            MoveTo("CinnabarIsland", 4, 4);

            TalkTo("PokemonMansion3F", 10, 5, Action.Up);
            ActivateMansionSwitch();

            MoveTo(16, 14);
            FallDown(); // TODO: look into not having to do this
            TalkTo("PokemonMansionB1F", 18, 25, Action.Up);
            ActivateMansionSwitch();

            TalkTo(20, 3, Action.Up);
            ActivateMansionSwitch();
            PickupItemAt(5, 13); // secret key
            UseItem("ESCAPE ROPE");
        });

        CacheState("silph", () => {
            Fly("CeladonCity");

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

            // SILPH GIOVANNI
            TalkTo(6, 13, Action.Up);
            MoveTo(6, 13);
            ClearText();
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            UseItem("ELIXER", "NIDOKING");
            UseItem("ESCAPE ROPE");
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
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM", Crit));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            UseItem("ESCAPE ROPE");
        });

        CacheState("erika", () => {
            Fly("CeladonCity");
            UseItem("BICYCLE");
            CutAt(35, 32);
            CutAt("CeladonGym", 2, 4);

            // BEAUTY
            MoveTo(3, 4);
            ClearText();
            ForceTurn(new RbyTurn("HORN DRILL"));

            // ERIKA
            TalkTo(4, 3);
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH", Crit));

            CutAt(5, 7);
            MoveTo("CeladonCity", 12, 28);
        });

        CacheState("sabrina", () => {
            Fly("SaffronCity");

            UseItem("BICYCLE");

            // SABRINA
            TalkTo("SaffronGym", 9, 8);
            ForceTurn(new RbyTurn("THRASH"));
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH", Crit));
            ForceTurn(new RbyTurn("THRASH", Crit), new RbyTurn("PSYWAVE", 26));

            MoveTo(1, 5);
            UseItem("ESCAPE ROPE");
        });

        CacheState("giovanni", () => {
            Fly("ViridianCity");

            UseItem("BICYCLE");

            // RHYHORN
            MoveTo("ViridianGym", 15, 5);
            ClearText();
            ForceTurn(new RbyTurn("BUBBLEBEAM"));

            // BLACKBELT
            MoveTo(10, 4);
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
            ForceTurn(new RbyTurn("BUBBLEBEAM"));

            MoveTo("ViridianCity", 32, 8);
        });

        CacheState("viridanrival", () => {
            ItemSwap("S.S.TICKET", "MAX ELIXER");
            UseItem("HM04", "SANDSHREW");
            UseItem("TM27", "NIDOKING", "THRASH");
            UseItem("BICYCLE");

            // VIRIDIAN RIVAL
            MoveTo("Route22", 29, 5);
            ClearText();
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("TAIL WHIP", Miss));
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("THUNDERBOLT"));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
        });

        CacheState("victoryroad", () => {
            MoveTo("Route22Gate", 4, 2, Action.Up);
            ClearText();
            MoveTo("Route23", 7, 139);
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
            UseItem("ELIXER", "NIDOKING");
            UseItem("BICYCLE");
            Execute("D R R U");
            PushBoulder(Joypad.Left, 14);

            TalkTo("IndigoPlateauLobby", 15, 8, Action.Up);

            // TODO: PC functions
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
        });

        CacheState("lorelei", () => {
            // LORELEI
            MoveTo("IndigoPlateauLobby", 8, 0);
            TalkTo("LoreleisRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("bruno", () => {
            // BRUNO
            Execute("U U U");
            TalkTo("BrunosRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("agatha", () => {
            // AGATHA
            Execute("U U U");
            TalkTo("AgathasRoom", 5, 2, Action.Right);
            ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("HYPNOSIS", Miss));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("FISSURE"));
        });

        CacheState("lance", () => {
            UseItem("MAX ELIXER", "NIDOKING");

            // LANCE
            Execute("U U U");
            MoveTo("LancesRoom", 6, 2);
            ClearText();
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("THUNDERBOLT", Crit), new RbyTurn("SUPERSONIC", Miss));
            ForceTurn(new RbyTurn("HORN DRILL"));
        });

        CacheState("champion", () => {
            // CHAMPION
            Execute("L U U U");
            ClearText();
            ForceTurn(new RbyTurn("X SPEED"), new RbyTurn("MIRROR MOVE", Miss));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("FISSURE"));
            ForceTurn(new RbyTurn("BUBBLEBEAM"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));
            ForceTurn(new RbyTurn("HORN DRILL"));

            ClearText();
        });

        Dispose();
    }
}