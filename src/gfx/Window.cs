using System;
using static SDL2.SDL;

public class Window : IDisposable {

    public IntPtr Handle;
    public int Width;
    public int Height;

    public Window(int width, int height, string title) {
        const uint flags = SDL_INIT_VIDEO;

        uint initialized = SDL_WasInit(flags);
        if(initialized != flags && SDL_Init(flags) != 0) {
            Debug.Error("Failed to initalized SDL2");
        }

        Width = width;
        Height = height;
        Handle = SDL_CreateWindow(title, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, width, height, Renderer.GetSDLWindowFlags());
        Renderer.Initialize(this);
    }

    public void Dispose() {
        Renderer.Dispose();
        SDL_DestroyWindow(Handle);
        SDL_Quit();
    }

    private SDL_Event e;

    public void Present() {
        Renderer.SwapBuffers(Handle);

        // TODO: different thread
        while(SDL_PollEvent(out e) != 0) {
        }
    }
}