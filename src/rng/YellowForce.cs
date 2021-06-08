public class YellowForce : RbyForce {

    public YellowForce(bool speedup = false) : base("roms/pokeyellow.gbc", speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) {
    }

    public void FastOptions(Joypad joypad) {
        if(CurrentMenuType == MenuType.Options) return;
        OpenStartMenu();
        ChooseMenuItem(4 + StartMenuOffset(), joypad);
        CurrentMenuType = MenuType.Options;
    }
}