using System;
using System.Runtime.InteropServices;
using static SDL2.SDL;

public class SDL2RenderContext : RenderContext {

    public IntPtr Context;

    public RendererCapabilities CreateContext(IntPtr window) {
        Context = SDL_CreateRenderer(window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        SDL_SetHint("SDL_RENDER_SCALE_QUALITY", "nearest");
        SDL_GetWindowSize(window, out int w, out int h);
        SetViewport(0, 0, w, h);
        return new RendererCapabilities {
            MaxTextureSize = 8192,
            NumTextureSlots = 8192,
        };
    }

    public void Initialize(uint[] indices, string shader) {
    }

    public SDL_WindowFlags GetSDLWindowFlags() {
        return (SDL_WindowFlags) SDL_WindowFlags.SDL_WINDOW_SHOWN;
    }

    public void Dispose() {
        SDL_DestroyRenderer(Context);
    }

    public void SwapBuffers(IntPtr window) {
        SDL_RenderPresent(Context);
    }

    public void ClearScreen() {
        SDL_RenderClear(Context);
    }

    public void SetViewport(int x, int y, int width, int height) {
        SDL_RenderSetLogicalSize(Context, width, height);
    }

    public void DrawIndexed(int count) {
        for(int i = 0; i < Renderer.VertexIndex; i += 4) {
            int tex = (int) Renderer.Vertices[i].TexIndex;
            Vertex vMin = Renderer.Vertices[i + 3];
            Vertex vMax = Renderer.Vertices[i + 1];
            int minX = (int) ((vMin.Position.X + 1.0f) * (Renderer.Window.Width / 2.0f));
            int minY = (int) ((vMin.Position.Y + 1.0f) * (Renderer.Window.Height / 2.0f));
            int maxX = (int) ((vMax.Position.X + 1.0f) * (Renderer.Window.Width / 2.0f));
            int maxY = (int) ((vMax.Position.Y + 1.0f) * (Renderer.Window.Height / 2.0f));
            int width = maxX - minX;
            int height = maxY - minY;
            SDL_Rect destrect = new SDL_Rect { x = minX, y = minY, w = width, h = height };
            SDL_RenderCopy(Context, (IntPtr) Renderer.TextureSlots[tex], IntPtr.Zero, ref destrect);
        }
    }

    public unsafe void ReadBuffer(byte[] dest) {
        SDL_Rect rect = new SDL_Rect { x = 0, y = 0, w = Renderer.Window.Width, h = Renderer.Window.Height };
        fixed(byte* data = dest) SDL_RenderReadPixels(Context, ref rect, SDL_PIXELFORMAT_RGBA8888, (IntPtr) data, Renderer.Window.Width * 4);
    }

    public ulong CreateTexture(int width, int height, PixelFormat format) {
        uint sdlformat = 0;
        switch(format) {
            case PixelFormat.RGBA: sdlformat = SDL_PIXELFORMAT_BGRA8888; break;
            case PixelFormat.BGRA: sdlformat = SDL_PIXELFORMAT_RGBA8888; break;
        }

        return (ulong) SDL_CreateTexture(Context, sdlformat, (int) SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
    }

    public unsafe void SetTexturePixels(ulong texture, byte[] pixels, int pitch, PixelFormat format) {
        fixed(byte* data = pixels) SDL_UpdateTexture((IntPtr) texture, IntPtr.Zero, (IntPtr) data - 1, pitch * 4);
    }

    public void PrepareDrawCall() {
    }
}