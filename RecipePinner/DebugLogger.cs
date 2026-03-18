using UnityEngine;

namespace ValheimRecipePinner
{
    public static class DebugLogger
    {
        private const string Prefix = "[RecipePinner]";

        // Log general information (only when debug mode is enabled)
        public static void Log(string message)
        {
            if (IsDebugEnabled())
            {
                Debug.Log($"{Prefix} {message}");
            }
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        // Log errors (always shown)
        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }

        // Log exceptions (always shown)
        public static void Error(string message, System.Exception ex)
        {
            Debug.LogError($"{Prefix} {message}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }

        // Log detailed information for troubleshooting (only when debug mode is enabled)
        public static void Verbose(string message)
        {
            if (IsDebugEnabled())
            {
                Debug.Log($"{Prefix} [VERBOSE] {message}");
            }
        }

        private static bool IsDebugEnabled()
        {
            return RecipePinnerPlugin.Instance != null &&
                   RecipePinnerPlugin.EnableDebugLogging != null &&
                   RecipePinnerPlugin.EnableDebugLogging.Value;
        }
    }
}