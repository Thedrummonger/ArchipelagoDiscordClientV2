namespace ArchipelagoDiscordClientLegacy.Helpers
{
    internal class NetworkHelpers
    {
        /// <summary>
        /// Parses an IP address string, extracting the IP and port if specified.
        /// </summary>
        /// <param name="input">The input string containing the IP address and optional port.</param>
        /// <returns>
        /// A tuple containing the extracted IP address and port number. 
        /// If no port is specified, the default AP port (38281) is used.
        /// If the input is null or empty, returns (null, 0).
        /// </returns>
        public static (string? Ip, int Port) ParseIpAddress(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (null, 0);
            var parts = input.Split(':', 2);
            string ip = parts[0];
            int port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 38281;
            return (ip, port);
        }
    }
}
