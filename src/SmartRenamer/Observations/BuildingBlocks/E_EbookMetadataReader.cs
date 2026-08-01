using SmartRenamer.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// Reads metadata from EPUB files.
    /// Current build:
    /// Returns the book title.
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

                XElement? titleElement =
                    package.Descendants(dc + "title")
                           .FirstOrDefault();

                var metadata = new E_EbookMetadata();

                if (titleElement != null)
                {
                    metadata.Title =
                        titleElement.Value.Trim();
                }

                return metadata;
            }
            catch
            {
                return null;
            }
        }
    }
}