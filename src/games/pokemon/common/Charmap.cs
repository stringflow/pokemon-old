using System.Text;

public class Charmap {

    public const byte Terminator = 0x50;

    public BiDictionary<byte, string> Map = new BiDictionary<byte, string>();

    public Charmap(string characters) {
        string[] arr = characters.Split(" ");
        for(int i = 0; i < arr.Length; i++) {
            Map[(byte) (0x80 + i)] = arr[i];
        }
        Map[0x7f] = " ";
    }

    public byte[] Encode(char c) {
        return Encode(c.ToString(), false);
    }

    public byte[] Encode(string text, bool terminator = true) {
        char[] chars = text.ToUpper().ToCharArray();
        byte[] bytes = new byte[chars.Length + (terminator ? 1 : 0)];
        for(int i = 0; i < chars.Length; i++) {
            bytes[i] = Map[chars[i].ToString()];
        }

        if(terminator) bytes[bytes.Length - 1] = Terminator;
        return bytes;
    }

    public string Decode(byte b) {
        return Map[b];
    }

    public string Decode(byte[] bytes) {
        StringBuilder sb = new StringBuilder(bytes.Length);
        foreach(byte b in bytes) {
            if(b == Terminator) break;
            sb.Append(Map[b]);
        }
        return sb.ToString();
    }
}