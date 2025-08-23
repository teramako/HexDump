namespace MT.HexDump;

[Flags]
public enum CharType : byte
{
    /// <summary>
    /// Indicates unallocated.
    /// </summary>
    Empty                 = 0,
    /// <summary>
    /// Flag indicating that the character (byte value) could not be decoded.
    /// </summary>
    Binary                = 0b0000_0001,
    /// <summary>
    /// Flag indicating that the character could be decoded.
    /// </summary>
    Char                  = 0b0000_0010,
    /// <summary>
    /// 1-byte character.
    /// </summary>
    SingleByteChar        = 0b0000_0100 | Char,
    /// <summary>
    /// Flag indicating that the character is a multibyte character.
    /// </summary>
    MultiByteChar         = 0b0000_1000 | Char,
    /// <summary>
    /// Flag indicating continution byte value in multibyte character.
    /// </summary>
    ContinutionByte       = 0b0001_0000,
    /// <summary>
    /// Flag indicating the FIRST of continution byte values of a multibyte character.
    /// </summary>
    First                 = 0b0010_0000,
    /// <summary>
    /// First of continution byte values for a multibyte character.
    /// </summary>
    ContinutionFirstByte  = ContinutionByte | First,
    /// <summary>
    /// Flag indicating the LAST of continution byte values of a multibyte character.
    /// </summary>
    Last                  = 0b0100_0000,
    /// <summary>
    /// First and last of continution byte values for a multibyte character.
    /// (This would represent the second byte of a 2-byte character.)
    /// </summary>
    ContinutionFirstAndLastByte = ContinutionByte | First | Last,
    /// <summary>
    /// Last of continution byte values for a multibyte character.
    /// </summary>
    ContinutionLastByte = ContinutionByte | Last,
}
