using System;
using System.IO;

namespace fraud_poc_project.Utilities
{
    /// <summary>
    /// Utility class for managing application paths in a portable, cross-platform manner.
    /// Replaces hard-coded paths with configurable, relative path resolution.
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// Directory names - configurable defaults for standard application directories
        /// </summary>
        public static class DirectoryNames
        {
            public const string Data = "data";
            public const string Logs = "logs";
            public const string Config = "config";
            public const string Temp = "temp";
        }

        /// <summary>
        /// Gets the application's base directory (where the executable is located)
        /// </summary>
        public static string GetApplicationBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// Gets a path relative to the application's base directory.
        /// Uses Path.Combine for cross-platform compatibility.
        /// </summary>
        /// <param name="relativePath">Relative path from the application base directory</param>
        /// <returns>Full path combining AppContext.BaseDirectory with the relative path</returns>
        public static string GetApplicationRelativePath(string relativePath)
        {
            return Path.Combine(GetApplicationBaseDirectory(), relativePath);
        }

        /// <summary>
        /// Gets the standard data directory path used by the application.
        /// </summary>
        /// <remarks>
        /// - Configurable via "write-dir" configuration key
        /// - Falls back to "write_dir" environment variable
        /// - Defaults to {BaseDirectory}/data
        /// </remarks>
        /// <param name="configuredPath">Configuration override (optional)</param>
        /// <returns>Full path to the data directory</returns>
        public static string GetDataDirectory(string configuredPath = null)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            return GetApplicationRelativePath(DirectoryNames.Data);
        }

        /// <summary>
        /// Gets the logs directory path
        /// </summary>
        /// <returns>Full path to the logs directory</returns>
        public static string GetLogsDirectory()
        {
            return GetApplicationRelativePath(DirectoryNames.Logs);
        }

        /// <summary>
        /// Gets the config directory path
        /// </summary>
        /// <returns>Full path to the config directory</returns>
        public static string GetConfigDirectory()
        {
            return GetApplicationRelativePath(DirectoryNames.Config);
        }

        /// <summary>
        /// Gets the temp directory path
        /// </summary>
        /// <returns>Full path to the temp directory</returns>
        public static string GetTempDirectory()
        {
            return GetApplicationRelativePath(DirectoryNames.Temp);
        }

        /// <summary>
        /// Ensures that a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="directory">Directory path to ensure exists</param>
        /// <returns>The directory path</returns>
        public static string EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        /// <summary>
        /// Gets the path to an XML documentation file for the specified assembly.
        /// Used by Swagger for including XML comments in API documentation.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly (without .dll extension)</param>
        /// <returns>Full path to the XML documentation file, or null if not found</returns>
        public static string GetXmlDocumentationPath(string assemblyName)
        {
            var xmlFileName = $"{assemblyName}.xml";
            var xmlPath = GetApplicationRelativePath(xmlFileName);

            return File.Exists(xmlPath) ? xmlPath : null;
        }
    }
}
