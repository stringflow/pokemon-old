public class Yellow : Rby {

    public Yellow(bool speedup = false) : base("roms/pokeyellow.gbc", speedup ? SpeedupFlags.NoVideo | SpeedupFlags.NoSound : SpeedupFlags.None) { }

    public override void ChooseMenuItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        MenuScroll(target, Joypad.A, false);
    }

    public override void SelectMenuItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        MenuScroll(target, Joypad.Select, true);
    }

    public override void ChooseListItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        ListScroll(target, Joypad.A, false);
    }

    public override void SelectListItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        ListScroll(target, Joypad.Select, true);
    }
}
