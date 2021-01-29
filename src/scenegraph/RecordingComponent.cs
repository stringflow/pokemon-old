using System;

public class RecordingComponent : Component {

    public string Movie;
    public FFMPEGStream VideoStream;
    public FFMPEGStream AudioStream;
    public double RecordingNow;
    public byte[] OffscreenBuffer;

    public RecordingComponent(string movie) {
        Movie = movie;
        VideoStream = new FFMPEGStream("-y -f rawvideo -s " + Renderer.Window.Width + "x" + Renderer.Window.Height + " -pix_fmt rgb24 -r 60 -i - -crf 0 movies/video.mp4");
        AudioStream = new FFMPEGStream("-y -f s16le -ar 2097152 -ac 2 -i - -af volume=0.1 movies/audio.mp3");
        OffscreenBuffer = new byte[Renderer.Window.Width * Renderer.Window.Height * 3];
    }

    public override void OnInit(GameBoy gb) {
        RecordingNow = gb.EmulatedSamples;
    }

    public override void Dispose(GameBoy gb) {
        VideoStream.Close();
        AudioStream.Close();
        FFMPEG.RunFFMPEGCommand("-y -i movies/video.mp4 -i movies/audio.mp3 -c:v copy -c:a copy -shortest movies/" + Movie + ".mp4");
    }

    public override void OnAudioReady(GameBoy gb, int bufferOffset) {
        AudioStream.Stream.Write(gb.AudioBuffer, 0, bufferOffset * 4);
    }

    public override void EndScene(GameBoy gb) {
        Renderer.ReadBuffer(OffscreenBuffer);
        while(gb.EmulatedSamples > RecordingNow) {
            VideoStream.Stream.Write(OffscreenBuffer);
            RecordingNow += Math.Pow(2, 21) / 60.0;
        }
    }
}