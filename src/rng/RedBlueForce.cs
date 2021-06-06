using System;
using System.Linq;
using System.Collections.Generic;

public enum MenuType {

    None,
    Party,
    Bag,
    Options,
    Fight,
    StartMenu,
    Mart,
}

public class RedBlueForce : RedBlue {

    public const int Miss = 0x40;
    public const int Crit = 0x80;
    public const int Effect = 0x100;
    public const int ThreeTurn = 0x200;
    public const int Hitself = 0x400;

    public const int WalkCost = 17;
    public const int BikeCo = 9;
    public const int LedgeHopCost = 40;
    public const int WarpCost = 100;
    public const int BonkCost = 8;

    public MenuType CurrentMenuType = MenuType.None;

    public RedBlueForce(string rom, bool speedup = false) : base(rom, speedup) {
    }

    public void ForceGiftDVs(ushort dvs) {
        RunUntil("_AddPartyMon.next4");
        A = dvs >> 8;
        B = dvs & 0xff;
    }

    public void ForceEncounter(Action action, int slotIndex, ushort dvs) {
        byte[] slots = new byte[] {
            0, 51, 102, 141, 166, 191, 216, 229, 242, 253
        };

        Inject((Joypad) action);
        CpuWrite("wGrassRate", 0xff);
        CpuWrite("wWaterRate", 0xff);
        Hold((Joypad) action, SYM["TryDoWildEncounter.CanEncounter"] + 3);
        A = 0x00;

        RunUntil(SYM["TryDoWildEncounter.CanEncounter"] + 8);
        A = slots[slotIndex];

        RunUntil(SYM["LoadEnemyMonData.storeDVs"]);
        A = dvs >> 8;
        B = dvs & 0xff;
    }

    public void ForceYoloball(string ballname) {
        ClearText();
        BattleMenu(0, 1);
        ChooseListItem(Bag.IndexOf(ballname));
        RunUntil(SYM["ItemUseBall.loop"] + 0x8);
        A = 1;
    }

    public void ForceTurn(RbyTurn playerTurn, RbyTurn enemyTurn = null, bool speedTieWin = true) {
        bool useItem = Items[playerTurn.Move] != null;
        if(useItem) {
            if(playerTurn.Pokemon != null) UseItem(playerTurn.Move, playerTurn.Pokemon);
            else UseItem(playerTurn.Move, playerTurn.Flags);
        } else if(!BattleMon.ThrashingAbout) {
            if(CurrentMenuType != MenuType.Fight) BattleMenu(0, 0);

            int moveIndex = FindMove(playerTurn.Move);

            // Reusing 'ChooseMenuItem' code, because the final AdvanceFrame advances past 'SelectEnemyMove.done', 
            // and I don't have a good solution for this problem right now.
            var scroll = CalcMenuScroll(moveIndex);
            for(int i = 0; i < scroll.Amount; i++) {
                MenuPress(scroll.Direction);
            }

            if(CpuRead("hJoyLast") == (byte) Joypad.A) Press(Joypad.None);
            Inject(Joypad.A);
        }

        Hold(Joypad.A, SYM["SelectEnemyMove.done"]);
        A = enemyTurn != null ? Moves[enemyTurn.Move].Id : 0;

        bool playerFirst;
        int speedtie = RunUntil(SYM["MainInBattleLoop.speedEqual"] + 9, SYM["MainInBattleLoop.enemyMovesFirst"], SYM["MainInBattleLoop.playerMovesFirst"]);
        if(speedtie == SYM["MainInBattleLoop.enemyMovesFirst"]) playerFirst = false;
        else if(speedtie == SYM["MainInBattleLoop.playerMovesFirst"]) playerFirst = true;
        else {
            A = speedTieWin ? 0x00 : 0xff;
            playerFirst = speedTieWin;
        }

        if(playerFirst) {
            if(!useItem) ForceTurnInternal(playerTurn);
            else RunUntil(SYM["MainInBattleLoop.playerMovesFirst"] + 6);
            if(enemyTurn != null) ForceTurnInternal(enemyTurn);
        } else {
            Debug.Assert(enemyTurn != null, "No enemy turn was specified even though the opponent moved first!");
            ForceTurnInternal(enemyTurn);
            if(!useItem) ForceTurnInternal(playerTurn);
        }

        CurrentMenuType = MenuType.None;

        // Semi-terrible code to get around thrash. TODO: fix
        if(BattleMon.ThrashingAbout) {
            if(EnemyMon.HP == 0) {
                if(EnemyParty.Where(mon => mon.HP > 0).Count() > 1) ClearTextUntil(Joypad.None, SYM["PlayCry"]);
                else ClearText();
            }
        } else {
            ClearText();
        }
    }

    private void ForceTurnInternal(RbyTurn turn) {
        int crit = SYM["CriticalHitTest.SkipHighCritical"] + 0x3;
        int accuracy = SYM["MoveHitTest.doAccuracyCheck"] + 0x3;
        int damageRoll = SYM["RandomizeDamage.loop"] + 0x8;
        int modifierDown = SYM["StatModifierDownEffect.statModifierDownEffect"] + 0xe;
        int ai = SYM["TrainerAI.getpointer"] + 0x6;
        int freezeBurnParalyze = SYM["FreezeBurnParalyzeEffect.next2"] + 0x4;
        int poison = SYM["PoisonEffect.sideEffectTest"] + 0x3;
        int playerConfusion = SYM["CheckPlayerStatusConditions.IsConfused"] + 0x12;
        int enemyConfusion = SYM["CheckEnemyStatusConditions.isConfused"] + 0x12;
        int thrash = SYM["ThrashPetalDanceEffect.thrashPetalDanceEffect"] + 0x5;
        int psywave = SYM["ApplyAttackToPlayerPokemon.loop"] + 0x3;
        int playerTurnDone1 = SYM["MainInBattleLoop.playerMovesFirst"] + 0x3;
        int playerTurnDone2 = SYM["MainInBattleLoop.AIActionUsedEnemyFirst"] + 0xc;
        int playerTurnDone3 = SYM["HandlePlayerMonFainted"];
        int enemyTurnDone1 = SYM["MainInBattleLoop.enemyMovesFirst"] + 0x11;
        int enemyTurnDone2 = SYM["MainInBattleLoop.playerMovesFirst"] + 0x27;
        int enemyTurnDone3 = SYM["HandleEnemyMonFainted"];

        int ret;
        do {
            while((ret = RunUntil(crit, accuracy, damageRoll, modifierDown, ai, freezeBurnParalyze, poison, playerConfusion, enemyConfusion, thrash, psywave, playerTurnDone1, playerTurnDone2, playerTurnDone3, enemyTurnDone1, enemyTurnDone2, enemyTurnDone3, SYM["ManualTextScroll"])) == SYM["ManualTextScroll"]) {
                Joypad joypad = (Joypad) CpuRead("hJoyLast");
                if(joypad == Joypad.None) joypad = Joypad.A;
                joypad ^= (Joypad.A | Joypad.B);
                Inject(joypad);
                RunFor(1);
            }

            if(ret == accuracy) {
                A = (turn.Flags & Miss) != 0 ? 0xff : 0x00;
            } else if(ret == crit) {
                A = (turn.Flags & Crit) != 0 ? 0x00 : 0xff;
            } else if(ret == damageRoll) {
                int roll = turn.Flags & 0x3f;
                if(roll < 1) roll = 1;
                if(roll > 39) roll = 39;
                A = 216 + roll;
            } else if(ret == ai) {
                A = 0xff; // all AI is ignored for now
            } else if(ret == freezeBurnParalyze || ret == poison || ret == modifierDown) {
                A = (turn.Flags & Effect) != 0 ? 0x00 : 0xff;
            } else if(ret == playerConfusion || ret == enemyConfusion) {
                A = (turn.Flags & Hitself) > 0 ? 0xff : 0x00;
            } else if(ret == thrash) {
                A = (turn.Flags & ThreeTurn) > 0 ? 0 : 1;
            } else if(ret == psywave) {
                A = turn.Flags & 0x3f;
            }

            RunFor(1);
        } while(!(ret == playerTurnDone1 || ret == playerTurnDone2 || ret == playerTurnDone3 || ret == enemyTurnDone1 || ret == enemyTurnDone2 || ret == enemyTurnDone3));
    }

    public void ForceCan() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["GymTrashScript.ok"] + 0xb, SYM["GymTrashScript.trySecondLock"] + 0x7);
        B = A;
        ClearText();
    }

    public override void ChooseMenuItem(int target) {
        Scroll(CalcMenuScroll(target), Joypad.A);
    }

    public override void SelectMenuItem(int target) {
        Scroll(CalcMenuScroll(target), Joypad.Select);
    }

    public override void ChooseListItem(int target) {
        Scroll(CalcListScroll(target), Joypad.A);
    }

    public override void SelectListItem(int target) {
        Scroll(CalcListScroll(target), Joypad.Select);
    }

    public (Joypad Direction, int Amount) CalcMenuScroll(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        bool inStartMenu = CpuReadLE<ushort>(Registers.SP + 6) == SYM["RedisplayStartMenu.loop"] + 0x3;
        int current = CpuRead("wCurrentMenuItem");
        int max = CpuRead("wMaxMenuItem");
        bool wrap = CpuRead("wMenuWrappingEnabled") > 0;

        if(inStartMenu) {
            max--;
            wrap = true;
        }

        return CalcScroll(target, current, max, wrap);
    }

    public (Joypad Direction, int Amount) CalcListScroll(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        return CalcScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);
    }

    public void Scroll((Joypad Direction, int Amount) scroll, Joypad finalInput) {
        for(int i = 0; i < scroll.Amount; i++) MenuPress(scroll.Direction);
        MenuPress(finalInput);
    }

    public int ClearTextUntilNaive(Joypad x, params int[] addrs) {
        int[] breakpoints = new int[addrs.Length + 1];
        breakpoints[0] = SYM["ManualTextScroll"];
        Array.Copy(addrs, 0, breakpoints, 1, addrs.Length);

        int ret;
        while((ret = RunUntil(breakpoints)) == SYM["ManualTextScroll"]) {
            Joypad joypad = (Joypad) CpuRead("hJoyLast");
            if(joypad == Joypad.None) joypad = Joypad.A;
            joypad ^= (Joypad.A | Joypad.B);
            Inject(joypad);
            RunFor(1);
        }

        return ret;
    }

    // Temporary non-generic pathfinding code as the generic code had too many issues and became a hassle to maintain.
    public List<Action> TempFindPath(RbyTile startTile, RbyTile endTile, Action preferredEndDirection = Action.None, params RbyTile[] additionallyBlockedTiles) {
        byte[] overworldMap = ReadOverworldTiles();

        Dictionary<RbyTile, int> costs = new Dictionary<RbyTile, int>();
        Dictionary<RbyTile, RbyTile> previousTiles = new Dictionary<RbyTile, RbyTile>();
        Dictionary<RbyTile, Action[]> previousActions = new Dictionary<RbyTile, Action[]>();
        Queue<RbyTile> tileQueue = new Queue<RbyTile>();

        byte walkBikeSurfState = CpuRead("wWalkBikeSurfState");
        bool biking = walkBikeSurfState == 1;
        bool surfing = walkBikeSurfState == 2;

        costs[startTile] = 0;
        tileQueue.Enqueue(startTile);

        while(tileQueue.Count > 0) {
            RbyTile currentTile = tileQueue.Dequeue();

            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                RbyTile neighborTile = GetNeighbor(currentTile, action);

                bool ledgeHop = LedgeCheck(currentTile, neighborTile, action);
                if(ledgeHop) neighborTile = GetNeighbor(neighborTile, action);

                if(neighborTile == null || (additionallyBlockedTiles != null && Array.IndexOf(additionallyBlockedTiles, neighborTile) != -1)) continue;

                RbyTileset tileset = neighborTile.Map.Tileset;
                bool landCollision = !CollisionCheck(overworldMap, startTile, endTile, currentTile, neighborTile, tileset.LandPermissions, tileset.TilePairCollisionsLand);

                if(!surfing) {
                    if(landCollision) continue;
                } else {
                    bool waterCollision = !CollisionCheck(overworldMap, startTile, endTile, currentTile, neighborTile, tileset.WaterPermissions, tileset.TilePairCollisionsWater);
                    if(waterCollision && !(!landCollision && neighborTile == endTile)) continue;
                }

                var warp = WarpCheck(neighborTile);
                bool isWarp = warp.TileToWarpTo != null;
                bool directionalWarp = warp.ActionRequired != Action.None;

                AddNewState(isWarp ? warp.TileToWarpTo : neighborTile, isWarp, directionalWarp, ledgeHop, warp.ActionRequired);
                if(directionalWarp || (isWarp && neighborTile == endTile)) AddNewState(neighborTile, false, false, false, Action.None);

                void AddNewState(RbyTile newTile, bool isWarp, bool isDirectionalWarp, bool isLedgeHop, Action directionalWarpAction) {
                    // Doors automatically move you 1 tile down
                    if(isWarp && DoorCheck(newTile)) {
                        newTile = GetNeighbor(newTile, Action.Down);
                    }

                    int cost = CalcStepCost(startTile, biking, isLedgeHop, isWarp, newTile, action);
                    if(isDirectionalWarp && directionalWarpAction != action) cost += BonkCost;

                    int newCost = costs[currentTile] + cost;
                    if(costs.ContainsKey(endTile) && newCost > costs[endTile] + WarpCost) return;

                    if(!costs.ContainsKey(newTile) || costs[newTile] > newCost) {
                        costs[newTile] = newCost;
                        previousTiles[newTile] = currentTile;
                        previousActions[newTile] = isDirectionalWarp ? new Action[] { action, directionalWarpAction } : new Action[] { action };
                        tileQueue.Enqueue(newTile);
                    }
                }
            }
        }

        Action endAction = Action.None;

        RbyTile newEndTile = null;
        if(!costs.ContainsKey(endTile)) {
            int minCost = int.MaxValue;
            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                if(preferredEndDirection != Action.None && action != preferredEndDirection.Opposite()) continue;

                RbyTile t1 = endTile.Neighbor(action);
                if(t1 != null && costs.ContainsKey(t1)) {
                    int cost = costs[t1] + BonkCost;
                    if(minCost > cost) {
                        minCost = cost;
                        newEndTile = t1;
                        endAction = action.Opposite();
                    }

                    RbyTile t2 = t1.Neighbor(action);
                    if(t2 != null && costs.ContainsKey(t2)) {
                        cost = costs[t2] + CalcStepCost(startTile, biking, false, false, t2, action);
                        if(minCost > cost) {
                            minCost = cost;
                            newEndTile = t2;
                            endAction = action.Opposite();
                        }
                    }
                }
            }
        } else if(preferredEndDirection != Action.None) {
            endAction = preferredEndDirection;
            newEndTile = endTile.Neighbor(preferredEndDirection.Opposite());
        }

        if(newEndTile != null) {
            endTile = newEndTile;
        }

        List<Action> path = new List<Action>();
        RbyTile tile = endTile;
        while(tile != startTile) {
            path.AddRange(previousActions[tile]);
            tile = previousTiles[tile];
        }

        path.Reverse();
        if(endAction != Action.None) path.Add(endAction);

        return path;
    }

    public byte[] ReadOverworldTiles() {
        RbyMap map = Map;
        int width = map.Width;
        int height = map.Height;

        byte[] overworldMap = From("wOverworldMap").Read(1300);
        byte[] blocks = new byte[width * height];

        for(int i = 0; i < height; i++) {
            Array.Copy(overworldMap, (i + 3) * (width + 6) + 3, blocks, i * width, width);
        }

        return map.Tileset.GetTiles(blocks, width);
    }

    public int CalcStepCost(RbyTile startTile, bool initialOnBike, bool ledgeHop, bool warp, RbyTile target, Action action) {
        if(ledgeHop) return LedgeHopCost;
        if(warp) return WarpCost;

        bool cyclingRoad = target.Map.Id == 28 || (target.Map.Id == 27 && target.Y >= 10 && target.X >= 23);
        bool onBike = startTile.Map.Id == target.Map.Id ? initialOnBike : target.Map.Tileset.AllowBike;

        if(cyclingRoad) {
            return action == Action.Down ? BikeCo : WalkCost;
        } else {
            return onBike ? BikeCo : WalkCost;
        }
    }

    public RbyTile GetNeighbor(RbyTile tile, Action action) {
        RbyMap map = tile.Map;
        RbyConnection connection = null;
        if(action == Action.Right && tile.X == map.Width * 2 - 1) connection = map.Connections[0];
        if(action == Action.Left && tile.X == 0) connection = map.Connections[1];
        if(action == Action.Down && tile.Y == map.Height * 2 - 1) connection = map.Connections[2];
        if(action == Action.Up && tile.Y == 0) connection = map.Connections[3];

        int xd;
        int yd;
        if(connection != null) {
            if(action == Action.Down || action == Action.Up) {
                xd = (tile.X + connection.XAlignment) & 0xff;
                yd = connection.YAlignment;
            } else {
                xd = connection.XAlignment;
                yd = (tile.Y + connection.YAlignment) & 0xff;
            }

            return map.Game.Maps[connection.MapId][xd, yd];
        } else {
            xd = tile.X;
            yd = tile.Y;
            switch(action) {
                case Action.Right: xd++; break;
                case Action.Left: xd--; break;
                case Action.Down: yd++; break;
                case Action.Up: yd--; break;
            }
            return tile.Map[xd, yd];
        }
    }

    public bool DoorCheck(RbyTile target) {
        return Array.IndexOf(target.Map.Tileset.DoorTiles, target.Collision) != -1;
    }

    public bool LedgeCheck(RbyTile src, RbyTile ledgeTile, Action action) {
        return src != null && ledgeTile != null &&
               src.Map.Game.Ledges.Any(ledge => ledge.Source == src.Collision && ledge.Ledge == ledgeTile.Collision && ledge.ActionRequired == action);
    }

    public bool CollisionCheck(byte[] overworldMap, RbyTile startTile, RbyTile endTile, RbyTile src, RbyTile dest, PermissionSet permissions, List<int> tilePairCollisions) {
        if(dest == null) return false;
        if(!IsTilePassable(overworldMap, startTile, src, dest, permissions, tilePairCollisions)) return false;
        if(IsCollidingWithSprite(dest)) return false;
        if(dest != endTile && IsMovingIntoTrainerVision(dest)) return false; // allow moving into trainer vision on the end tile
        if(BlockSpinningTiles(dest)) return false;
        return true;
    }

    public bool IsTilePassable(byte[] overworldMap, RbyTile startTile, RbyTile src, RbyTile dest, PermissionSet permissions, List<int> tilePairCollisions) {
        byte destCollision;
        if(startTile.Map.Id == dest.Map.Id) {
            destCollision = overworldMap[(dest.X * 2) + (dest.Y * 2) * dest.Map.Width * 4 + dest.Map.Width * 4];
        } else {
            destCollision = dest.Collision;
        }

        if(!permissions.IsAllowed(destCollision)) return false;
        if(tilePairCollisions.Contains(src.Collision << 8 | destCollision)) return false;

        return true;
    }

    public bool IsCollidingWithSprite(RbyTile dest) {
        if(dest.Map == Map) {
            for(int spriteIndex = 1; spriteIndex < 16; spriteIndex++) {
                RbySprite sprite = dest.Map.Sprites[spriteIndex - 1];
                if(!IsSpriteHidden(sprite)) {
                    int spriteX = CpuRead(0xc205 | (spriteIndex << 4)) - 4;
                    int spriteY = CpuRead(0xc204 | (spriteIndex << 4)) - 4;
                    if(spriteX == dest.X && spriteY == dest.Y) return true;
                }
            }
        } else {
            foreach(RbySprite sprite in dest.Map.Sprites) {
                if(!IsSpriteHidden(sprite) && sprite.X == dest.X && sprite.Y == dest.Y) return true;
            }
        }

        return false;
    }

    public bool IsSpriteHidden(RbySprite sprite) {
        if(sprite == null) return false;

        return sprite.CanBeMissable && (CpuRead(sprite.MissableAddress) & (1 << sprite.MissableBit)) > 0;
    }

    public bool BlockSpinningTiles(RbyTile dest) {
        // TODO: Handle them properly ecks dee
        return dest.Map.Tileset.Id == 7 && (dest.Collision == 0x3c || dest.Collision == 0x3d || dest.Collision == 0x4c || dest.Collision == 0x4d);
    }

    public bool IsMovingIntoTrainerVision(RbyTile tile) {
        foreach(RbyTrainer trainer in tile.Map.Trainers) {
            if((CpuRead(trainer.EventFlagAddress) & (1 << trainer.EventFlagBit)) != 0) continue;

            int range = trainer.SightRange;
            if(trainer.Direction == Action.Down && tile.Y - trainer.Y == 4) range--;

            RbyTile current = trainer.Map[trainer.X, trainer.Y];
            for(int i = 0; i < range && current != null; i++) {
                current = current.Neighbor(trainer.Direction);
                if(current == tile) {
                    return true;
                }
            }
        }

        return false;
    }

    public (RbyTile TileToWarpTo, Action ActionRequired) WarpCheck(RbyTile warpTile) {
        RbyWarp srcWarp = warpTile.Map.Warps[warpTile.X, warpTile.Y];
        if(srcWarp != null) {
            RbyMap destMap = Maps[srcWarp.DestinationMap];
            if(destMap != null) {
                RbyWarp destWarp = destMap.Warps[srcWarp.DestinationIndex];
                if(destWarp != null) {
                    RbyTile destTile = destMap[destWarp.X, destWarp.Y];
                    if(Array.IndexOf(warpTile.Map.Tileset.WarpTiles, warpTile.Collision) != -1) return (destTile, Action.None);
                    else {
                        Action action = ExtraWarpCheck(warpTile);
                        if(action != Action.None) {
                            return (destTile, action);
                        }
                    }
                }
            }
        }

        return (null, Action.None);
    }

    public Action ExtraWarpCheck(RbyTile warpTile) {
        byte map = warpTile.Map.Id;
        byte tileset = warpTile.Map.Tileset.Id;

        // https://github.com/pret/pokered/blob/master/home/overworld.asm#L719-L747
        if(map == 0x61) return EdgeOfMapWarpCheck(warpTile);
        else if(map == 0xc7) return DirectionalWarpCheck(warpTile);
        else if(map == 0xc8) return DirectionalWarpCheck(warpTile);
        else if(map == 0xca) return DirectionalWarpCheck(warpTile);
        else if(map == 0x52) return DirectionalWarpCheck(warpTile);
        else if(tileset == 0) return DirectionalWarpCheck(warpTile);
        else if(tileset == 0xd) return DirectionalWarpCheck(warpTile);
        else if(tileset == 0xe) return DirectionalWarpCheck(warpTile);
        else if(tileset == 0x17) return DirectionalWarpCheck(warpTile);
        else return EdgeOfMapWarpCheck(warpTile);
    }

    public Action EdgeOfMapWarpCheck(RbyTile warpTile) {
        if(warpTile.X == 0) return Action.Left;
        else if(warpTile.X == warpTile.Map.Width * 2 - 1) return Action.Right;
        else if(warpTile.Y == 0) return Action.Up;
        else if(warpTile.Y == warpTile.Map.Height * 2 - 1) return Action.Down;

        return Action.None;
    }

    public Action DirectionalWarpCheck(RbyTile warpTile) {
        for(int i = 0; i < 4; i++) {
            Action action = (Action) (0x10 << i);
            RbyTile neighbor = warpTile.Neighbor(action);
            byte collision;
            if(neighbor == null) {
                int offs;
                if(action == Action.Up) offs = 2 + warpTile.X % 2;
                else if(action == Action.Down) offs = warpTile.X % 2;
                else if(action == Action.Right) offs = (warpTile.Y % 2) * 2;
                else offs = 1 + (warpTile.Y % 2) * 2;

                RbyMap map = warpTile.Map;
                collision = ROM[(warpTile.Map.Tileset.Bank << 16 | map.Tileset.BlockPointer) + map.BorderBlock * 16 + offs * 4 + 3];
            } else {
                collision = neighbor.Collision;
            }
            if(Array.IndexOf(DirectionalWarpTiles[action], collision) != -1) return action;
        }

        return Action.None;
    }

    public int MoveTo(int map, int x, int y, Action preferredDirection = Action.None) {
        return MoveTo(Maps[map][x, y], preferredDirection);
    }

    public int MoveTo(string map, int x, int y, Action preferredDirection = Action.None) {
        return MoveTo(Maps[map][x, y], preferredDirection);
    }

    public override int MoveTo(int targetX, int targetY, Action preferredDirection = Action.None) {
        return MoveTo(Map[targetX, targetY], preferredDirection);
    }

    public int MoveTo(RbyTile target, Action preferredDirection = Action.None, params RbyTile[] additionallyBlockedTiles) {
        List<Action> path = TempFindPath(Tile, target, preferredDirection, additionallyBlockedTiles);
        return Execute(path.ToArray());
    }

    public void TalkTo(int map, int x, int y, Action preferredDirection = Action.None) {
        TalkTo(Maps[map][x, y], preferredDirection);
    }

    public void TalkTo(string map, int x, int y, Action preferredDirection = Action.None) {
        TalkTo(Maps[map][x, y], preferredDirection);
    }

    public void TalkTo(int targetX, int targetY, Action preferredDirection = Action.None) {
        TalkTo(Map[targetX, targetY], preferredDirection);
    }

    public void TalkTo(RbyTile target, Action preferredDirection = Action.None) {
        MoveTo(target, preferredDirection);
        Press(Joypad.A);
        ClearText();
    }

    public void PickupItemAt(int map, int x, int y, Action preferredDirection = Action.None) {
        PickupItemAt(Maps[map][x, y], preferredDirection);
    }

    public void PickupItemAt(string map, int x, int y, Action preferredDirection = Action.None) {
        PickupItemAt(Maps[map][x, y], preferredDirection);
    }

    public void PickupItemAt(int targetX, int targetY, Action preferredDirection = Action.None) {
        PickupItemAt(Map[targetX, targetY], preferredDirection);
    }

    public void PickupItemAt(RbyTile target, Action preferredDirection = Action.None) {
        MoveTo(target, preferredDirection, target);
        PickupItem();
    }

    public void CutAt(int map, int x, int y, Action preferredDirection = Action.None) {
        CutAt(Maps[map][x, y], preferredDirection);
    }

    public void CutAt(string map, int x, int y, Action preferredDirection = Action.None) {
        CutAt(Maps[map][x, y], preferredDirection);
    }

    public void CutAt(int targetX, int targetY, Action preferredDirection = Action.None) {
        CutAt(Map[targetX, targetY], preferredDirection);
    }

    public void CutAt(RbyTile target, Action preferredDirection = Action.None) {
        MoveTo(target, preferredDirection);
        Cut();
    }

    public override int Execute(params Action[] actions) {
        CloseMenu();

        int ret = 0;

        bool directionalWarp = false;
        Action previous = Action.None;

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    CpuWrite("wGrassRate", 0);
                    CpuWrite("wWaterRate", 0);

                    Joypad joypad = (Joypad) action;
                    if(directionalWarp && previous == action) {
                        Inject(joypad);
                    } else {
                        if(CpuReadLE<ushort>(SP) != SYM["JoypadOverworld"] + 0xd) RunUntil(SYM["JoypadOverworld"] + 0xa);

                        Inject(joypad);
                        bool turnframe = CpuRead("wCheckFor180DegreeTurn") == 1;
                        while((ret = Hold(turnframe ? joypad : Joypad.None, SYM["OverworldLoopLessDelay.newBattle"] + 3, SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["TryWalking"])) == SYM["TryWalking"]) {
                            D = 0x00;
                            E = 0x00;
                            RunFor(1);
                        }

                        if(turnframe && (ret == SYM["CollisionCheckOnLand.collision"] || ret == SYM["CollisionCheckOnWater.collision"])) {
                            RunUntil(SYM["JoypadOverworld"]);
                        }
                    }

                    do {
                        RunFor(1);
                        ret = RunUntil(SYM["JoypadOverworld"], SYM["CheckWarpsNoCollisionLoop"] + 0x2d, SYM["PrintLetterDelay"], SYM["TryWalking"]);
                        if(ret == SYM["CheckWarpsNoCollisionLoop"] + 0x2d) {
                            directionalWarp = true;
                            Inject(Joypad.None);
                            break;
                        } else if(ret == SYM["PrintLetterDelay"]) {
                            break;
                        } else if(ret == SYM["TryWalking"]) {
                            D = 0x00;
                            E = 0x00;
                        } else {
                            directionalWarp = false;
                        }
                    } while((CpuRead("wd730") & 0xa0) > 0);
                    break;
            }
            previous = action;
        }

        return ret;
    }

    // FAST   MEDIUM   SLOW     0   1   2
    // ON              OFF      0       1
    // SHIFT           SET      0       1
    public void SetOptions(int textSpeed, int animations, int battleStyle) {
        OpenOptions();
        byte options = CpuRead("wOptions");

        int curTextSpeed = (options & 7) >> 1;
        int curAnimations = options & 0x40;
        int curBattleStyle = options & 0x20;

        if(textSpeed != curTextSpeed) MenuPress(textSpeed > curTextSpeed ? Joypad.Right : Joypad.Left);

        if(animations != curAnimations || battleStyle != curBattleStyle) {
            MenuPress(Joypad.Down);
            if(animations != curAnimations) MenuPress(animations > curAnimations ? Joypad.Right : Joypad.Left);

            if(battleStyle != curBattleStyle) {
                MenuPress(Joypad.Down);
                MenuPress(battleStyle > curBattleStyle ? Joypad.Right : Joypad.Left);
            }
        }
    }

    public void Buy(params object[] itemsToBuy) {
        ChooseMenuItem(0);
        ClearText();

        RAMStream stream = From("wItemList");
        byte[] mart = stream.Read(stream.u8());

        for(int i = 0; i < itemsToBuy.Length; i += 2) {
            byte item = Items[itemsToBuy[i].ToString()].Id;
            int quantity = (int) itemsToBuy[i + 1];

            int itemSlot = Array.IndexOf(mart, item);
            ChooseListItem(itemSlot);
            for(int j = 1; j < quantity; j++) MenuPress(Joypad.Up);
            MenuPress(Joypad.A);
            ClearText();
            MenuPress(Joypad.A);
            ClearText();
        }

        MenuPress(Joypad.B);
        ClearText();

        CurrentMenuType = MenuType.Mart;
    }

    public void Sell(params object[] itemsToSell) {
        ChooseMenuItem(1);
        ClearText();
        Press(Joypad.None); // BAD CONSIDER NOT USING ANY MENUING STRATS FOR COMPARISONS??? SHIT'S ANNOYING

        for(int i = 0; i < itemsToSell.Length; i += 2) {
            string item = itemsToSell[i].ToString();
            int quantity = (int) itemsToSell[i + 1];

            ChooseListItem(Bag.IndexOf(item));
            if(quantity == 0) MenuPress(Joypad.Down);
            else {
                for(int j = 1; j < quantity; j++) MenuPress(Joypad.Up);
            }
            MenuPress(Joypad.A);
            ClearText();
            MenuPress(Joypad.A);
            ClearText();
        }

        MenuPress(Joypad.B);
        ClearText();

        CurrentMenuType = MenuType.Mart;
    }

    public void OpenStartMenu() {
        if(CurrentMenuType != MenuType.None) {
            MenuPress(Joypad.B);
        } else if(CurrentMenuType != MenuType.StartMenu) {
            MenuPress(Joypad.Start);
            CurrentMenuType = MenuType.StartMenu;
        }
    }

    public void CloseMenu() {
        if(CurrentMenuType != MenuType.None) {
            if(CurrentMenuType == MenuType.Mart) {
                MenuPress(Joypad.B);
                ClearText();
            } else {
                if(CurrentMenuType != MenuType.StartMenu) MenuPress(Joypad.B);
                MenuPress(Joypad.Start);
            }
        }
        CurrentMenuType = MenuType.None;
    }

    public void EndMenu() {
        if(CurrentMenuType != MenuType.None) MenuPress(Joypad.B);
        MenuPress(Joypad.Start);
        CurrentMenuType = MenuType.None;
    }

    private void OpenParty() {
        if(CurrentMenuType == MenuType.Party) return;
        OpenStartMenu();

        if(InBattle) BattleMenu(1, 0);
        else ChooseMenuItem(1);  // TODO: Pokedex obtained flag check?

        CurrentMenuType = MenuType.Party;
    }

    private void OpenBag() {
        if(CurrentMenuType == MenuType.Bag) return;
        OpenStartMenu();

        if(InBattle) BattleMenu(0, 1);
        else ChooseMenuItem(2);  // TODO: Pokedex obtained flag check?

        CurrentMenuType = MenuType.Bag;
    }

    private void OpenOptions() {
        if(CurrentMenuType == MenuType.Options) return;
        OpenStartMenu();
        ChooseMenuItem(4);  // TODO: Pokedex obtained flag check?
        CurrentMenuType = MenuType.Options;
    }

    public int PartyIndex(string mon) {
        RbyPokemon[] party = Party;
        return Array.IndexOf(party, party.Where(p => p.Species.Name == mon).First());
    }

    public new void PartySwap(int mon1, int mon2) {
        OpenParty();
        ChooseMenuItem(mon1);
        ChooseMenuItem(1);
        ChooseMenuItem(mon2);
    }

    public void PartySwap(string mon1, string mon2) {
        PartySwap(PartyIndex(mon1), PartyIndex(mon2));
    }

    public void ItemSwap(string item1, string item2) {
        ItemSwap(Bag.IndexOf(item1), Bag.IndexOf(item2));
    }

    public new void ItemSwap(int item1, int item2) {
        OpenBag();
        SelectListItem(item1);
        SelectListItem(item2);
    }

    public void UseItem(string name, int target1 = -1, int target2 = -1) {
        UseItem(Items[name], target1, target2);
    }

    public void UseItem(string name, string target1, string target2 = "") {
        int partyIndex = PartyIndex(target1);
        int slotIndex = -1;
        if(target2 != "") {
            RbyPokemon mon = Party[partyIndex];
            slotIndex = Array.IndexOf(mon.Moves, mon.Moves.Where(m => m != null && m.Name == target2).First());
        }
        UseItem(Items[name], partyIndex, slotIndex);
    }

    public void UseItem(RbyItem item, int target1 = -1, int target2 = -1) {
        OpenBag();

        ChooseListItem(Bag.IndexOf(item));

        switch(item.ExecutionPointerLabel) {
            case "ItemUseEvoStone": // Can only be used outside of battle
                ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                RunUntil("Evolution_PartyMonLoop.done");
                break;
            case "ItemUseTMHM": // Can only be used outside of battle
                int numMoves = PartyMon(target1).NumMoves;
                ChooseMenuItem(0); // USE
                ClearText();
                MenuPress(Joypad.A); // Do you want to teach?
                ChooseMenuItem(target1);
                ClearText();
                if(numMoves == 4) {
                    MenuPress(Joypad.A); // Do you want to overwrite?
                    ClearText();
                    ChooseMenuItem(target2); // Which move to overwrite?
                    ClearText();
                }
                break;
            case "ItemUseEscapeRope": // Can only be used outside of battle
                ChooseMenuItem(0); // USE
                RunUntil("DisableLCD");
                RunUntil("JoypadOverworld");
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUseBicycle": // Can only be used outside of battle
                ClearText();
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUsePokedoll": // Can only be used in battle
                ClearText();
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUsePokeflute":
                if(!InBattle) ChooseMenuItem(0); // USE
                ClearText();
                if(!InBattle) ClearText();
                break;
            case "ItemUseVitamin": // Can only be used outside of battle
                if(item.Name == "RARE CANDY") {
                    ChooseMenuItem(0); // USE
                    ChooseMenuItem(target1);
                    ClearText();
                } else {
                    // TODO: Implement
                }
                break;
            case "ItemUsePPRestore":
                if(!InBattle) ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                RunUntil("ManualTextScroll");
                Inject(Joypad.B);
                if(!InBattle) {
                    AdvanceFrame(Joypad.B);
                    RunUntil("Joypad");
                }
                break;
            case "ItemUseXAccuracy":
            case "ItemUseXStat": // Can only be used in battle
                RunUntil("DoneText");
                Inject(Joypad.B);
                RunUntil("ManualTextScroll");
                Inject(Joypad.B);
                break;
        }

        if(InBattle) CurrentMenuType = MenuType.None;
    }

    public void MoveSwap(int move1, int move2) {
        if(CurrentMenuType != MenuType.Fight) BattleMenu(0, 0);
        SelectMenuItem(move1);
        SelectMenuItem(move2);
        CurrentMenuType = MenuType.Fight;
    }

    public void MoveSwap(string move1, string move2) {
        MoveSwap(FindMove(move1), FindMove(move2));
    }

    public int FindMove(string move) {
        return Array.IndexOf(BattleMon.Moves, Moves[move]);
    }

    public void TeachLevelUpMove(string moveToOverwrite) {
        TeachLevelUpMove(FindMove(moveToOverwrite));
    }

    public void TeachLevelUpMove(int slot) {
        MenuPress(Joypad.A);
        ClearText();
        ChooseMenuItem(slot);
        ClearText();
    }

    public void Cut() {
        UseOverworldMove("CUT");
        ClearText();
        byte direction = CpuRead("wPlayerDirection");
        switch(direction) {
            case 0x1: Execute("R"); break;
            case 0x2: Execute("L"); break;
            case 0x4: Execute("D"); break;
            case 0x8: Execute("U"); break;
        }
    }

    public void Surf() {
        UseOverworldMove("SURF");
        ClearText();
    }

    public void Strength() {
        UseOverworldMove("STRENGTH");
        ClearText();
    }

    public void Fly(Joypad direction, int amount) {
        UseOverworldMove("FLY");
        for(int i = 0; i < amount; i++) MenuPress(direction);
        MenuPress(Joypad.A);
        RunUntil("DisableLCD");
        RunFor(1);
        RunUntil("DisableLCD");
        while(RunUntil("TryWalking", "Joypad") == SYM["TryWalking"]) {
            D = 0x00;
            E = 0x00;
            RunFor(1);
        }
    }

    public void UseOverworldMove(string name) {
        string[] overworldMoves = {
            "CUT",
            "SURF",
            "STRENGTH",
            "FLY",
            "TELEPORT",
            "DIG",
            "FLASH"
        };

        int partyIndex = 0;
        int moveIndex = 0;
        for(int i = 0; i < PartySize && partyIndex == 0; i++) {
            RbyPokemon partyMon = PartyMon(i);
            moveIndex = 0;
            for(int j = 0; j < 4; j++) {
                RbyMove move = partyMon.Moves[j];
                if(move == null) continue;
                if(overworldMoves.Contains(move.Name)) {
                    if(move.Name == name) {
                        partyIndex = i;
                        break;
                    }
                    moveIndex++;
                }
            }
        }

        OpenParty();
        ChooseMenuItem(partyIndex);
        ChooseMenuItem(moveIndex);
        CurrentMenuType = MenuType.None;
    }

    public void Evolve() {
        RunUntil("Evolution_PartyMonLoop.done");
        RunUntil(SYM["JoypadOverworld"] + 0xa);
    }

    public void RunAway() {
        BattleMenu(1, 1);
        ClearText();
    }

    public void FallDown() {
        RunUntil("DisableLCD");
        RunUntil("JoypadOverworld");
    }

    public void ActivateMansionSwitch() {
        MenuPress(Joypad.A);
        ClearText();
    }

    public void BlaineQuiz(Joypad joypad) {
        MenuPress(joypad);
        ClearText();
    }

    public void PushBoulder(Joypad joypad) {
        int encounterCheck = SYM["TryDoWildEncounter.CanEncounter"] + 3;
        while(Hold(joypad, 0x14f41, encounterCheck) == encounterCheck) {
            A = 0xff;
            RunFor(1);
        }
    }

    public void Yes() {
        MenuPress(Joypad.A);
    }

    public void No() {
        MenuPress(Joypad.B);
    }
}