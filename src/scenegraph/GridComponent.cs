using System;
using System.Linq;
using System.Collections.Generic;

public class GridComponent : Component {

    public static Dictionary<Action, byte[]> Colors = new Dictionary<Action, byte[]>() {
        { Action.Up, new byte[] { 0xff, 0xff, 0x00 } },
        { Action.Down, new byte[] { 0xff, 0xff, 0x00 } },
        { Action.Left, new byte[] { 0xff, 0xff, 0x00 } },
        { Action.Right, new byte[] { 0xff, 0xff, 0x00 } },
        { Action.A, new byte[] { 0x00, 0xff, 0x00 } },
        { Action.A | Action.Up, new byte[] { 0x00, 0xff, 0x00 } },
        { Action.A | Action.Down, new byte[] { 0x00, 0xff, 0x00 } },
        { Action.A | Action.Left, new byte[] { 0x00, 0xff, 0x00 } },
        { Action.A | Action.Right, new byte[] { 0x00, 0xff, 0x00 } },
    };

    public byte SCX;
    public byte SCY;
    public int XOffset;
    public int YOffset;
    public Bitmap Map;
    public Bitmap Buffer;
    public ulong Texture;

    public GridComponent(float x, float y, float width, float height, float renderLayer, string path) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        RenderLayer = renderLayer;

        int minX = 0;
        int minY = 0;
        int maxX = 0;
        int maxY = 0;
        int xTile = 0;
        int yTile = 0;
        Action[] actions = ActionFunctions.PathToActions(path);
        Dictionary<(int, int), byte[]> tiles = new Dictionary<(int, int), byte[]>() {
            { (0,0), Colors[Action.Down] },
        };

        foreach(Action action in actions) {
            switch(action & ~Action.A) {
                case Action.Left: xTile--; break;
                case Action.Right: xTile++; break;
                case Action.Up: yTile--; break;
                case Action.Down: yTile++; break;
            }

            tiles[(xTile, yTile)] = Colors[action];

            minX = Math.Min((int) minX, (int) xTile);
            minY = Math.Min((int) minY, (int) yTile);
            maxX = Math.Max((int) maxX, (int) xTile);
            maxY = Math.Max((int) maxY, (int) yTile);
        }

        XOffset = -minX * 16;
        YOffset = -minY * 16;

        Buffer = new Bitmap((int) width, (int) height);
        Map = new Bitmap(Math.Max((int) width, (maxX - minX + 10) * 16), Math.Max((int) height, (maxY - minY + 9) * 16));

        Array.Fill<byte>(Map.Pixels, 0x00);
        for(int i = 0; i < tiles.Count; i++) {
            KeyValuePair<(int X, int Y), byte[]> tile = tiles.ElementAt(i);
            int xt = tile.Key.X - minX;
            int yt = tile.Key.Y - minY;
            byte[] color = tile.Value;
            Map.FillRect((xt + 4) * 16, (yt + 4) * 16, 16, 16, color[0], color[1], color[2], 0x80);
        }

        for(int i = 0; i < Map.Width / 16; i++) {
            Map.FillRect(i * 16, 0, 1, Map.Height, 0x00, 0x00, 0x00, 0xff);
        }

        for(int i = 0; i < Map.Height / 16; i++) {
            Map.FillRect(0, i * 16, Map.Width, 1, 0x00, 0x00, 0x00, 0xff);
        }

        Texture = Renderer.CreateTexture(Buffer.Width, Buffer.Height, PixelFormat.RGBA);
    }

    public override void BeginScene(GameBoy gb) {
        byte lcdc = gb.CpuRead(0xff40);
        byte wy = gb.CpuRead(0xff4a);
        byte wx = gb.CpuRead(0xff4b);
        bool windowShown = (lcdc & 0x20) > 0 && wx >= 0 && wx <= 166 && wy >= 0 && wy <= 143;

        byte newSCX = gb.CpuRead(0xff43);
        byte newSCY = gb.CpuRead(0xff42);
        int xDiff = CalcDifference(SCX, newSCX);
        int yDiff = CalcDifference(SCY, newSCY);
        if(!(newSCX == 0 && newSCY == 0 && (Math.Abs(xDiff) > 8 || Math.Abs(yDiff) > 8))) {
            XOffset += xDiff;
            YOffset += yDiff;
        }
        SCX = newSCX;
        SCY = newSCY;

        Map.SubBitmap(Buffer, XOffset, YOffset, Buffer.Width, Buffer.Height);

        byte[] state = gb.SaveState();
        for(int sprite = 0; sprite < 40; sprite++) {
            byte y = (byte) (state[gb.SaveStateLabels["hram"] + sprite * 4 + 0] - 16);
            byte x = (byte) (state[gb.SaveStateLabels["hram"] + sprite * 4 + 1] - 8);
            byte tile = state[gb.SaveStateLabels["hram"] + sprite * 4 + 2];
            byte flags = state[gb.SaveStateLabels["hram"] + sprite * 4 + 3];

            if(x >= 0 && x < 160 && y >= 0 && y < 144) {
                byte[] spritePixels = state.Subarray(gb.SaveStateLabels["vram"] + tile * 16 + ((flags >> 3) & 0x1) * 0x2000, 16);
                for(int j = 0; j < 8; j++) {
                    int jj = (flags & 0x10) > 0 ? 7 - j : j;
                    byte top = spritePixels[j * 2 + 0];
                    byte bot = spritePixels[j * 2 + 1];
                    for(int k = 0; k < 8; k++) {
                        int kk = (flags & 0x20) > 0 ? 7 - k : k;
                        if(((top >> (7 - k)) & 1) + ((bot >> (7 - k)) & 1) * 2 > 0) {
                            int xPixel = x + kk;
                            int yPixel = y + jj;
                            if(xPixel >= 0 && yPixel >= 0 && xPixel < Buffer.Width && yPixel < Buffer.Height) {
                                Buffer.SetPixel(xPixel, yPixel, 0x00, 0x00, 0x00, 0x00);
                            }
                        }
                    }
                }
            }
        }

        if(windowShown) HideMenuTiles(state, gb.SaveStateLabels["vram"] + ((lcdc & 0x40) > 0 ? 0x1c00 : 0x1800), wx - 7, wy);
        else HideMenuTiles(state, gb.SaveStateLabels["vram"] + ((lcdc & 0x8) > 0 ? 0x1c00 : 0x1800), 0, 0);

        Renderer.SetTexturePixels(Texture, Buffer.Pixels, Buffer.Width, PixelFormat.RGBA);
    }

    private void HideMenuTiles(byte[] state, int vramOffset, int xOffs, int yOffs) {
        for(int x = 0; x < 20; x++) {
            for(int y = 0; y < 18; y++) {
                int tile = x + y * 32;
                byte tileIdx = state[vramOffset + tile];
                if(tileIdx >= 0x60) {
                    Buffer.FillRect(x * 8 + xOffs, y * 8 + yOffs, 8, 8, 0x00, 0x00, 0x00, 0x00);
                }
            }
        }
    }

    public override void Render(GameBoy gb) {
        Renderer.DrawQuad(X, Y, RenderLayer, Width, Height, Texture);
    }

    private int CalcDifference(int a, int b) {
        int x = (int) ((b - a) & 0xff);
        int y = (int) ((a - b) & 0xff);
        return x <= 128 ? x : -y;
    }
}