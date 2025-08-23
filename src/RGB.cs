namespace MT.HexDump;

/// <summary>
/// RGB カラー構造体
/// </summary>
public record struct RGB
{
    public byte R;
    public byte G;
    public byte B;
    public RGB(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// HLS パラメータから <see cref="RGB"/> を返す
    /// </summary>
    /// <param name="h">色相 (Hue) 0 - 359</param>
    /// <param name="l">輝度 (Lightness) 0 - 100</param>
    /// <param name="s">彩度 (Saturation) 0 - 100</param>
    public static RGB FromHLS(int h, int l, int s)
    {
        if (h is > 360 or < 0)
            throw new ArgumentOutOfRangeException(nameof(h), h, "Hue must be between 0 and 360");
        if (l is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(l), l, "Lightness must be between 0 and 100");
        if (s is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(s), s, "Saturation must be between 0 and 100");

        double r; double g; double b;
        double max; double min;

        if (l > 50)
        {
            max = l + (s * (1.0 - (l / 100.0)));
            min = l - (s * (1.0 - (l / 100.0)));
        }
        else
        {
            max = l + (s * l / 100.0);
            min = l - (s * l / 100.0);
        }

        h = (h + 240) % 360;

        (r, g, b) = h switch
        {
            < 60 => (max, min + ((max - min) * h / 60.0), min),
            < 120 => (min + ((max - min) * (120 - h) / 60.0), max, min),
            < 180 => (min, max, min + ((max - min) * (h - 120) / 60.0)),
            < 240 => (min, min + ((max - min) * (240 - h) / 60.0), max),
            < 300 => (min + ((max - min) * (h - 240) / 60.0), min, max),
            _ => (max, min, min + ((max - min) * (360 - h) / 60.0))
        };

        return new((byte)Math.Round(r * 0xFF / 100.0),
                   (byte)Math.Round(g * 0xFF / 100.0),
                   (byte)Math.Round(b * 0xFF / 100.0));
    }

    /// <summary>
    /// ターミナル背景色のエスケープシーケンスを返す。
    /// 要 256color 対応ターミナル
    /// </summary>
    /// <returns><c>\e[48;2;{<see cref="R"/>};{<see cref="G"/>};{<see cref="B"/>}m</c></returns>
    public string ToTermBg()
    {
        return $"\u001b[48;2;{R};{G};{B}m";
    }
}
