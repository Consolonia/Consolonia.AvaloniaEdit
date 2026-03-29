using System.Reflection;

namespace Consolonia.Editor.Resources
{
    internal class ResourceLoader
    {
        private const string SampleFilesPrefix = "Consolonia.Editor.Resources.SampleFiles.";

        internal static string LoadSampleFile(string fileName)
        {
            Stream? stream = typeof(ResourceLoader).GetTypeInfo().Assembly
                .GetManifestResourceStream(SampleFilesPrefix + fileName);

            if (stream == null)
                return string.Empty;

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}