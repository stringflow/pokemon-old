public class SpriteComponent : Component {

    public uint Texture;

    public SpriteComponent(float x, float y, float width, float height, uint texture) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Texture = texture;
    }

    public override void Render(GameBoy gb) {
        Renderer.DrawQuad(X, Y, RenderLayer, Width, Height, Texture);
    }
}