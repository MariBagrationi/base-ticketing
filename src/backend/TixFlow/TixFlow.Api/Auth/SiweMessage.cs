using System.Text.RegularExpressions;

namespace TixFlow.Api.Auth;

public partial class SiweMessage
{
    public string Domain { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Statement { get; init; }
    public string Uri { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ChainId { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public string IssuedAt { get; init; } = string.Empty;

    public static SiweMessage? Parse(string message)
    {
        var match = SiweRegex().Match(message);
        if (!match.Success)
            return null;

        return new SiweMessage
        {
            Domain = match.Groups["domain"].Value,
            Address = match.Groups["address"].Value,
            Statement = match.Groups["statement"].Success ? match.Groups["statement"].Value.Trim() : null,
            Uri = match.Groups["uri"].Value,
            Version = match.Groups["version"].Value,
            ChainId = match.Groups["chainId"].Value,
            Nonce = match.Groups["nonce"].Value,
            IssuedAt = match.Groups["issuedAt"].Value
        };
    }

    [GeneratedRegex(
        @"^(?<domain>[^\s]+) wants you to sign in with your Ethereum account:\s*\n" +
        @"(?<address>0x[0-9a-fA-F]{40})\s*\n" +
        @"(?:\n(?<statement>[^\n]+)\s*\n)?" +
        @"\s*\n" +
        @"URI: (?<uri>[^\n]+)\n" +
        @"Version: (?<version>[^\n]+)\n" +
        @"Chain ID: (?<chainId>[^\n]+)\n" +
        @"Nonce: (?<nonce>[^\n]+)\n" +
        @"Issued At: (?<issuedAt>[^\n]+)",
        RegexOptions.Singleline)]
    private static partial Regex SiweRegex();
}
