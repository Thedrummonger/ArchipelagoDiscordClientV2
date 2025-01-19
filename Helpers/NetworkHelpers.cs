namespace ArchipelagoDiscordClientLegacy.Helpers
{
    internal class NetworkHelpers
    {
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
