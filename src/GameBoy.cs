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
    public Scene Scene;
    public ulong EmulatedSamples;

    // Returns the current cycle-based time counter as dividers. (2^21/sec)
    public ulong TimeNow {
        get { return Libgambatte.gambatte_timenow(Handle); }
    }

    // Get Reg and flag values.
    public Registers Registers {
        get { Libgambatte.gambatte_getregs(Handle, out Registers regs); return regs; }
        set { Libgambatte.gambatte_setregs(Handle, value); }
    }

    public int PC {
        get { return Registers.PC; }
        set { Registers regs = Registers; regs.PC = value; Registers = regs; }
    }

    public int SP {
        get { return Registers.SP; }
        set { Registers regs = Registers; regs.SP = value; Registers = regs; }
    }

    public int A {
        get { return Registers.A; }
        set { Registers regs = Registers; regs.A = value; Registers = regs; }
    }

    public int B {
        get { return Registers.B; }
        set { Registers regs = Registers; regs.B = value; Registers = regs; }
    }

    public int C {
        get { return Registers.C; }
        set { Registers regs = Registers; regs.C = value; Registers = regs; }
    }

    public int D {
        get { return Registers.D; }
        set { Registers regs = Registers; regs.D = value; Registers = regs; }
    }

    public int E {
        get { return Registers.E; }
        set { Registers regs = Registers; regs.E = value; Registers = regs; }
    }

    public int F {
        get { return Registers.F; }
        set { Registers regs = Registers; regs.F = value; Registers = regs; }
    }

    public int H {
        get { return Registers.H; }
        set { Registers regs = Registers; regs.H = value; Registers = regs; }
    }

    public int L {
        get { return Registers.L; }
        set { Registers regs = Registers; regs.L = value; Registers = regs; }
    }

    public int DividerState {
        get { return Libgambatte.gambatte_getdivstate(Handle); }
    }

    public GameBoy(string biosFile, string romFile, string savFile = null, SpeedupFlags speedupFlags = SpeedupFlags.None) {
        ROM = new ROM(romFile);
        Debug.Assert(ROM.HeaderChecksumMatches(), "Cartridge header checksum mismatch!");

        string romName = Path.GetFileNameWithoutExtension(romFile);

        if(savFile == null || savFile == "") {
            File.Delete("roms/" + romName + ".sav");
        } else {
            File.WriteAllBytes("roms/" + romName + ".sav", File.ReadAllBytes(savFile));
        }

        Handle = Libgambatte.gambatte_create();
        Debug.Assert(Libgambatte.gambatte_loadbios(Handle, biosFile, 0x900, 0x31672598) == 0, "Unable to load BIOS!");
        Debug.Assert(Libgambatte.gambatte_load(Handle, romFile, LoadFlags.GbaFlag | LoadFlags.GcbMode | LoadFlags.ReadOnlySav) == 0, "Unable to load ROM!");

        VideoBuffer = new byte[160 * 144 * 4];
        AudioBuffer = new byte[(SamplesPerFrame + 2064) * 2 * 2]; // Stereo 16-bit samples

        InputGetter = () => CurrentJoypad;
        Libgambatte.gambatte_setinputgetter(Handle, InputGetter);

        string symPath = "symfiles/" + romName + ".sym";
        if(File.Exists(symPath)) {
            SYM = new SYM(symPath);
            ROM.Symbols = SYM;
        }

        SetSpeedupFlags(speedupFlags);
        StateSize = Libgambatte.gambatte_savestate(Handle, null, 160, null);
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
            // returns a positive value if the video frame hass been completed.
            if(videoFrameDoneSampleCount >= 0) {
                Scene.Begin();
                Scene.Render();
                Scene.End();
            }
        }

        return runsamples;
    }

    // Emulates until the next video frame has to be drawn. Returns the hit address.
    public int AdvanceFrame(Joypad joypad = Joypad.None) {
        CurrentJoypad = joypad;
        RunFor(SamplesPerFrame - BufferSamples);
        CurrentJoypad = Joypad.None;
        return Libgambatte.gambatte_gethitinterruptaddress(Handle);
    }

    public void AdvanceFrames(int amount, Joypad joypad = Joypad.None) {
        for(int i = 0; i < amount; i++) AdvanceFrame(joypad);
    }

    // Emulates while holding the specified input until the program counter hits one of the specified breakpoints.
    public unsafe virtual int Hold(Joypad joypad, params int[] addrs) {
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

    // Returns the emulator state as a struct.
    public DetailedState SaveDetailedState() {
        byte[] saveState = SaveState();
        return new DetailedState(saveState);
    }

    // Loads the emulator state given by a buffer.
    public void LoadState(byte[] buffer) {
        Libgambatte.gambatte_loadstate(Handle, buffer, buffer.Length);
    }

    // Helper function that reads the buffer directly from disk.
    public void LoadState(string file) {
        LoadState(File.ReadAllBytes(file));
    }

    // Loads the emulator state given by a struct.
    public void LoadDetailedState(DetailedState state) {
        LoadState(state.ToBuffer());
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

    public void SetRTCDivisorOffset(int rtcDivisorOffset) {
        Libgambatte.gambatte_setrtcdivisoroffset(Handle, rtcDivisorOffset);
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

    public void PlayBizhawkMovie(string bk2File, int frameCount = int.MaxValue) {
        using(FileStream bk2Stream = File.OpenRead(bk2File))
        using(ZipArchive zip = new ZipArchive(bk2Stream, ZipArchiveMode.Read))
        using(StreamReader bk2Reader = new StreamReader(zip.GetEntry("Input Log.txt").Open())) {
            PlayBizhawkInputLog(bk2Reader.ReadToEnd().Split('\n'), frameCount);
        }
    }

    public void PlayBizhawkInputLog(string fileName, int frameCount = int.MaxValue) {
        PlayBizhawkInputLog(File.ReadAllLines(fileName), frameCount);
    }

    public void PlayBizhawkInputLog(string[] lines, int frameCount = int.MaxValue) {
        Joypad[] joypadFlags = { Joypad.Up, Joypad.Down, Joypad.Left, Joypad.Right, Joypad.Start, Joypad.Select, Joypad.B, Joypad.A };
        lines = lines.Subarray(2, lines.Length - 4);
        for(int i = 0; i < lines.Length && i < frameCount; i++) {
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

    public void PlayGambatteMovie(string filename) {
        byte[] file = File.ReadAllBytes(filename);
        ReadStream movie = new ReadStream(file);
        Debug.Assert(movie.u8() == 0xfe, "The specified file was not a gambatte movie.");
        Debug.Assert(movie.u8() == 0x01, "The specified gambatte movie was of an incorrect version.");

        int stateSize = movie.u24be();
        byte[] state = movie.Read(stateSize);

        LoadState(state);

        while(movie.Position < file.Length) {
            long samples = movie.u32be();
            byte input = movie.u8();

            if(input == 0xff) {
                HardReset();
            } else {
                CurrentJoypad = (Joypad) input;
                while(samples > 0) samples -= RunFor((int) Math.Min(samples, SamplesPerFrame));
            }
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

    // Reads the game's font from the ROM. Each game overrides this function and implements it in its own way.
    public virtual Font ReadFont() {
        return null;
    }

    // Injects an input by overwriting the hardware register.
    public virtual void Inject(Joypad joypad) {
        throw new NotImplementedException();
    }

    // Wrapper for advancing to joypad polling and injecting an input
    public virtual void Press(params Joypad[] joypads) {
        throw new NotImplementedException();
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

    /**
	  * Load ROM image.
	  *
	  * @param romfile  Path to rom image file. Typically a .gbc, .gb-file.
	  * @param flags    ORed combination of LoadFlags.
	  * @return 0 on success, negative value on failure.
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_load(IntPtr gb, string romfile, LoadFlags flags);

    /**
	  * Load bios image.
	  *
	  * @param biosfile  Path to bios image file. Typically a .bin-file.
	  * @param size      File size requirement or 0.
	  * @param crc       File crc32 requirement or 0.
	  * @return 0 on success, negative value on failure.
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_loadbios(IntPtr gb, string biosfile, int size, int crc);

    /**
	  * Emulates until at least 'samples' audio samples are produced in the
	  * supplied audio buffer, or until a video frame has been drawn.
	  *
	  * There are 35112 audio (stereo) samples in a video frame.
	  * May run for up to 2064 audio samples too long.
	  *
	  * An audio sample consists of two native endian 2s complement 16-bit PCM samples,
	  * with the left sample preceding the right one.
      *
	  * Returns early when a new video frame has finished drawing in the video buffer,
	  * such that the caller may update the video output before the frame is overwritten.
	  * The return value indicates whether a new video frame has been drawn, and the
	  * exact time (in number of samples) at which it was completed.
	  *
	  * @param videoBuf 160x144 RGB32 (native endian) video frame buffer or 0
	  * @param pitch distance in number of pixels (not bytes) from the start of one line
	  *              to the next in videoBuf.
	  * @param audioBuf buffer with space >= samples + 2064
	  * @param samples  in: number of stereo samples to produce,
	  *                out: actual number of samples produced
	  * @return sample offset in audioBuf at which the video frame was completed, or -1
	  *         if no new video frame was completed.
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_runfor(IntPtr gb, byte[] videoBuf, int pitch, byte[] audioBuf, ref int samples);

    /** adjust the assumed clock speed of the CPU compared to the RTC */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setrtcdivisoroffset(IntPtr gb, int rtcDivisorOffset);

    /**
	  * Reset to initial state.
	  * Equivalent to reloading a ROM image, or turning a Game Boy Color off and on again.
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_reset(IntPtr gb, int samplesToStall);

    /** Sets the callback used for getting input state. */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setinputgetter(IntPtr gb, InputGetter inputgetter);

    /**
	  * Saves emulator state to the buffer given by 'stateBuf'.
	  *
	  * @param  videoBuf 160x144 RGB32 (native endian) video frame buffer or 0. Used for
	  *                  saving a thumbnail.
	  * @param  pitch distance in number of pixels (not bytes) from the start of one line
	  *               to the next in videoBuf.
	  * @return size
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_savestate(IntPtr gb, byte[] videoBuf, int pitch, byte[] stateBuf);

    /**
	  * Loads emulator state from the buffer given by 'stateBuf' of size 'size'.
	  * @return success
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool gambatte_loadstate(IntPtr gb, byte[] stateBuf, int size);

    /**
	  * Read a single byte from the CPU bus. This includes all RAM, ROM, MMIO, etc as
	  * it is visible to the CPU (including mappers). While there is no cycle cost to
	  * these reads, there may be other side effects! Use at your own risk.
	  *
	  * @param addr system bus address
	  * @return byte read
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte gambatte_cpuread(IntPtr gb, ushort addr);

    /**
	  * Write a single byte to the CPU bus. While there is no cycle cost to these
	  * writes, there can be quite a few side effects. Use at your own risk.
	  *
	  * @param addr system bus address
	  * @param val  byte to write
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_cpuwrite(IntPtr gb, ushort addr, byte value);

    /** Get reg and flag values. */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_getregs(IntPtr gb, out Registers regs);

    /** Set reg and flag values. */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setregs(IntPtr gb, Registers regs);

    /**
	  * Sets addresses the CPU will interrupt processing at before the instruction.
	  * Format is 0xBBAAAA where AAAA is an address and BB is an optional ROM bank.
	  */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setinterruptaddresses(IntPtr gb, int* addrs, int numAddrs);

    /** Gets the address the CPU was interrupted at or -1 if stopped normally. */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_gethitinterruptaddress(IntPtr gb);

    /** Returns the current cycle-based time counter as dividers. (2^21/sec) */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong gambatte_timenow(IntPtr gb);

    /** Return a value in range 0-3FFF representing current "position" of internal divider */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gambatte_getdivstate(IntPtr gb);

    /** Sets flags to control non-critical processes for CPU-concerned emulation. */
    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void gambatte_setspeedupflags(IntPtr gb, SpeedupFlags falgs);
}