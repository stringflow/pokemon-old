namespace Pokemon
{
    public class Crystal : Gsc
    {

        public Crystal(bool speedup = false) : base("roms/pokecrystal.gbc", speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }
    } 
}