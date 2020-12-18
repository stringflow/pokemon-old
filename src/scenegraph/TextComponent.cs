public class TextComponent : Component {

    public string Text;
    public float Scale;

    public TextComponent(string text, float x, float y, float scale = 1) {
        Text = text;
        X = x;
        Y = y;
        Scale = scale;
    }

    public override void Render(GameBoy gb) {
        Renderer.DrawString(Text, X, Y, RenderLayer, Scale, System.Numerics.Vector4.One);
    }
}