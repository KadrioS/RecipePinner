using UnityEngine;

namespace ValheimRecipePinner
{
    public static class DebugLogger
    {
        private const string Prefix = "[RecipePinner]";

        /// <summary>
        /// Log general information (only when debug mode is enabled)
        /// </summary>
        public static void Log(string message)
        {
            if (IsDebugEnabled())
            {
                Debug.Log($"{Prefix} {message}");
            }
        }

        /// <summary>
        /// Log warnings (always shown)
        /// </summary>
        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        /// <summary>
        /// Log errors (always shown)
        /// </summary>
        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }

        /// <summary>
        /// Log exceptions (always shown)
        /// </summary>
        public static void Error(string message, System.Exception ex)
        {
            Debug.LogError($"{Prefix} {message}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Log detailed information for troubleshooting (only when debug mode is enabled)
        /// </summary>
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