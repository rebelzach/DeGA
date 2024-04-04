using System.Text;
using System.Text.RegularExpressions;

namespace Wolder.CSharp.OpenAI.Actions;

public class ParseUtilities
{
    public static string ExtractCodeBlocks(string input)
    {
        // Pattern to match code blocks, capturing the content inside the backticks
        string pattern = @"```(?:[^`\n]*\n)?(.*?)```";
        var matches = Regex.Matches(input, pattern, RegexOptions.Singleline);

        // Use StringBuilder to concatenate all the code block contents
        var result = new StringBuilder();
        foreach (Match match in matches)
        {
            // Append the content of the code block to the result
            if (match.Groups.Count > 1)
            {
                result.AppendLine(match.Groups[1].Value);
            }
        }

        return result.ToString().TrimEnd(); // TrimEnd to remove the last newline added by AppendLine
    }
}