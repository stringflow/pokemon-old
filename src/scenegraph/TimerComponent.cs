using System;

public class TimerComponent : TextComponent {

    public ulong Start;
    public bool Running = true;

    public TimerComponent(float x, float y, float scale = 1) : base("", x, y, scale) {
    }

    public override void OnInit(GameBoy gb) {
        Start = gb.EmulatedSamples;
    }

    public override void BeginScene(GameBoy gb) {
        TimeSpan duration = TimeSpan.FromSeconds((gb.EmulatedSamples - Start) / 2097152.0);
        if(Running) Text = string.Format("{0:ss\\.fff}", duration);
    }
}