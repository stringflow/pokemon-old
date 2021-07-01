using System;
using static Tests;

public static class BitmapTests {

    public static Random Random = new Random();

    public static void RunAllTests() {
        RunAllTestsInFile(typeof(BitmapTests));
    }

    [Test]
    public static void BMPCodec() {
        Test("bmp-codec", () => TestCodec("bmp-test.bmp"));
    }

    [Test]
    public static void PNGCodec() {
        Test("png-codec", () => TestCodec("png-test.png"));
    }

    [Test]
    public static void Screenshot() {
        Test("gb-screenshot", () => {
            Red gb1 = new Red(null, false);
            gb1.RunUntil("TitleScreenScrollInMon");
            Bitmap bitmap1 = gb1.Screenshot();

            Red gb2 = new Red();
            gb2.Show();
            gb2.RunUntil("TitleScreenScrollInMon");
            Bitmap bitmap2 = gb2.Screenshot();

            gb2.Dispose();

            return TestBitmaps(bitmap1, bitmap2);
        });
    }

    private static (string, string) TestCodec(string file) {
        Bitmap encode = RandomBitmap();
        encode.Save(file);
        Bitmap decode = new Bitmap(file);
        var result = TestBitmaps(encode, decode);
        System.IO.File.Delete(file);
        return result;
    }

    private static (string, string) TestBitmaps(Bitmap bitmap1, Bitmap bitmap2) {
        string expected;
        string got;

        expected = bitmap1.Width + "x" + bitmap1.Height;
        got = bitmap2.Width + "x" + bitmap2.Height;
        if(expected != got) return (expected, got);

        for(int y = 0; y < bitmap1.Height; y++) {
            for(int x = 0; x < bitmap1.Width; x++) {
                int offs = (x + y * bitmap1.Width) * 4;
                int encodePixel = bitmap1.Pixels[offs] << 24 | bitmap1.Pixels[offs + 1] << 16 | bitmap1.Pixels[offs + 2] << 8 | bitmap1.Pixels[offs + 3];
                int decodePixel = bitmap2.Pixels[offs] << 24 | bitmap2.Pixels[offs + 1] << 16 | bitmap2.Pixels[offs + 2] << 8 | bitmap2.Pixels[offs + 3];
                expected = string.Format("{0},{1}=0x{2:x8}", x, y, encodePixel);
                got = string.Format("{0},{1}=0x{2:x8}", x, y, decodePixel);
                if(expected != got) {
                    return (expected, got);
                }
            }
        }

        return ("", "");
    }

    public static Bitmap RandomBitmap() {
        Bitmap bitmap = new Bitmap(256, 256);
        Random.NextBytes(bitmap.Pixels);
        return bitmap;
    }
}