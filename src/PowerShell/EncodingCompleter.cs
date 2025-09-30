using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using EncodingData = (string Name, int CodePage, string DisplayName);

namespace MT.HexDump.PowerShell;

public class EncodingArgumentCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName,
                                                          string parameterName,
                                                          string wordToComplete,
                                                          CommandAst commandAst,
                                                          IDictionary fakeBoundParameters)
    {
        var encodings = Encoding.GetEncodings()
                                .Select(enc => (enc.Name, enc.CodePage, enc.DisplayName))
                                .Union(Aliaes);
        if (string.IsNullOrEmpty(wordToComplete))
        {
            foreach (var enc in encodings)
            {
                yield return GetCompletionResult(enc);
            }
            yield break;
        }

        bool completed = false;
        foreach (var enc in encodings)
        {
            if (enc.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                completed = true;
                yield return GetCompletionResult(enc);
            }
        }

        if (completed)
        {
            yield break;
        }

        foreach (var enc in encodings)
        {
            if(enc.CodePage.ToString().StartsWith(wordToComplete, StringComparison.Ordinal)
               || enc.DisplayName.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                yield return GetCompletionResult(enc);
            }
        }
    }

    private static CompletionResult GetCompletionResult(EncodingData enc)
    {
        return new CompletionResult(enc.Name,
                                    enc.Name,
                                    CompletionResultType.ParameterValue,
                                    $"{enc.Name} CodePage: {enc.CodePage} Description: {enc.DisplayName}");
    }

    private static EncodingData[] Aliaes = [
        ("ASCII", 20127, "Alias to US-ASCII"),
        ("Latin1", 28591, "Alias to Latin1 (Western European (ISO))"),
        ("UTF8", 65001, "Alias to Unicode (UTF-8)"),
        ("UTF16", 1200, "Alias to Unicode (UTF-16)"),
    ];
}
