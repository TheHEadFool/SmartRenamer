using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadataReader
    /// =========================================================================
    /// Reads the basic identity metadata from an EPUB.
    /// =========================================================================
    /// </summary>
    public static class E_EbookMetadataReader
    {
        public static E_EbookMetadata? Read(FileContext file)
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

                XDocument container;

                using (Stream stream = containerEntry.Open())
                {
                    container = XDocument.Load(stream);
                }

                XNamespace containerNs =
                    "urn:oasis:names:tc:opendocument:xmlns:container";

                string? packagePath =
                    container.Root?
                        .Element(containerNs + "rootfiles")?
                        .Element(containerNs + "rootfile")?
                        .Attribute("full-path")?
                        .Value;

                if (string.IsNullOrWhiteSpace(packagePath))
                    return null;

                ZipArchiveEntry? packageEntry =
                    archive.GetEntry(packagePath);

                if (packageEntry == null)
                    return null;

                XDocument package;

                using (Stream stream = packageEntry.Open())
                {
                    package = XDocument.Load(stream);
                }

                XNamespace dc =
                    "http://purl.org/dc/elements/1.1/";

                E_EbookMetadata metadata = new();

                metadata.Title =
                    package.Descendants(dc + "title")
                        .FirstOrDefault()?.Value.Trim() ?? "";

                metadata.Author =
                    package.Descendants(dc + "creator")
                        .FirstOrDefault()?.Value.Trim() ?? "";

                metadata.Publisher =
                    package.Descendants(dc + "publisher")
                        .FirstOrDefault()?.Value.Trim() ?? "";

                metadata.Language =
                    package.Descendants(dc + "language")
                        .FirstOrDefault()?.Value.Trim() ?? "";

                metadata.Description =
                    package.Descendants(dc + "description")
                        .FirstOrDefault()?.Value.Trim() ?? "";

                metadata.Isbn =
                    package.Descendants(dc + "identifier")
                        .Select(i => i.Value.Trim())
                        .FirstOrDefault(id =>
                            id.StartsWith("978") ||
                            id.StartsWith("979")) ?? "";

                return metadata;
            }
            catch
            {
                return null;
            }
        }
    }
}