using System.IO;
using System.Reflection;

namespace HsMod
{
    public class FileManager
    {
        public static string ReadEmbeddedFile(string hsModEmbeddedFilePath)
        {
            if (hsModEmbeddedFilePath.Substring(0, 2).ToLower() == "./")
            {
                hsModEmbeddedFilePath = hsModEmbeddedFilePath.Substring(2);
            }

            hsModEmbeddedFilePath = hsModEmbeddedFilePath.Replace("\\", "/");
            hsModEmbeddedFilePath = hsModEmbeddedFilePath.Replace("/", ".");
            hsModEmbeddedFilePath = "HsMod." + hsModEmbeddedFilePath;
            Assembly assembly = Assembly.GetCallingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(hsModEmbeddedFilePath))
            {
                if (stream == null)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"Embedded file not found: {hsModEmbeddedFilePath}");
                    return "";
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string[] GetEmbeddedFileList()
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, $"Found embedded file: {resourceName}");
            }

            return resourceNames;

        }
    }


}