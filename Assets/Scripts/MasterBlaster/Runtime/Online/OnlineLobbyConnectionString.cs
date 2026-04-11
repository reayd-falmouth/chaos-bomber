namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>Trim/normalize PlayFab lobby connection strings pasted from the host.</summary>
    public static class OnlineLobbyConnectionString
    {
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            return raw.Trim();
        }
    }
}
