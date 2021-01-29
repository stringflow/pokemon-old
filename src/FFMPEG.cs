using System;
using System.IO;
using System.Diagnostics;

public static class FFMPEG {

    public static Process RunFFMPEGCommand(string args, bool wait = true) {
        Process p = new Process();
        p.StartInfo.FileName = "ffmpeg";
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardError = true;
        //p.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
        p.Start();
        p.BeginErrorReadLine();
        if(wait) p.WaitForExit();
        return p;
    }
}

public class FFMPEGStream {

    public Process Process;
    public Stream Stream;

    public FFMPEGStream(string args) {
        Process = FFMPEG.RunFFMPEGCommand(args, false);
        Stream = Process.StandardInput.BaseStream;
    }

    public void Close() {
        Stream.Flush();
        Stream.Close();
        Process.WaitForExit();
    }
}