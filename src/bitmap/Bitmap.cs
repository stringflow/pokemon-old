using System.IO;

public class Bitmap {

    public int Width;
    public int Height;
    public byte[] Pixels; // In 32-bit rgb format

    public Bitmap(int width, int height) : this(width, height, new byte[width * height * 4]) {
    }

    public Bitmap(int width, int height, byte[] pixels) {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public Bitmap(string file) {
        ByteStream stream = new ByteStream(File.ReadAllBytes(file));
        string extension = Path.GetExtension(file);
        switch(extension.ToLower()) {
            case ".bmp": BMP.Decode(stream, this); break;
            case ".png": PNG.Decode(stream, this); break;
            default:
                Debug.Assert(false, "Image file extension not supported: {0}", extension);
                break;
        }
    }

    public void Save(string file) {
        string extension = Path.GetExtension(file);
        byte[] data = null;
        switch(extension.ToLower()) {
            case ".bmp": data = BMP.Encode(this); break;
            case ".png": data = PNG.Encode(this); break;
            default:
                Debug.Assert(false, "Image file extension not supported: {0}", extension);
                break;
        }

        File.WriteAllBytes(file, data);
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xff) {
        int offset = (x + y * Width) * 4;
        Pixels[offset + 0] = r;
        Pixels[offset + 1] = g;
        Pixels[offset + 2] = b;
        Pixels[offset + 3] = a;
    }

    public byte this[int offset] {
        get { return Pixels[offset]; }
        set { Pixels[offset] = value; }
    }
}