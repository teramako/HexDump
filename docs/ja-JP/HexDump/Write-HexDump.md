---
document type: cmdlet
external help file: HexDump-Help.xml
HelpUri: ''
Locale: ja-JP
Module Name: HexDump
ms.date: 08/30/2025
PlatyPS schema version: 2024-05-01
title: Write-HexDump
---

# Write-HexDump

## SYNOPSIS

データの16進数ダンプをする

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

Unix系コマンドの `hexdump` のように与えられたバイト列やストリーム、ファイルを読み、バイト単位の16進数ダンプをします。

またバイト列のテキスト変換をした結果も出力します。

## EXAMPLES

### Example 1. ASCIIコード一覧を出す

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

### Example 2. 16進数ダンプとテキスト化データを一つの列にする

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

16進数ダンプ列とテキスト化列の配色設定

- `None`: 色なし
- `ByByte`: バイト値に基づいた配色をする
- `ByUnicodeCategtory`: Unicodeカテゴリーに基づいた配色をする
- `ByCharType`: ASCII制御文字/ASCII文字/マルチバイト文字/文字にデコードできなかった値、等の分類で配色をする

`Config` の設定より優先されます。

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

エンコーディングや配色設定などが入った設定群

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

ダンプを行う対象のバイト配列

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

バイト列を文字にデコードする際に使用するエンコーディング

`System.Text.Encoding` 型を直接指定するか、
`System.Text.Encoding.GetEncoding(name or codepage)` に使用可能な名前もしくはコードページの指定も可能。

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

出力フォーマット。 以下の指定が可能。

- `SplitHexAndChars`
- `UnifyHexAndChars`

このパラメータは、 `Write-HexDump .... | Format-Table view {フォーマット名}` へのシンタックスシュガーです。

ダンプ出力結果を変数に格納したい場合は、このパラメータを指定しないことを推奨します。

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

ダンプ開始位置からの終了までの長さ。
未指定の場合は、最後まで。

```yaml
Type: System.Int32
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

### -Offset

ダンプ開始位置。
未指定の場合は最初から(`0`)

```yaml
Type: System.Int64
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

### -Path

ダンプを行う対象ファイルパス

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

ダンプ対象となるストリーム・オブジェクト

ダンプが完了すると、このストリーム・オブジェクトは閉じら(`Dispose()`さ)れます。

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

ダンプ対象のバイト列。 (`-Data`パラメータ)

パイプラインからバイト列を与えるのには、少し工夫が必要なことに注意ください。
`Write-Output` (Alias: `echo`) に `NoEnumerate` パラメーターを付ける必要があるでしょう。

```powershell
$bytes = @(...)
Write-Output -NoEnumerate $bytes | Write-HexDump ...
```

### System.IO.Stream

ダンプ対象のストリーム・オブジェクト。 (`-Stream`パラメーター)

### System.String

ダンプ対象のファイルパス。 (`-Path`パラメータ)

## OUTPUTS

### MT.HexDump.CharCollectionRow

各16バイト分の情報を収めた行オブジェクト

## NOTES

## RELATED LINKS

