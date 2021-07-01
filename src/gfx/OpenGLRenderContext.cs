using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using OpenGL;
using static SDL2.SDL;

public class OpenGLRenderContext : RenderContext {

    public static IntPtr ContextHandle;

    private int Version_;
    public int Version {
        get {
            if(Version_ == 0) {
                Gl.GetInteger(GetPName.MajorVersion, out int major);
                Gl.GetInteger(GetPName.MinorVersion, out int minor);
                Version_ = major * 100 + minor * 10;
            }

            return Version_;
        }
    }

    private string ShaderVersion_;
    public string ShaderVersion {
        get {
            if(ShaderVersion_ == null) {
                int version = Version;

                // Map the OpenGL version to its corresponding GLSL version.
                if(version >= 330) {
                    ShaderVersion_ = version.ToString();
                } else if(version >= 320) {
                    ShaderVersion_ = "150";
                } else if(version >= 310) {
                    ShaderVersion_ = "140";
                } else if(version >= 300) {
                    ShaderVersion_ = "130";
                } else if(version >= 210) {
                    ShaderVersion_ = "120";
                } else if(version >= 20) {
                    ShaderVersion_ = "110";
                } else {
                    int major = version / 100;
                    int minor = (version / 10) % 10;
                    Debug.Error("OpenGL Version {0}.{1} does not support shaders", major, minor);
                    return "";
                }
            }

            return ShaderVersion_;
        }
    }

    public uint VertexBuffer;
    public uint VertexArray;
    public uint IndexBuffer;
    public uint ShaderProgram;
    public uint VertexShader;
    public uint FragmentShader;

    public Dictionary<string, int> Uniforms;

    public RendererCapabilities CreateContext(IntPtr window) {
        // OpenGL only allows for one thread per context, so if a context already exists it has to be cleaned up before trying to make a new one.
        if(ContextHandle != IntPtr.Zero) {
            SDL_GL_DeleteContext(ContextHandle);
        } else {
            Gl.Initialize();
        }

        // Set OpenGL attributes.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8);     // Allocate 8 bits per pixel for the red color-channel.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8);   // Allocate 8 bits per pixel for the green color-channel.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8);    // Allocate 8 bits per pixel for the blue color-channel.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);   // Allocate 8 bits per pixel for the alpha channel.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BUFFER_SIZE, 32); // Therefore, one pixel in the screen buffer allocates 32 bits.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);  // Allocate a 16-bit depth buffer.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1); // Enable double buffering.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3); // Set the specifications of the OpenGL context to 3.2 core.
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);

        // Create the context and bind it to the hwnd.
        ContextHandle = SDL_GL_CreateContext(window);
        SDL_GL_MakeCurrent(window, ContextHandle);
        // Disable V-Sync.
        SDL_GL_SetSwapInterval(0);

        // Setup an OpenGL debug context for easier debugging.
        Gl.Enable((EnableCap) Gl.DEBUG_OUTPUT);
        Gl.Enable((EnableCap) Gl.DEBUG_OUTPUT_SYNCHRONOUS);
        Gl.DebugMessageCallback(OpenGLMessageCallback, IntPtr.Zero);
        Gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0, null, false);

        // Query the graphics driver for the capabilities of the GPU.
        RendererCapabilities caps = new RendererCapabilities();
        Gl.GetInteger(GetPName.MaxTextureSize, out caps.MaxTextureSize);
        Gl.GetInteger(GetPName.MaxTextureImageUnits, out caps.NumTextureSlots);

        // Setup alpha blending and depth testing.
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Gl.CullFace(CullFaceMode.Back);
        Gl.Enable(EnableCap.Blend);
        Gl.Enable(EnableCap.DepthTest);

        return caps;
    }

    public void Initialize(uint[] indices, string shader) {
        VertexArray = Gl.GenVertexArray();
        Gl.BindVertexArray(VertexArray);

        // Allocate a vertex buffer that is big enough to hold 'MaxVerticies' number of vertices.
        // The draw hint 'DynamicDraw' indicades, that the data may be changed every frame.
        VertexBuffer = Gl.GenBuffer();
        Gl.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        Gl.BufferData(BufferTarget.ArrayBuffer, (uint) (Renderer.MaxVerticies * Marshal.SizeOf<Vertex>()), IntPtr.Zero, BufferUsage.DynamicDraw);
        Gl.EnableVertexAttribArray(0);
        // The vertex position is a 4-component vector that is not normalized.
        Gl.VertexAttribPointer(0, 4, VertexAttribType.Float, false, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("Position"));
        Gl.EnableVertexAttribArray(1);
        // The vertex color is a 4-component vector that is normalized.
        Gl.VertexAttribPointer(1, 4, VertexAttribType.Float, true, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("Color"));
        Gl.EnableVertexAttribArray(2);
        // The vertex texture coordinates is a 2-component vector that is not normalized.
        Gl.VertexAttribPointer(2, 2, VertexAttribType.Float, false, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("TexCoord"));
        Gl.EnableVertexAttribArray(3);
        // The vertex texture index is a 1-component vector that is not normalized.
        Gl.VertexAttribPointer(3, 1, VertexAttribType.Float, false, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("TexIndex"));

        // Allocate an index buffer that is big enough to hold 'MaxIndices' number of indices.
        // The draw hint 'StaticDraw' indicades, that the data will not be changed.
        IndexBuffer = Gl.GenBuffer();
        Gl.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
        Gl.BufferData(BufferTarget.ElementArrayBuffer, (uint) (indices.Length * sizeof(uint)), indices, BufferUsage.StaticDraw);

        // Load the shader source.
        string shaderText = File.ReadAllText(shader);
        // The shader source is split into a vertex shader and a fragment shader via preprocessor statements.
        string vertexShaderText = "#version " + ShaderVersion + "\n#define VS_BUILD\n" + shaderText;
        string fragmentShaderText = "#version " + ShaderVersion + "\n#define FS_BUILD\n" + shaderText;

        // Create a new shader program and compile the vertex shader and fragment shader.
        ShaderProgram = Gl.CreateProgram();
        VertexShader = CompileShader(ShaderType.VertexShader, vertexShaderText);
        FragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderText);

        // Attach the shaders to the program and link.
        Gl.AttachShader(ShaderProgram, VertexShader);
        Gl.AttachShader(ShaderProgram, FragmentShader);
        Gl.LinkProgram(ShaderProgram);
        Gl.ValidateProgram(ShaderProgram);

        Gl.UseProgram(ShaderProgram);

        // Find and bind the attribute locations.
        Gl.GetProgram(ShaderProgram, ProgramProperty.ActiveAttributes, out int numActiveAttribs);
        Gl.GetProgram(ShaderProgram, ProgramProperty.ActiveAttributeMaxLength, out int maxAttribNameLength);
        StringBuilder nameData = new StringBuilder(maxAttribNameLength);
        for(uint attrib = 0; attrib < numActiveAttribs; attrib++) {
            Gl.GetActiveAttrib(ShaderProgram, attrib, nameData.Capacity, out _, out _, out _, nameData);
            Gl.BindAttribLocation(ShaderProgram, attrib, nameData.ToString());
        }

        // Query the uniform locations.
        Gl.GetProgram(ShaderProgram, ProgramProperty.ActiveUniforms, out int numActiveUniforms);
        Gl.GetProgram(ShaderProgram, ProgramProperty.ActiveUniformMaxLength, out int maxUniformNameLength);
        Uniforms = new Dictionary<string, int>();
        nameData = new StringBuilder(maxUniformNameLength);
        for(uint uniform = 0, samplerCount = 0; uniform < numActiveUniforms; uniform++) {
            Gl.GetActiveUniform(ShaderProgram, uniform, nameData.Capacity, out _, out int arraySize, out int type, nameData);
            string uniformName = nameData.ToString();
            if(arraySize > 1) uniformName = uniformName.Substring(0, uniformName.IndexOf("["));
            for(int i = 0; i < arraySize; i++) {
                string name = arraySize == 1 ? uniformName : uniformName + "[" + i + "]";
                int location = Gl.GetUniformLocation(ShaderProgram, name);
                Uniforms[name] = location;
                // Additionally bind the sampler2D uniforms to the OpenGL texture slots in order of appearence.
                if(type == Gl.SAMPLER_2D) {
                    Gl.Uniform1(location, (int) (samplerCount++));
                }
            }
        }
    }

    public SDL_WindowFlags GetSDLWindowFlags() {
        return SDL_WindowFlags.SDL_WINDOW_OPENGL;
    }

    public void Dispose() {
        // Clean up the OpenGL objects.
        Gl.DeleteBuffers(VertexBuffer, IndexBuffer);
        Gl.DeleteVertexArrays(VertexArray);
        Gl.DetachShader(ShaderProgram, VertexShader);
        Gl.DetachShader(ShaderProgram, FragmentShader);
        Gl.DeleteShader(VertexShader);
        Gl.DeleteShader(FragmentShader);
        Gl.DeleteProgram(ShaderProgram);
        // Delete the OpenGL context.
        SDL_GL_DeleteContext(ContextHandle);
    }

    private static void OpenGLMessageCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
        // Translate and dispatch any debug messages from OpenGL.
        string messageText = Marshal.PtrToStringAnsi(message);
        switch(severity) {
            case DebugSeverity.DebugSeverityHigh:
            case DebugSeverity.DebugSeverityMedium: Debug.Error(messageText); break;
            case DebugSeverity.DebugSeverityLow: Debug.Warning(messageText); break;
            case DebugSeverity.DebugSeverityNotification: Debug.Info(messageText); break;
        }
    }

    public void SwapBuffers(IntPtr window) {
        // Display the offscreen buffer to the screen.
        SDL_GL_SwapWindow(window);
    }

    public void ClearScreen() {
        // Clear the color and depth buffer.
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void SetViewport(int x, int y, int width, int height) {
        // Set the drawing viewport.
        Gl.Viewport(x, y, width, height);
    }

    public void DrawIndexed(int count) {
        // Draw 'count' indices from the currently bound index buffer to the screen.
        Gl.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, IntPtr.Zero);
    }

    public unsafe void ReadBuffer(byte[] dest) {
        byte[] buf = new byte[dest.Length];
        fixed(byte* data = buf) Gl.ReadPixels(0, 0, Renderer.Window.Width, Renderer.Window.Height, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr) data);
        int scanlineSize = Renderer.Window.Width * 4;
        for(int i = 0; i < Renderer.Window.Height; i++) {
            Array.Copy(buf, i * scanlineSize, dest, (Renderer.Window.Height - i - 1) * scanlineSize, scanlineSize);
        }
    }

    public ulong CreateTexture(int width, int height, PixelFormat format) {
        // Allocate a new 2d texture.
        uint tex = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2d, tex);
        // Use the nearest-filter for upscaling or downscaling the texture.
        Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.Nearest);
        Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Nearest);
        return (ulong) tex;
    }

    public void SetTexturePixels(ulong texture, byte[] pixels, int pitch, PixelFormat format) {
        // Convert from the graphics API independent pixel format to the OpenGL specific one.
        OpenGL.PixelFormat openglformat = 0;
        switch(format) {
            case PixelFormat.RGBA: openglformat = OpenGL.PixelFormat.Rgba; break;
            case PixelFormat.BGRA: openglformat = OpenGL.PixelFormat.Bgra; break;
        }

        Gl.BindTexture(TextureTarget.Texture2d, (uint) texture);
        // Upload the new pixel data to VRAM.
        Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, pitch, (pixels.Length >> 2) / pitch, 0, openglformat, PixelType.UnsignedByte, pixels);
    }

    public void PrepareDrawCall() {
        // Bind OpenGL objects.
        Gl.UseProgram(ShaderProgram);
        for(int i = 0; i < Renderer.TextureSlotIndex; i++) {
            Gl.ActiveTexture((TextureUnit) (TextureUnit.Texture0 + i));
            Gl.BindTexture(TextureTarget.Texture2d, (uint) Renderer.TextureSlots[i]);
        }
        Gl.BindVertexArray(VertexArray);
        Gl.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        // Upload the new vertex buffer data to VRAM.
        Gl.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (uint) (Renderer.VertexIndex * Marshal.SizeOf<Vertex>()), Renderer.Vertices);
    }

    private uint CompileShader(ShaderType type, string text) {
        // Create a new shader.
        uint shader = Gl.CreateShader(type);

        // Link the source to the shader and compile.
        Gl.ShaderSource(shader, new string[] { text });
        Gl.CompileShader(shader);

        // Check if the compliations has been successful.
        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if(success == (int) OpenGL.Boolean.False) {
            Gl.GetShader(shader, ShaderParameterName.InfoLogLength, out int length);
            StringBuilder sb = new StringBuilder(length);
            Gl.GetShaderInfoLog(shader, sb.Capacity, out _, sb);
            Debug.Error("Failed to compile shader of type {0}: \n{1}", Enum.GetName(typeof(ShaderType), type), sb.ToString());
            return 0;
        }

        return shader;
    }
}