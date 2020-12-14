using System;
using System.Numerics;
using System.Runtime.InteropServices;

/*
    The universal rendering pipeline that can support different backends. (currently only OpenGL)
    Implemented is a simple 2d batch renderer.
*/

[StructLayout(LayoutKind.Sequential)]
public struct Vertex {

    public Vector4 Position;
    public Vector4 Color;
    public Vector2 TexCoord;
    public float TexIndex;
}

// Capabilities of the GPU returned by the graphics driver.
public struct RendererCapabilities {

    public int MaxTextureSize;
    public int NumTextureSlots;
}

// A graphics API independent pixel format.
public enum PixelFormat {

    RGBA,
    BGRA,
}

// The graphics API backend interface.
public interface RenderContext : IDisposable {

    // Creates the context of the graphics API.
    RendererCapabilities CreateContext(IntPtr window);
    // Initializes the renderer.
    void Initialize(uint[] indices, string shader);

    // Swaps the buffers.
    void SwapBuffers(IntPtr window);
    // Clears the screen.
    void ClearScreen();
    // Sets the viewport.
    void SetViewport(int x, int y, int width, int height);
    // Prepares for a draw call.
    void PrepareDrawCall();
    // Draws an indexed mesh.
    void DrawIndexed(int count);

    // Creates a 2d texture and returns a handle.
    uint CreateTexture();
    // Uploads new pixel data to a 2d texture given a handle.
    void SetTexturePixels(uint texture, byte[] pixels, int pitch, PixelFormat format);
}

public static class Renderer {

    // Currently used render API and its capabilities.
    public static RenderContext RenderAPI = new OpenGLRenderContext();
    public static RendererCapabilities Capabilities;

    // Maximum number of quads per draw call.
    public const int MaxQuads = 1000;
    // Maximum number of vertices per draw call.
    public const int MaxVerticies = MaxQuads * 4;
    // Maximum number of indices per draw call.
    public const int MaxIndices = MaxQuads * 6;

    // Vertex positions of a quad.
    public static Vector4[] VertexPositions;
    // Vertex texture coordinates of a quad.
    public static Vector2[] VertexTexCoords;
    // Vertices that make up the current scene. (CPU side)
    public static Vertex[] Vertices;

    // Currently bound textures.
    public static uint[] TextureSlots;
    // A 1x1 white texture that is sampled from for solid colored quads.
    public static uint WhiteTexture;

    // The tail of the 'Vertices' array.
    public static uint VertexIndex;
    // The tail of the 'Indices' array.
    public static int IndexCount;
    // The tail of the 'TextureSlots' array.
    public static int TextureSlotIndex;

    // The camera's projection.
    public static Matrix4x4 Projection;

    public static void Initialize(IntPtr window) {
        // Create a new graphics API context and query the capabilities.
        Capabilities = RenderAPI.CreateContext(window);

        /* 
            Vertex positions of a 2d quad specified in the folling order:
                3    4

                1    2
        */
        VertexPositions = new Vector4[] { new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
                                          new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                          new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                                          new Vector4(0.0f, 1.0f, 0.0f, 1.0f)};

        // Texture coordinates positions of a 2d quad specified in the same order as above.
        VertexTexCoords = new Vector2[] { new Vector2(0.0f, 0.0f),
                                          new Vector2(1.0f, 0.0f),
                                          new Vector2(1.0f, 1.0f),
                                          new Vector2(0.0f, 1.0f)};

        Vertices = new Vertex[MaxVerticies];

        /* 
            The index buffer is filled in the pattern: 1, 2, 3,
                                                       3, 4, 1
            Making 2 triangles that make out a quad.
        */
        uint[] indices = new uint[MaxIndices];
        for(uint i = 0, offset = 0; i < MaxIndices; offset += 4) {
            indices[i++] = offset + 0;
            indices[i++] = offset + 1;
            indices[i++] = offset + 2;
            indices[i++] = offset + 2;
            indices[i++] = offset + 3;
            indices[i++] = offset + 0;
        }

        // Initialize the graphics API.
        RenderAPI.Initialize(indices, "assets/shader.glsl");

        // Create a fully white 1x1 texture.
        WhiteTexture = CreateTexture(new byte[] { 0xff, 0xff, 0xff, 0xff }, 1, PixelFormat.RGBA);

        // Allocate 'Capabilities.NumTextureSlots' number of texture slots.
        TextureSlots = new uint[Capabilities.NumTextureSlots];
        // Set the first texture slot to the white texture.
        TextureSlots[0] = WhiteTexture;
    }

    public static void Dispose() {
        RenderAPI.Dispose();
    }

    public static void SwapBuffers(IntPtr window) {
        RenderAPI.SwapBuffers(window);
    }

    public static void ClearScreen() {
        RenderAPI.ClearScreen();
    }

    public static void SetViewport(int x, int y, int width, int height) {
        RenderAPI.SetViewport(x, y, width, height);
    }

    public static uint CreateTexture() {
        return RenderAPI.CreateTexture();
    }

    // A helper function for quickly creating a 2d texture and assigning it with pixel data.
    public static uint CreateTexture(byte[] pixels, int pitch, PixelFormat format) {
        uint tex = RenderAPI.CreateTexture();
        SetTexturePixels(tex, pixels, pitch, format);
        return tex;
    }

    public static void SetTexturePixels(uint texture, byte[] pixels, int pitch, PixelFormat format) {
        RenderAPI.SetTexturePixels(texture, pixels, pitch, format);
    }

    // Update the camera's projection and start a new batch.
    public static void BeginScene(Matrix4x4 projection) {
        Projection = projection;
        StartBatch();
    }

    public static void EndScene() {
        Flush();
    }

    public static void StartBatch() {
        // Reset the pointers into the arrays.
        VertexIndex = 0;
        IndexCount = 0;
        TextureSlotIndex = 1; // The first texture slot will always be assigned with the fully white texture.
    }

    // Flush and start a new batch.
    public static void NextBatch() {
        Flush();
        StartBatch();
    }

    public static void Flush() {
        // Draw the quads.
        RenderAPI.PrepareDrawCall();
        RenderAPI.DrawIndexed(IndexCount);
    }

    public static void DrawQuad(float x, float y, float renderLayer, float width, float height, uint texture) {
        DrawQuad(x, y, renderLayer, width, height, texture, Vector4.One);
    }

    public static void DrawQuad(float x, float y, float renderLayer, float width, float height, Vector4 color) {
        DrawQuad(x, y, renderLayer, width, height, WhiteTexture, color);
    }

    public static void DrawQuad(float x, float y, float renderLayer, float width, float height, uint texture, Vector4 color) {
        // If the index buffer is full, a new batch must be made.
        if(IndexCount >= MaxIndices) {
            NextBatch();
        }

        // Create a transformation matrix for the quad.
        Matrix4x4 transform = Matrix4x4.CreateScale(width, height, 1.0f) * Matrix4x4.CreateTranslation(x, y, renderLayer);

        int texIndex = 0;
        // Find if the texture has already been inserted into the texture slots starting from index 1 to skip the fully white texture.
        for(int i = 1; i < TextureSlotIndex; i++) {
            if(TextureSlots[i] == texture) {
                texIndex = i;
                break;
            }
        }

        // Check if the texture has not been found in the texture slots.
        if(texIndex == 0) {
            // If the texture slots are full, a new batch must be made.
            if(TextureSlotIndex >= TextureSlots.Length) {
                NextBatch();
            }

            // Insert the texture at the tail of the texture slots.
            texIndex = TextureSlotIndex;
            TextureSlots[TextureSlotIndex++] = texture;
        }

        // Insert the 4 vertices that make out the quad into the vertex buffer.
        for(int i = 0; i < 4; i++) {
            Vertices[VertexIndex++] = new Vertex {
                Position = Vector4.Transform(Vector4.Transform(VertexPositions[i], transform), Projection),
                Color = color,
                TexCoord = VertexTexCoords[i],
                TexIndex = (float) texIndex,
            };
        }

        // Add 2 triangles to the index count.
        IndexCount += 6;
    }
}