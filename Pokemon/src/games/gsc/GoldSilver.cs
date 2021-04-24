namespace Pokemon
{
    public class GoldSilver : Gsc
    {

        public GoldSilver(string rom, bool speedup = false) : base(rom, speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
    }

    public class Gold : GoldSilver
    {

        public Gold(bool speedup = false) : base("roms/pokegold.gbc", speedup) { }
    }

    public class Silver : GoldSilver
    {

        public Silver(bool speedup = false) : base("roms/pokesilver.gbc", speedup) { }
    } 
}