---
document type: cmdlet
external help file: HexDump-Help.xml
HelpUri: ''
Locale: ja-JP
Module Name: HexDump
ms.date: 08/24/2025
PlatyPS schema version: 2024-05-01
title: Write-HexDump
---

# Write-HexDump

## SYNOPSIS

{{ Fill in the Synopsis }}

## SYNTAX

### Data

```
Write-HexDump [-Data] <byte[]> [-Encoding <Encoding>] [-Offset <long>] [-Length <int>]
 [-Format <string>] [-Color <ColorType>] [<CommonParameters>]
```

### Stream

```
Write-HexDump [-Stream] <Stream> [-Encoding <Encoding>] [-Offset <long>] [-Length <int>]
 [-Format <string>] [-Color <ColorType>] [<CommonParameters>]
```

## ALIASES

This cmdlet has the following aliases,
  {{Insert list of aliases}}

## DESCRIPTION

{{ Fill in the Description }}

## EXAMPLES

### Example 1

{{ Add example description here }}

## PARAMETERS

### -Color

{{ Fill Color Description }}

```yaml
Type: ColorType
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
AcceptedValues: []
HelpMessage: ''
```

### -Data

{{ Fill Data Description }}

```yaml
Type: Byte[]
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

{{ Fill Encoding Description }}

```yaml
Type: Encoding
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

{{ Fill Format Description }}

```yaml
Type: String
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
AcceptedValues: []
HelpMessage: ''
```

### -Length

{{ Fill Length Description }}

```yaml
Type: Int32
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

{{ Fill Offset Description }}

```yaml
Type: Int64
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

### -Stream

{{ Fill Stream Description }}

```yaml
Type: Stream
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

### System.Byte

{{ Fill in the Description }}

### System.IO.Stream

{{ Fill in the Description }}

### System.Byte[]

{{ Fill in the Description }}

## OUTPUTS

### MT.HexDump.CharCollectionRow

{{ Fill in the Description }}

## NOTES

{{ Fill in the Notes }}

## RELATED LINKS

{{ Fill in the related links here }}

