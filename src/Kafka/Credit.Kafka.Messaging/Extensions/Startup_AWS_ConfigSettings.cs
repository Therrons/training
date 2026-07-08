namespace Credit.Kafka.Messaging.Extensions
{
    public static class Startup_AWS_ConfigSettings
    {
        private static volatile Dictionary<string, bool> _newConfig;
        private static readonly object _lock = new object();

        public static void Set_Startup_AWS_ConfigSettings(Dictionary<string, bool> newConfig)
        {
            lock (_lock)
            {
                _newConfig = newConfig; 
            }
        }

        public static Dictionary<string, bool> Get_Startup_AWS_ConfigSettings_To_Dictionary()
        {
            return _newConfig ?? [];
        }

        public static List<(string, bool)> Get_Startup_AWS_ConfigSettings_To_List()
        {
            return _newConfig?
                .Select(kv => (kv.Key, kv.Value))
                .ToList<(string, bool)>() ?? [];
        }
    }
}
