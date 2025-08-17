using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace MT.HexDump;

public class EncodingArgumentCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(string commandName,
                                                          string parameterName,
                                                          string wordToComplete,
                                                          CommandAst commandAst,
                                                          IDictionary fakeBoundParameters)
    {
        return Encoding.GetEncodings()
                       .Select(enc => (enc.Name, enc.CodePage, enc.DisplayName))
                       .Union(Aliaes)
                       .Where(enc => string.IsNullOrEmpty(wordToComplete)
                                     || enc.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                                     || enc.CodePage.ToString().StartsWith(wordToComplete, StringComparison.Ordinal)
                                     || enc.DisplayName.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
                       .Select(static enc => new CompletionResult(enc.Name,
                                                                  enc.Name,
                                                                  CompletionResultType.ParameterValue,
                                                                  $"{enc.Name} CodePage: {enc.CodePage} Description: {enc.DisplayName}"));
    }

    private static (string Name, int CodePage, string DisplayName)[] Aliaes = [
        ("ASCII", 20127, "Alias to US-ASCII"),
        ("Latin1", 28591, "Alias to Latin1 (Western European (ISO))"),
        ("UTF8", 65001, "Alias to Unicode (UTF-8)"),
        ("UTF16", 1200, "Alias to Unicode (UTF-16)"),
    ];
}
