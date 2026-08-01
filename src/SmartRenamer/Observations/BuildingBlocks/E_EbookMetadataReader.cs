using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
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
    /// Open an EPUB and determine the location of its package document (.opf).
    ///
    /// Future builds will:
    /// • Read metadata
    /// • Read the cover
    /// • Read the synopsis
    /// =========================================================================
    /// </summary>
    public static class E_EbookMetadataReader
    {
        public static string? Read(FileContext file)
        {
            if (!file.Extension.Equals(
                ".epub",
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(file.CurrentFullPath);

                ZipArchiveEntry? containerEntry =
                    archive.GetEntry("META-INF/container.xml");

                if (containerEntry == null)
                    return null;

                using Stream stream =
                    containerEntry.Open();

                XDocument document =
                    XDocument.Load(stream);

                XNamespace ns =
                    "urn:oasis:names:tc:opendocument:xmlns:container";

                XElement? rootFile =
                    document.Root?
                        .Element(ns + "rootfiles")?
                        .Element(ns + "rootfile");

                return rootFile?
                    .Attribute("full-path")?
                    .Value;
            }
            catch
            {
                return null;
            }
        }
    }
}