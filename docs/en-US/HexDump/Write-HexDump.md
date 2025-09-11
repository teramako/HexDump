---
document type: cmdlet
external help file: HexDump-Help.xml
HelpUri: ''
Locale: en-US
Module Name: HexDump
ms.date: 08/30/2025
PlatyPS schema version: 2024-05-01
title: Write-HexDump
---

# Write-HexDump

## SYNOPSIS

Dump the data hexadecimal.

## SYNTAX

### Data

```
Write-HexDump [-Data] <byte[]> [-Config <Config>] [-Encoding <Encoding>] [-Offset <long>]
 [-Length <int>] [-Format <string>] [-Color <ColorType>] [<CommonParameters>]
```

### Stream

```
Write-HexDump [-Stream] <Stream> [-Config <Config>] [-Encoding <Encoding>] [-Offset <long>]
 [-Length <int>] [-Format <string>] [-Color <ColorType>] [<CommonParameters>]
```

### Path

```
Write-HexDump [-Path] <string> [-Config <Config>] [-Encoding <Encoding>] [-Offset <long>]
 [-Length <int>] [-Format <string>] [-Color <ColorType>] [<CommonParameters>]
```

## ALIASES

hexdump

## DESCRIPTION

Like the `hexdump` Unix-like command, it reads a given byte sequence, stream, or file and dumps it hexadecimal.
It also outputs the result of the text conversion of the byte sequence.

## EXAMPLES

### Example 1. List ASCII codes

```powershell
hexdump -Data @(0x00..0x7F)
```

Output:

```
Row        Hex   2  3  4  5  6  7  8  9  A  B  C  D  E  F   C 1 2 3 4 5 6 7 8 9 A B C D E F
---        ----------------------------------------------   -------------------------------
0x00000000 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F  ␀ ␁ ␂ ␃ ␄ ␅ ␆ ␇ ␈ ␉ ␊ ␋ ␌ ␍ ␎ ␏
0x00000010 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F  ␐ ␑ ␒ ␓ ␔ ␕ ␖ ␗ ␘ ␙ ␚ ␛ ␜ ␝ ␞ ␟
0x00000020 20 21 22 23 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F    ! " # $ % & ' ( ) * + , - . /
0x00000030 30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F  0 1 2 3 4 5 6 7 8 9 : ; < = > ?
0x00000040 40 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F  @ A B C D E F G H I J K L M N O
0x00000050 50 51 52 53 54 55 56 57 58 59 5A 5B 5C 5D 5E 5F  P Q R S T U V W X Y Z [ \ ] ^ _
0x00000060 60 61 62 63 64 65 66 67 68 69 6A 6B 6C 6D 6E 6F  ` a b c d e f g h i j k l m n o
0x00000070 70 71 72 73 74 75 76 77 78 79 7A 7B 7C 7D 7E 7F  p q r s t u v w x y z { | } ~ ␡

```

### Example 2. Hexadecimal dump and textualized data in one column

```powershell
hexdump -Data @(0x00..0x7F) -Format UnifyHexAndChars
```

Output:

```
Row        Hex and Letters
---        ---------------
0x00000000 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
           ␀  ␁  ␂  ␃  ␄  ␅  ␆  ␇  ␈  ␉  ␊  ␋  ␌  ␍  ␎  ␏
0x00000010 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F
           ␐  ␑  ␒  ␓  ␔  ␕  ␖  ␗  ␘  ␙  ␚  ␛  ␜  ␝  ␞  ␟
0x00000020 20 21 22 23 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F
              !  "  #  $  %  &  '  (  )  *  +  ,  -  .  /
0x00000030 30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F
           0  1  2  3  4  5  6  7  8  9  :  ;  <  =  >  ?
0x00000040 40 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F
           @  A  B  C  D  E  F  G  H  I  J  K  L  M  N  O
0x00000050 50 51 52 53 54 55 56 57 58 59 5A 5B 5C 5D 5E 5F
           P  Q  R  S  T  U  V  W  X  Y  Z  [  \  ]  ^  _
0x00000060 60 61 62 63 64 65 66 67 68 69 6A 6B 6C 6D 6E 6F
           `  a  b  c  d  e  f  g  h  i  j  k  l  m  n  o
0x00000070 70 71 72 73 74 75 76 77 78 79 7A 7B 7C 7D 7E 7F
           p  q  r  s  t  u  v  w  x  y  z  {  |  }  ~  ␡
```

## PARAMETERS

### -Color

Color scheme settings for hexadecimal dump columns and textualized columns

- `None`: no color
- `ByByte`: color scheme based on byte values
- `ByUnicodeCategory`: color scheme based on Unicode categories
- `ByCharType`: color scheme based on ASCII control characters, ASCII characters and multi-byte characters values that could not be decoded into characters, etc.

It overrides the `Config` setting.

```yaml
Type: MT.HexDump.ColorType
DefaultValue: ''
SupportsWildcards: false
Aliases:
- c
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- None
- ByByte
- ByUnicodeCategory
- ByCharType
HelpMessage: ''
```

### -Config

A set of settings including encoding, color scheme settings, etc.

```yaml
Type: MT.HexDump.Config
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Data

Byte array to be dumped

```yaml
Type: System.Byte[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- d
ParameterSets:
- Name: Data
  Position: 0
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Encoding

Encoding used to decode byte strings into characters

You can also specify the `System.Text.Encoding` type directly,
or specify a name or codepage that can be used in `System.Text.Encoding.GetEncoding(name or codepage)`.

```yaml
Type: System.Text.Encoding
DefaultValue: ''
SupportsWildcards: false
Aliases:
- e
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Format

Output Format. The following can be specified

- `SplitHexAndChars`
- `UnifyHexAndChars`

This parameter is a syntax sugar to `Write-HexDump .... | Format-Table -view {format name}`.

If you wish to store the dump output results in a variable, it is recommended that you do not specify this parameter.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- f
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- SplitHexAndChars
- UnifyHexAndChars
HelpMessage: ''
```

### -Length

Length from the dump start position to the end.
If not specified, to the end.

```yaml
Type: System.Int32
DefaultValue: 0
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Offset

Dump start position.
If not specified, read from the beginning.

```yaml
Type: System.Int64
DefaultValue: 0
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Target file path to dump

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Path
  Position: 0
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Stream

Stream object to be dumped.
When dumping is complete, this stream object is closed (`Dispose()`).

```yaml
Type: System.IO.Stream
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Stream
  Position: 0
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Byte[]

Byte sequence to dump. (See: `-Data` parameter)

Note that giving a sequence of bytes from the pipeline requires a little ingenuity.
You will need to add a `NoEnumerate` parameter to `Write-Output` (Alias: `echo`).

```powershell
$bytes = @(...)
Write-Output -NoEnumerate $bytes | Write-HexDump ...
```

### System.IO.Stream

The stream object to dump. (See: `-Stream` parameter)


### System.String

File path to dump. (See: `-Path` parameter)


## OUTPUTS

### MT.HexDump.CharCollectionRow

Row objects containing 16 bytes of information each

## NOTES

## RELATED LINKS

