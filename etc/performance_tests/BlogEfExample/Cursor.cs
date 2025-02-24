using System.Text;

namespace PostEfExample;

public static class CursorDecoder
{
    public static string Decode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        var bs = Convert.FromBase64String(input);
        return Encoding.UTF8.GetString(bs);
    }

    public static string Encode(string input)
    {
        var output = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        output = output.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return output;
    }
}