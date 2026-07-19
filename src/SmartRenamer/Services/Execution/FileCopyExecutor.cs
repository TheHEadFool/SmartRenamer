using System.IO;

namespace SmartRenamer.Services.Execution
{
    internal class FileCopyExecutor
    {
        public void Copy(string sourceFile, string destinationFile)
        {
            string? folder = Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.Copy(sourceFile, destinationFile);
        }
    }
}