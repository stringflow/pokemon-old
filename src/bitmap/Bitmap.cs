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

    public byte this[int offset] {
        get { return Pixels[offset]; }
        set { Pixels[offset] = value; }
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 0xff) {
        int offset = (x + y * Width) * 4;
        Pixels[offset + 0] = r;
        Pixels[offset + 1] = g;
        Pixels[offset + 2] = b;
        Pixels[offset + 3] = a;
    }

    public void Unpack2BPP(byte[] data) {
        byte[][] pal = new byte[][] {
                    new byte[] { 232, 232, 232 },
                    new byte[] { 160, 160, 160 },
                    new byte[] { 88, 88, 88 },
                    new byte[] { 16, 16, 16 }};

        int w = Width / 8;
        for(int i = 0; i < data.Length / 16; i++) {
            for(int j = 0; j < 8; j++) {
                byte top = data[i * 16 + j * 2 + 0];
                byte bot = data[i * 16 + j * 2 + 1];
                for(int k = 0; k < 8; k++) {
                    byte[] col = pal[(byte) (((top >> (7 - k)) & 1) + ((bot >> (7 - k)) & 1) * 2)];
                    int idx = ((i / w) * (w * 8) * 8 + j * (w * 8) + (i % w) * 8 + k) * 4;
                    Pixels[idx + 0] = col[0];
                    Pixels[idx + 1] = col[1];
                    Pixels[idx + 2] = col[2];
                    Pixels[idx + 3] = 0xff;
                }
            }
        }
    }
}