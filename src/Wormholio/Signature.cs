using System.Text.RegularExpressions;

namespace Wormholio;

public sealed partial record Signature(string Id, string Type, string Group, string Name, double Signal, string Distance)
{
    public static bool TryParseClipboard(string input, out List<Signature> list)
    {
        list = [];
        foreach (var line in input.Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var match = SignatureRegex().Match(line);
            if (!match.Success)
                return false;

            var id = match.Groups[1].Value;
            var type = match.Groups[2].Value;
            var group = match.Groups[3].Value;
            var name = match.Groups[4].Value;
            var signal = double.Parse(match.Groups[5].Value);
            var distance = match.Groups[6].Value;
            list.Add(new Signature(id, type, group, name, signal, distance));
        }

        return true;
    }

    [GeneratedRegex(@"^([A-Z]{3}-[0-9]{3})\t(.*?)\t(.*?)\t(.*?)\t([0-9]+\.[0-9+])%\t(.*?)$")]
    private static partial Regex SignatureRegex();
}