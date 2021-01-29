using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public enum LoadFlags : int {

    GcbMode = 0x01,          // Treat the ROM as having CGB support regardless of what its header advertises.
    GbaFlag = 0x02,          // Use GBA initial CPU register values in CGB mode.
    MultiCartCompat = 0x04,  // Use heuristics to detect and support multicart MBCs disguised as MBC1.
    SgbMode = 0x08,          // Treat the ROM as having SGB support regardless of what its header advertises.
    ReadOnlySav = 0x10,      // Prevent implicit saveSavedata calls for the ROM.
}

public struct Registers {

    public int PC;
    public int SP;
    public int A;
    public int B;
    public int C;
    public int D;
    public int E;
    public int F;
    public int H;
    public int L;
}

public enum SpeedupFlags : uint {

    None = 0x00,
    NoSound = 0x01,   // Skip generating sound samples.
    NoPPUCall = 0x02, // Skip PPU calls. (breaks LCD interrupt)
    NoVideo = 0x04,   // Skip writing to the video buffer.
    All = 0xffffffff,
}

public enum Joypad : byte {

    None = 0x0,
    A = 0x1,
    B = 0x2,
    Select = 0x4,
    Start = 0x8,
    Right = 0x10,
    Left = 0x20,
    Up = 0x40,
    Down = 0x80,
    All = 0xff,
}

public partial class GameBoy : IDisposable {

    public const int SamplesPerFrame = 35112;

    public IntPtr Handle;
    public byte[] VideoBuffer; // Although shared library outputs in the ARGB little-endian format, the data is interpreted as big-endian making it effectively BGRA.
    public byte[] AudioBuffer;
    public InputGetter InputGetter;
    public Joypad CurrentJoypad;
    public int BufferSamples;
    public int StateSize;

    public ROM ROM;
    public SYM SYM;
    public Dictionary<string, int> SaveStateLabels;
    public Scene Scene;
    public ulong EmulatedSamples;

    // Returns the current cycle-based time counter as dividers. (2^21/sec)
    public int TimeNow {
        get { return Libgambatte.gambatte_timenow(Handle); }
    }

    // Get Reg and flag values.
    public Registers Registers {
        get { Libgambatte.gambatte_getregs(Handle, out Registers regs); return regs; }
    }

    public int DividerState {
        get { return Libgambatte.gambatte_getdivstate(Handle); }
    }

    public GameBoy(string biosFile, string romFile, SpeedupFlags speedupFlags = SpeedupFlags.None) {
        ROM = new ROM(romFile);
        Debug.Assert(ROM.HeaderChecksumMatches(), "Cartridge header checksum mismatch!");

        Handle = Libgambatte.gambatte_create();
        Debug.Assert(Libgambatte.gambatte_loadbios(Handle, biosFile, 0x900, 0x31672598) == 0, "Unable to load BIOS!");
        Debug.Assert(Libgambatte.gambatte_load(Handle, romFile, LoadFlags.GbaFlag | LoadFlags.GcbMode | LoadFlags.ReadOnlySav) == 0, "Unable to load ROM!");

        VideoBuffer = new byte[160 * 144 * 4];
        AudioBuffer = new byte[(SamplesPerFrame + 2064) * 2 * 2]; // Stereo 16-bit samples

        InputGetter = () => CurrentJoypad;
        Libgambatte.gambatte_setinputgetter(Handle, InputGetter);

        string symPath = "symfiles/" + Path.GetFileNameWithoutExtension(romFile) + ".sym";
        if(File.Exists(symPath)) {
            SYM = new SYM(symPath);
            ROM.Symbols = SYM;
        }

        SetSpeedupFlags(speedupFlags);
        StateSize = Libgambatte.gambatte_savestate(Handle, null, 160, null);

        SaveStateLabels = new Dictionary<string, int>();
        byte[] state = SaveState();
        ByteStream data = new ByteStream(state);
        data.Seek(3);
        data.Seek(data.u24be());
        while(data.Position < state.Length) {
            string label = "";
            byte character;
            while((character = data.u8()) != 0x00) label += Convert.ToChar(character);
            int size = data.u24be();
            SaveStateLabels[label] = (int) data.Position;
            data.Seek(size);
        }
    }

    public void Dispose() {
        if(Scene != null) Scene.Dispose();
        Libgambatte.gambatte_destroy(Handle);
    }

    public void HardReset(bool fade = false) {
        Libgambatte.gambatte_reset(Handle, fade ? 101 * (2 << 14) : 0);
        BufferSamples = 0;
    }

    // Emulates 'runsamples' number of samples, or until a video frame has to be drawn. (1 sample = 2 cpu cycles)
    public int RunFor(int runsamples) {
        int videoFrameDoneSampleCount = Libgambatte.gambatte_runfor(Handle, VideoBuffer, 160, AudioBuffer, ref runsamples);
        int outsamples = videoFrameDoneSampleCount >= 0 ? BufferSamples + videoFrameDoneSampleCount : BufferSamples + runsamples;
        BufferSamples += runsamples;
        BufferSamples -= outsamples;
        EmulatedSamples += (ulong) outsamples;

        if(Scene != null) {
            Scene.OnAudioReady(outsamples);
            // returns a positive value if a video frame needs to be drawn.
            if(videoFrameDoneSampleCount >= 0) {
                Scene.Begin();
                Scene.Render();
                Scene.End();
            }
        }

        return Libgambatte.gambatte_gethitinterruptaddress(Handle);
    }

    // Emulates until the next video frame has to be drawn. Returns the hit address.
    public int AdvanceFrame(Joypad joypad = Joypad.None) {
        CurrentJoypad = joypad;
        int hitaddress = RunFor(SamplesPerFrame - BufferSamples);
        CurrentJoypad = Joypad.None;
        return hitaddress;
    }

    public void AdvanceFrames(int amount, Joypad joypad = Joypad.None) {
        for(int i = 0; i < amount; i++) AdvanceFrame(joypad);
    }

    // Emulates while holding the specified input until the program counter hits one of the specified breakpoints.
    public unsafe int Hold(Joypad joypad, params int[] addrs) {
        fixed(int* addrPtr = addrs) { // Note: Not fixing the pointer causes an AccessValidationException.
            Libgambatte.gambatte_setinterruptaddresses(Handle, addrPtr, addrs.Length);
            int hitaddress;
            do {
                hitaddress = AdvanceFrame(joypad);
            } while(Array.IndexOf(addrs, hitaddress) == -1);
            Libgambatte.gambatte_setinterruptaddresses(Handle, null, 0);
            return hitaddress;
        }
    }

    // Helper function that emulates with no joypad held.
    public int RunUntil(params int[] addrs) {
        return Hold(Joypad.None, addrs);
    }

    // Writes one byte of data to the CPU bus.
    public void CpuWrite(int addr, byte data) {
        Libgambatte.gambatte_cpuwrite(Handle, (ushort) addr, data);
    }

    // Reads one byte of data from the CPU bus.
    public byte CpuRead(int addr) {
        return Libgambatte.gambatte_cpuread(Handle, (ushort) addr);
    }

    // Returns the emulator state as a buffer.
    public byte[] SaveState() {
        byte[] state = new byte[StateSize];
        Libgambatte.gambatte_savestate(Handle, null, 160, state);
        return state;
    }

    // Helper function that writes the buffer directly to disk.
    public void SaveState(string file) {
        File.WriteAllBytes(file, SaveState());
    }

    // Loads the emulator state given by a buffer.
    public void LoadState(byte[] buffer) {
        Libgambatte.gambatte_loadstate(Handle, buffer, buffer.Length);
    }

    // Helper function that reads the buffer directly from disk.
    public void LoadState(string file) {
        LoadState(File.ReadAllBytes(file));
    }

    // Sets flags to control non-critical processes for CPU-concerned emulation.
    public void SetSpeedupFlags(SpeedupFlags flags) {
        Libgambatte.gambatte_setspeedupflags(Handle, flags);
    }

    // Helper functions that translate SYM labels to their respective addresses.
    public int RunUntil(params string[] addrs) {
        return RunUntil(Array.ConvertAll(addrs, e => SYM[e]));
    }

    public int Hold(Joypad joypad, params string[] addrs) {
        return Hold(joypad, Array.ConvertAll(addrs, e => SYM[e]));
    }

    public void CpuWrite(string addr, byte data) {
        CpuWrite(SYM[addr], data);
    }

    public byte CpuRead(string addr) {
        return CpuRead(SYM[addr]);
    }

    // Helper function that creates a basic scene graph with a video buffer component.
    public void Show() {
        Scene s = new Scene(this, 160, 144);
        s.AddComponent(new VideoBufferComponent(0, 0, 160, 144));
        SetSpeedupFlags(SpeedupFlags.NoSound);
    }

    // Helper function that creates a basic scene graph with a video buffer component and a record component.
    public void Record(string movie) {
        Show();
        SetSpeedupFlags(SpeedupFlags.None);
        Scene.AddComponent(new RecordingComponent(movie));
    }

    public void PlayBizhawkMovie(string bk2File) {
        using(FileStream bk2Stream = File.OpenRead(bk2File))
        using(ZipArchive zip = new ZipArchive(bk2Stream, ZipArchiveMode.Read))
        using(StreamReader bk2Reader = new StreamReader(zip.GetEntry("Input Log.txt").Open())) {
            PlayBizhawkInputLog(bk2Reader.ReadToEnd().Split('\n'));
        }
    }

    public void PlayBizhawkInputLog(string fileName) {
        PlayBizhawkInputLog(File.ReadAllLines(fileName));
    }

    public void PlayBizhawkInputLog(string[] lines) {
        Joypad[] joypadFlags = { Joypad.Up, Joypad.Down, Joypad.Left, Joypad.Right, Joypad.Start, Joypad.Select, Joypad.B, Joypad.A };
        lines = lines.Subarray(2, lines.Length - 3);
        for(int i = 0; i < lines.Length; i++) {
            if(lines[i][9] != '.') {
                HardReset(false);
            }
            Joypad joypad = Joypad.None;
            for(int j = 0; j < joypadFlags.Length; j++) {
                if(lines[i][j + 1] != '.') {
                    joypad |= joypadFlags[j];
                }
            }
            AdvanceFrame(joypad);
        }
    }

    public Bitmap Screenshot() {
        Bitmap bitmap;
        if(Scene == null) {
            bitmap = new Bitmap(160, 144, VideoBuffer);
            bitmap.RemapRedAndBlueChannels();
        } else {
            bitmap = new Bitmap(Scene.Window.Width, Scene.Window.Height);
            Renderer.ReadBuffer(bitmap.Pixels);
        }
        return bitmap;
    }
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate Joypad InputGetter();

public static unsafe class Libgambatte {

    public const string dll = "libgambatte.dll";

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_revision();

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr gambatte_create();

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_destroy(IntPtr gb);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_load(IntPtr gb, string romfile, LoadFlags flags);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_loadbios(IntPtr gb, string biosfile, int size, int crc);

    // Emulates until at least 'samples' audio samples are produced in the supplied audio buffer, or until a video frame has been drawn.
    // There are 35112 audio (stereo) samples in a video frame.
    // May run up to 2064 audio samples too long.
    // The video buffer must have space for at least 160x144 ARGB32 (native endian) pixels.
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_runfor(IntPtr gb, byte[] videoBuf, int pitch, byte[] audioBuf, ref int samples);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setrtcdivisoroffset(IntPtr gb, int rtcDivisorOffset);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_reset(IntPtr gb, int samplesToStall);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setinputgetter(IntPtr gb, InputGetter inputgetter);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_savestate(IntPtr gb, byte[] videoBuf, int pitch, byte[] stateBuf);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool gambatte_loadstate(IntPtr gb, byte[] stateBuf, int size);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte gambatte_cpuread(IntPtr gb, ushort addr);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_cpuwrite(IntPtr gb, ushort addr, byte value);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_getregs(IntPtr gb, out Registers regs);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setregs(IntPtr gb, Registers regs);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setinterruptaddresses(IntPtr gb, int* addrs, int numAddrs);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_gethitinterruptaddress(IntPtr gb);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_timenow(IntPtr gb);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_getdivstate(IntPtr gb);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setspeedupflags(IntPtr gb, SpeedupFlags falgs);
}