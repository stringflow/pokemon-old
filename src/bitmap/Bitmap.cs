using System.IO;
using System.Linq;

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

    public void DrawPixel(int x, int y, byte r, byte g, byte b, byte a = 0xff) {
        int offset = (x + y * Width) * 4;
        byte thisR = Pixels[offset + 0];
        byte thisG = Pixels[offset + 1];
        byte thisB = Pixels[offset + 2];
        byte otherAmount = a;
        byte thisAmount = (byte) (255 - a);
        Pixels[offset + 0] = (byte) ((thisR * thisAmount + r * otherAmount) >> 8);
        Pixels[offset + 1] = (byte) ((thisG * thisAmount + g * otherAmount) >> 8);
        Pixels[offset + 2] = (byte) ((thisB * thisAmount + b * otherAmount) >> 8);
    }

    public void DrawBitmap(Bitmap src, int xOffs, int yOffs) {
        for(int x = 0; x < src.Width; x++) {
            int xx = x + xOffs;
            if(xx < 0 || xx >= Width) continue;
            for(int y = 0; y < src.Height; y++) {
                int yy = y + yOffs;
                if(yy < 0 || yy >= Height) continue;
                int srcIndex = (x + y * src.Width) * 4;
                DrawPixel(xx, yy, src.Pixels[srcIndex + 0], src.Pixels[srcIndex + 1], src.Pixels[srcIndex + 2], src.Pixels[srcIndex + 3]);
            }
        }
    }

    public void Unpack2BPP(byte[] data, byte[][] pal, bool transparent = false) {
        Unpack2BPP(data, new byte[][][] { pal }, null, transparent);
    }

    public void Unpack2BPP(byte[] data, byte[][][] pal, byte[] palMap, bool transparent = false) {
        int w = Width / 8;
        for(int i = 0; i < data.Length / 16; i++) {
            int palMapIndex = palMap == null ? 0 : palMap[i];
            for(int j = 0; j < 8; j++) {
                byte top = data[i * 16 + j * 2 + 0];
                byte bot = data[i * 16 + j * 2 + 1];
                for(int k = 0; k < 8; k++) {
                    int idx = ((i / w) * (w * 8) * 8 + j * (w * 8) + (i % w) * 8 + k) * 4;
                    int palIdx = (byte) (((top >> (7 - k)) & 1) + ((bot >> (7 - k)) & 1) * 2);
                    byte[] col = pal[palMapIndex][palIdx];
                    Pixels[idx + 0] = col[0];
                    Pixels[idx + 1] = col[1];
                    Pixels[idx + 2] = col[2];
                    Pixels[idx + 3] = (byte) (transparent && palIdx == 0 ? 0x00 : 0xff);
                }
            }
        }
    }

    public void Flip(bool xflip, bool yflip) {
        byte[] newPixels = new byte[Pixels.Length];
        for(int srcX = 0; srcX < Width; srcX++) {
            int destX = xflip ? Width - srcX - 1 : srcX;
            for(int srcY = 0; srcY < Height; srcY++) {
                int destY = yflip ? Height - srcY - 1 : srcY;
                int srcIdx = (srcX + srcY * Width) * 4;
                int destIdx = (destX + destY * Width) * 4;
                newPixels[destIdx + 0] = Pixels[srcIdx + 0];
                newPixels[destIdx + 1] = Pixels[srcIdx + 1];
                newPixels[destIdx + 2] = Pixels[srcIdx + 2];
                newPixels[destIdx + 3] = Pixels[srcIdx + 3];
            }
        }
        Pixels = newPixels;
    }

    public void RemapRedAndBlueChannels() {
        for(int i = 0; i < Width * Height; i++) {
            byte temp = Pixels[i * 4 + 0];
            Pixels[i * 4 + 0] = Pixels[i * 4 + 2];
            Pixels[i * 4 + 2] = temp;
        }
    }
}