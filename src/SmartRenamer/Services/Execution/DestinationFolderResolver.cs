using System.IO;

namespace SmartRenamer.Services.Execution
{
    internal class DestinationFolderResolver
    {
        public string GetDestinationRoot(string sourceFolder)
        {
            string parentFolder = Path.GetDirectoryName(sourceFolder)!;
            string folderName = Path.GetFileName(sourceFolder);

            string destination =
                Path.Combine(parentFolder, $"{folderName} (Scout Organized)");

            int copyNumber = 2;

            while (Directory.Exists(destination))
            {
                destination = Path.Combine(
                    parentFolder,
                    $"{folderName} (Scout Organized {copyNumber})");

                copyNumber++;
            }

            return destination;
        }
    }
}