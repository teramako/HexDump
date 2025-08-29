using System.Text;

namespace MT.HexDump;

public class Config() : ICloneable
{
    private static Lazy<Config> _default = new(() => new());
    public static Config Default => _default.Value;

    /// <summary>
    /// Encoding used decode the bytes to runes
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Coloring setting
    /// </summary>
    public ColorType ColorType { get; set; } = ColorType.None;

    private int _initialHue = 0;
    /// <summary>
    /// Initial hue value for HLS colors (0-360)
    /// </summary>
    public int InitialHue
    {
        get => _initialHue;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(InitialHue));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 360, nameof(InitialHue));
            _initialHue = value;
        }
    }
    private int _defaultLightness = 30;
    /// <summary>
    /// Default lightness value for HLS colors (0-100)
    /// </summary>
    public int DefaultLightness
    {
        get => _defaultLightness;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(DefaultLightness));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(DefaultLightness));
            _defaultLightness = value;
        }
    }

    private int _defaultSaturation = 60;
    /// <summary>
    /// Default saturation value for HLS colors (0-100)
    /// </summary>
    public int DefaultSaturation
    {
        get => _defaultSaturation;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(DefaultSaturation));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(DefaultSaturation));
            _defaultSaturation = value;
        }
    }

    public string HexColumnSeparator { get; set; } = string.Empty;
    public string CharColumnSeparator { get; set; } = string.Empty;

    /// <summary>
    /// Letter indicating none of data
    /// </summary>
    public char NullLeter { get; set; } = ' ';

    /// <summary>
    /// Letter indicating data that could not be decoded to a character
    /// </summary>
    public char NonLetter { get; set; } = '.';

    private char[] _continutionLetters = ['←', '─', '─'];
    /// <summary>
    /// Character used to indicate the continution bytes in a multibyte characters
    /// </summary>
    public char[] ContinutionLetters
    {
        get => _continutionLetters;
        set {
            ArgumentOutOfRangeException.ThrowIfNotEqual(value.Length, 3, nameof(ContinutionLetters));
            _continutionLetters = value;
        }
    }

    /// <summary>
    /// Letters mapped to ASCII controll characters
    /// </summary>
    public char[] AsciiControlLetters { get; set; } =
    [
        '\u2400', // (0x00) NUL
        '\u2401', // (0x01) SOH
        '\u2402', // (0x02) STX
        '\u2403', // (0x03) ETX
        '\u2404', // (0x04) EOT
        '\u2405', // (0x05) ENQ
        '\u2406', // (0x06) ACK
        '\u2407', // (0x07) BEL
        '\u2408', // (0x08) BS
        '\u2409', // (0x09) HT
        '\u240A', // (0x0A) LF
        '\u240B', // (0x0B) VT
        '\u240C', // (0x0C) FF
        '\u240D', // (0x0D) CR
        '\u240E', // (0x0E) SO
        '\u240F', // (0x0F) SI

        '\u2410', // (0x10) DLE
        '\u2411', // (0x11) DC1
        '\u2412', // (0x12) DC2
        '\u2413', // (0x13) DC3
        '\u2414', // (0x14) DC4
        '\u2415', // (0x15) NAK
        '\u2416', // (0x16) SYN
        '\u2417', // (0x17) ETB
        '\u2418', // (0x18) CAN
        '\u2419', // (0x19) EM
        '\u241A', // (0x1A) SUB
        '\u241B', // (0x1B) ESC
        '\u241C', // (0x1C) FS
        '\u241D', // (0x1D) GS
        '\u241E', // (0x1E) RS
        '\u241F', // (0x1F) US

        '\u2420', // (0x20) SP
        '\u2421', // (0x21) DEL
    ];

    public object Clone()
    {
        return new Config()
        {
            Encoding = this.Encoding,
            ColorType = this.ColorType,
            InitialHue = this.InitialHue,
            DefaultLightness = this.DefaultLightness,
            DefaultSaturation = this.DefaultSaturation,
            HexColumnSeparator = this.HexColumnSeparator,
            CharColumnSeparator = this.CharColumnSeparator,
            NullLeter = this.NullLeter,
            NonLetter = this.NonLetter,
            ContinutionLetters = this.ContinutionLetters,
            AsciiControlLetters = this.AsciiControlLetters
        };
    }
}
