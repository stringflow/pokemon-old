using System;
using System.Linq;

public partial class Rby {

    // Code ported from route one. (https://github.com/HRoll/poke-router/blob/master/src/DamageCalculator.java)
    public int CalcDamage(RbyPokemon attacker, RbyPokemon defender, RbyMove move, int damageRoll, bool crit) {
        damageRoll = Math.Clamp(damageRoll, 217, 255);

        if(move.Power == 0) return 0;

        bool special = move.Type.IsSpecial();
        int attackUnmodified = special ? attacker.UnmodifiedSpecial : attacker.UnmodifiedAttack;
        int attack = special ? attacker.Special : attacker.Attack;
        int defenseUnmodified = special ? defender.UnmodifiedSpecial : defender.UnmodifiedDefense;
        int defense = special ? defender.Special : defender.Defense;

        if(move.Name == "SELFDESTRUCT" || move.Name == "EXPLOSION") {
            defenseUnmodified = Math.Max(defenseUnmodified / 2, 1);
            defense = Math.Max(defense / 2, 1);
        }

        bool stab = attacker.Species.Type1 == move.Type || attacker.Species.Type2 == move.Type;

        int damage = ((attacker.Level * (crit ? 2 : 1)) & 0xff) * 2 / 5 + 2;
        damage *= crit ? attackUnmodified : attack;
        damage *= move.Power;
        damage /= 50;
        damage /= crit ? defenseUnmodified : defense;
        damage += 2;
        if(stab) damage = damage * 3 / 2;
        damage = damage * move.Game.GetTypeEffectiveness(move.Type, defender.Species.Type1) / 10;
        damage = damage * move.Game.GetTypeEffectiveness(move.Type, defender.Species.Type2) / 10;

        if(damage == 0) {
            return 0;
        }

        damage *= damageRoll;
        damage /= 255;

        return Math.Max(damage, 1);
    }

    public int[] CalcDamage(RbyPokemon attacker, RbyPokemon defender, RbyMove move, bool crit) {
        int[] ret = new int[39];
        for(int i = 0; i < ret.Length; i++) {
            ret[i] = CalcDamage(attacker, defender, move, i + 217, crit);
        }
        return ret;
    }

    public float OneShotPercentage(RbyPokemon attacker, RbyPokemon defender, RbyMove move, bool crit) {
        int[] damageRolls = CalcDamage(attacker, defender, move, crit);
        return (float) damageRolls.Where(dmg => dmg >= defender.HP).Count() / (float) damageRolls.Length;
    }

    // TODO: n shot percentage, one shot percentage with crits factored in, etc.
}