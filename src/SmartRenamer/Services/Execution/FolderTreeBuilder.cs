using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartRenamer.Services.Execution
{
    internal class FolderTreeBuilder
    {
        public void Build(IEnumerable<string> folders, string destinationRoot)
        {
            foreach (string folder in folders.Distinct())
            {
                string fullPath = Path.Combine(destinationRoot, folder);

                Directory.CreateDirectory(fullPath);
            }
        }
    }
}