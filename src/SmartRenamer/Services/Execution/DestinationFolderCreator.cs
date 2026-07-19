using System.IO;

namespace SmartRenamer.Services.Execution
{
    internal class DestinationFolderCreator
    {
        public void Create(string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);
        }
    }
}