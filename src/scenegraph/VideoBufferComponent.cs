public class VideoBufferComponent : Component {

    public uint Texture;

    public VideoBufferComponent(float x, float y, float width, float height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Texture = Renderer.CreateTexture();
    }

    public override void BeginScene(GameBoy gb) {
        Renderer.SetTexturePixels(Texture, gb.VideoBuffer, 160, PixelFormat.BGRA);
    }

    public override void Render(GameBoy gb) {
        Renderer.DrawQuad(X, Y, RenderLayer, Width, Height, Texture);
    }
}