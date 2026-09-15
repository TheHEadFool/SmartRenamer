using System;
using System.Collections.Generic;
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
    /// Reads metadata and the embedded cover from an EPUB.
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

                XNamespace opf =
                    package.Root?.Name.Namespace ?? XNamespace.None;

                E_EbookMetadata metadata = new();

                // -----------------------------------------------------------------
                // Title
                // -----------------------------------------------------------------

                metadata.Title =
                    package.Descendants(dc + "title")
                        .Select(element => element.Value.Trim())
                        .FirstOrDefault(value =>
                            !string.IsNullOrWhiteSpace(value)) ?? "";

                // -----------------------------------------------------------------
                // Authors
                // -----------------------------------------------------------------

                List<string> authors =
                    package.Descendants(dc + "creator")
                        .Select(element => element.Value.Trim())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                metadata.Author =
                    string.Join("; ", authors);

                // -----------------------------------------------------------------
                // Publisher
                // -----------------------------------------------------------------

                metadata.Publisher =
                    package.Descendants(dc + "publisher")
                        .Select(element => element.Value.Trim())
                        .FirstOrDefault(value =>
                            !string.IsNullOrWhiteSpace(value)) ?? "";

                // -----------------------------------------------------------------
                // Language
                // -----------------------------------------------------------------

                metadata.Language =
                    package.Descendants(dc + "language")
                        .Select(element => element.Value.Trim())
                        .FirstOrDefault(value =>
                            !string.IsNullOrWhiteSpace(value)) ?? "";

                // -----------------------------------------------------------------
                // Description
                // -----------------------------------------------------------------

                metadata.Description =
                    package.Descendants(dc + "description")
                        .Select(element => element.Value.Trim())
                        .FirstOrDefault(value =>
                            !string.IsNullOrWhiteSpace(value)) ?? "";

                // -----------------------------------------------------------------
                // Series
                //
                // EPUB 3 can use:
                //     <meta property="belongs-to-collection">...</meta>
                //
                // Calibre commonly uses:
                //     <meta name="calibre:series" content="..."/>
                // -----------------------------------------------------------------

                XElement? belongsToCollection =
                    package.Descendants()
                        .FirstOrDefault(element =>
                            element.Name.LocalName.Equals(
                                "meta",
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                (string?)element.Attribute("property"),
                                "belongs-to-collection",
                                StringComparison.OrdinalIgnoreCase));

                string series =
                    belongsToCollection?.Value.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(series))
                {
                    series =
                        package.Descendants()
                            .FirstOrDefault(element =>
                                element.Name.LocalName.Equals(
                                    "meta",
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    (string?)element.Attribute("name"),
                                    "calibre:series",
                                    StringComparison.OrdinalIgnoreCase))
                            ?.Attribute("content")?
                            .Value.Trim() ?? "";
                }

                metadata.Series = series;

                // -----------------------------------------------------------------
                // ISBN
                //
                // Prefer identifiers explicitly marked as ISBN.
                // Also accept valid ISBN-10 / ISBN-13 identifiers when no
                // explicit ISBN scheme is provided.
                // -----------------------------------------------------------------

                var identifiers =
                    package.Descendants(dc + "identifier")
                        .Select(element => new
                        {
                            Value = NormalizeIsbn(element.Value.Trim()),
                            Scheme =
                                (string?)element.Attribute("scheme") ?? ""
                        })
                        .Where(identifier =>
                            IsValidIsbn(identifier.Value))
                        .ToList();

                string? explicitIsbn =
                    identifiers
                        .FirstOrDefault(identifier =>
                            identifier.Scheme.Equals(
                                "ISBN",
                                StringComparison.OrdinalIgnoreCase))
                        ?.Value;

                metadata.Isbn =
                    explicitIsbn ??
                    identifiers.FirstOrDefault()?.Value ??
                    "";

                // -----------------------------------------------------------------
                // Manifest
                // -----------------------------------------------------------------

                XElement? manifest =
                    package.Root?.Element(opf + "manifest");

                if (manifest == null)
                    return metadata;

                // -----------------------------------------------------------------
                // Cover
                //
                // EPUB 3:
                //     properties="cover-image"
                //
                // EPUB 2 / Calibre:
                //     <meta name="cover" content="cover"/>
                //     <item id="cover" href="cover.jpeg" .../>
                // -----------------------------------------------------------------

                XElement? coverItem =
                    manifest.Elements(opf + "item")
                        .FirstOrDefault(item =>
                        {
                            string properties =
                                (string?)item.Attribute("properties") ?? "";

                            return properties
                                .Split(
                                    new[] { ' ', '\t', '\r', '\n' },
                                    StringSplitOptions.RemoveEmptyEntries)
                                .Any(property =>
                                    property.Equals(
                                        "cover-image",
                                        StringComparison.OrdinalIgnoreCase));
                        });

                if (coverItem == null)
                {
                    XElement? coverMeta =
                        package.Descendants()
                            .FirstOrDefault(element =>
                                element.Name.LocalName.Equals(
                                    "meta",
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    (string?)element.Attribute("name"),
                                    "cover",
                                    StringComparison.OrdinalIgnoreCase));

                    string? coverId =
                        coverMeta?.Attribute("content")?.Value.Trim();

                    if (!string.IsNullOrWhiteSpace(coverId))
                    {
                        coverItem =
                            manifest.Elements(opf + "item")
                                .FirstOrDefault(item =>
                                    string.Equals(
                                        (string?)item.Attribute("id"),
                                        coverId,
                                        StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (coverItem == null)
                    return metadata;

                string? coverPath =
                    (string?)coverItem.Attribute("href");

                if (string.IsNullOrWhiteSpace(coverPath))
                    return metadata;

                coverPath =
                    Uri.UnescapeDataString(coverPath)
                        .Replace('\\', '/');

                string packageDirectory =
                    GetZipDirectory(packagePath);

                string fullCoverPath =
                    string.IsNullOrWhiteSpace(packageDirectory)
                        ? coverPath
                        : packageDirectory + "/" + coverPath;

                fullCoverPath =
                    NormalizeZipPath(fullCoverPath);

                ZipArchiveEntry? coverEntry =
                    archive.GetEntry(fullCoverPath);

                if (coverEntry == null)
                    return metadata;

                metadata.HasCover = true;

                using Stream coverStream =
                    coverEntry.Open();

                using MemoryStream memory =
                    new();

                coverStream.CopyTo(memory);

                metadata.CoverImage =
                    memory.ToArray();

                return metadata;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeIsbn(string value)
        {
            return new string(
                value
                    .Where(character =>
                        char.IsDigit(character) ||
                        character == 'X' ||
                        character == 'x')
                    .ToArray())
                .ToUpperInvariant();
        }

        private static bool IsValidIsbn(string value)
        {
            if (value.Length == 10)
                return IsValidIsbn10(value);

            if (value.Length == 13)
                return IsValidIsbn13(value);

            return false;
        }

        private static bool IsValidIsbn10(string value)
        {
            if (!value.Take(9).All(char.IsDigit))
                return false;

            if (!(char.IsDigit(value[9]) ||
                  value[9] == 'X'))
            {
                return false;
            }

            int sum = 0;

            for (int i = 0; i < 9; i++)
            {
                sum += (value[i] - '0') * (10 - i);
            }

            int check =
                value[9] == 'X'
                    ? 10
                    : value[9] - '0';

            sum += check;

            return sum % 11 == 0;
        }

        private static bool IsValidIsbn13(string value)
        {
            if (!value.All(char.IsDigit))
                return false;

            int sum = 0;

            for (int i = 0; i < 12; i++)
            {
                int digit = value[i] - '0';

                sum +=
                    i % 2 == 0
                        ? digit
                        : digit * 3;
            }

            int checkDigit =
                (10 - (sum % 10)) % 10;

            return checkDigit == value[12] - '0';
        }

        private static string GetZipDirectory(string path)
        {
            int separator =
                path.LastIndexOf('/');

            if (separator < 0)
                return "";

            return path[..separator];
        }

        private static string NormalizeZipPath(string path)
        {
            string[] parts =
                path.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

            List<string> normalized =
                new();

            foreach (string part in parts)
            {
                if (part == ".")
                    continue;

                if (part == "..")
                {
                    if (normalized.Count > 0)
                        normalized.RemoveAt(normalized.Count - 1);

                    continue;
                }

                normalized.Add(part);
            }

            return string.Join("/", normalized);
        }
    }
}