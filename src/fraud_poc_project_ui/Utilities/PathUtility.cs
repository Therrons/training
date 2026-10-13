using System;
using System.IO;

namespace fraud_poc_project.Utilities
{
    public enum StandardDirectory
    {
        Data,
        Logs,
        Config,
        Temp
    }

    public static class PathUtility
    {
        public static string GetApplicationBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        public static string GetApplicationRelativePath(string relativePath)
        {
            return Path.Combine(GetApplicationBaseDirectory(), relativePath);
        }

        public static string GetDirectory(StandardDirectory directory, string configuredPath = null)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            var directoryName = directory.ToString().ToLower();
            return GetApplicationRelativePath(directoryName);
        }

        public static string GetDataDirectory(string configuredPath = null)
        {
            return GetDirectory(StandardDirectory.Data, configuredPath);
        }

        public static string GetLogsDirectory()
        {
            return GetDirectory(StandardDirectory.Logs);
        }

        public static string GetConfigDirectory()
        {
            return GetDirectory(StandardDirectory.Config);
        }

        public static string GetTempDirectory()
        {
            return GetDirectory(StandardDirectory.Temp);
        }

        public static string EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string GetXmlDocumentationPath(string assemblyName)
        {
            var xmlFileName = $"{assemblyName}.xml";
            var xmlPath = GetApplicationRelativePath(xmlFileName);

            return File.Exists(xmlPath) ? xmlPath : null;
        }
    }
}
