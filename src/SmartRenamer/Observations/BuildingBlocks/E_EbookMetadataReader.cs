using System;
using System.IO.Compression;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadataReader
    /// =========================================================================
    ///
    /// Current Responsibility
    /// -------------------------------------------------------------------------
    /// Verify that an EPUB can be opened and that it contains the
    /// standard META-INF/container.xml file.
    ///
    /// Future builds will:
    /// • Read container.xml
    /// • Locate the package (.opf)
    /// • Read metadata
    /// • Read the cover
    /// =========================================================================
    /// </summary>
    public static class E_EbookMetadataReader
    {
        public static bool Read(FileContext file)
        {
            if (!file.Extension.Equals(
                ".epub",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(file.CurrentFullPath);

                ZipArchiveEntry? container =
                    archive.GetEntry("META-INF/container.xml");

                return container != null;
            }
            catch
            {
                return false;
            }
        }
    }
}