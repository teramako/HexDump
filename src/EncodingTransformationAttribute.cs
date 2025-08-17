using System.Management.Automation;
using System.Text;

namespace MT.HexDump;

/// <summary>
/// PowerShell のパラメータを文字コードインスタンスへ変換する属性クラス
/// </summary>
public class EncodingTransformationAttribute : ArgumentTransformationAttribute
{
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        switch (inputData)
        {
            case Encoding:
                return inputData;
            case string name:
                return name.ToUpperInvariant() switch
                {
                    "ASCII" => Encoding.ASCII,
                    "LATIN1" => Encoding.Latin1,
                    // "UTF7" => Encoding.UTF7, // 使用しない
                    "UTF8" => Encoding.UTF8,
                    "UTF16" => Encoding.Unicode,
                    _ => Encoding.GetEncoding(name)
                };
            case int codePage:
                return Encoding.GetEncoding(codePage);
        }

        throw new ArgumentException($"Failed to transform to Encoding instance: {inputData}");
    }
}
