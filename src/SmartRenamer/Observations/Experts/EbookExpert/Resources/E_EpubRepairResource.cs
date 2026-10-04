using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// =========================================================================
    /// E_EpubRepairResource
    /// =========================================================================
    ///
    /// Performs the physical EPUB modification requested by the Ebook Expert.
    ///
    /// SAFETY BOUNDARY
    /// -------------------------------------------------------------------------
    /// The Resource modifies only the supplied target path.
    ///
    /// It never replaces, deletes, or otherwise modifies the original source
    /// EPUB.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_EpubRepairResource
    {
        /// <summary>
        /// Adds an ISBN to an EPUB working copy.
        ///
        /// sourceFile identifies the original ebook whose metadata is being
        /// repaired.
        ///
        /// targetPath identifies the physical EPUB copy that will actually
        /// be modified.
        ///
        /// The original EPUB is never modified.
        /// </summary>
        public bool AddIsbn(
            FileContext sourceFile,
            string isbn,
            string targetPath)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(
                    "Target path cannot be empty.",
                    nameof(targetPath));

            if (!sourceFile.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!File.Exists(targetPath))
                return false;

            string temporaryPath =
                targetPath + ".repairing";

            try
            {
                //---------------------------------------------------------
                // Remove an abandoned temporary repair package.
                //---------------------------------------------------------

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                //---------------------------------------------------------
                // Work on a copy of the supplied target.
                //---------------------------------------------------------

                File.Copy(
                    targetPath,
                    temporaryPath,
                    overwrite: false);

                using (ZipArchive archive =
                    ZipFile.Open(
                        temporaryPath,
                        ZipArchiveMode.Update))
                {
                    //-----------------------------------------------------
                    // Locate the EPUB package document.
                    //-----------------------------------------------------

                    ZipArchiveEntry? containerEntry =
                        archive.GetEntry(
                            "META-INF/container.xml");

                    if (containerEntry == null)
                        return false;

                    XDocument container;

                    using (Stream stream =
                        containerEntry.Open())
                    {
                        container =
                            XDocument.Load(stream);
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
                        return false;

                    ZipArchiveEntry? packageEntry =
                        archive.GetEntry(packagePath);

                    if (packageEntry == null)
                        return false;

                    //-----------------------------------------------------
                    // Load the OPF package document.
                    //-----------------------------------------------------

                    XDocument package;

                    using (Stream stream =
                        packageEntry.Open())
                    {
                        package =
                            XDocument.Load(stream);
                    }

                    XNamespace dc =
                        "http://purl.org/dc/elements/1.1/";

                    XElement? metadata =
                        package.Root?
                            .Elements()
                            .FirstOrDefault(
                                element =>
                                    element.Name.LocalName ==
                                    "metadata");

                    if (metadata == null)
                        return false;

                    //-----------------------------------------------------
                    // Do not create a duplicate ISBN.
                    //-----------------------------------------------------

                    string normalizedIsbn =
                        isbn.Trim();

                    bool alreadyHasIsbn =
                        package
                            .Descendants(dc + "identifier")
                            .Any(identifier =>
                                string.Equals(
                                    identifier.Value.Trim(),
                                    normalizedIsbn,
                                    StringComparison.OrdinalIgnoreCase));

                    if (alreadyHasIsbn)
                        return true;

                    //-----------------------------------------------------
                    // Add the approved ISBN.
                    //-----------------------------------------------------

                    XElement identifier =
                        new(
                            dc + "identifier",
                            normalizedIsbn);

                    metadata.Add(identifier);

                    //-----------------------------------------------------
                    // Replace the OPF entry inside the temporary EPUB.
                    //-----------------------------------------------------

                    string packageText =
                        package.ToString(
                            SaveOptions.DisableFormatting);

                    packageEntry.Delete();

                    ZipArchiveEntry replacement =
                        archive.CreateEntry(
                            packagePath,
                            CompressionLevel.Optimal);

                    using (Stream stream =
                        replacement.Open())
                    using (StreamWriter writer =
                        new(stream))
                    {
                        writer.Write(packageText);
                    }
                }

                //---------------------------------------------------------
                // The repaired temporary EPUB is complete.
                //
                // Replace ONLY the target working copy.
                //
                // The original source EPUB is never touched.
                //---------------------------------------------------------

                File.Copy(
                    temporaryPath,
                    targetPath,
                    overwrite: true);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                //---------------------------------------------------------
                // Never leave the internal .repairing file behind.
                //---------------------------------------------------------

                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch
                    {
                        // Preserve the result of the repair operation.
                    }
                }
            }
        }
       /// <summary>
       /// Applies one approved repair change to the supplied EPUB working copy.
       ///
       /// The Resource owns the physical EPUB modification.
       /// The original EPUB is never modified.
       /// </summary>
        public bool ApplyRepairChange(
            FileContext sourceFile,
            E_RepairChange change,
            string targetPath)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (change == null)
                throw new ArgumentNullException(nameof(change));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(
                    "Target path cannot be empty.",
                    nameof(targetPath));

            if (!change.CanExecute)
                return false;

            if (string.Equals(
                    change.RepairType,
                    "ISBN",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedIsbn)
                    return false;

                return AddIsbn(
                    sourceFile,
                    approvedIsbn,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Title",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedTitle)
                    return false;

                return ReplaceOrAddSingleDcElement(
                    sourceFile,
                    "title",
                    approvedTitle,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Author",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedAuthor)
                    return false;

                return ReplaceOrAddSingleDcElement(
                    sourceFile,
                    "creator",
                    approvedAuthor,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Publisher",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedPublisher)
                    return false;

                return ReplaceOrAddSingleDcElement(
                    sourceFile,
                    "publisher",
                    approvedPublisher,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Language",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedLanguage)
                    return false;

                return ReplaceOrAddSingleDcElement(
                    sourceFile,
                    "language",
                    approvedLanguage,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Description",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedDescription)
                    return false;

                return ReplaceOrAddSingleDcElement(
                    sourceFile,
                    "description",
                    approvedDescription,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "Series",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedSeries)
                    return false;

                return UpdateSeriesMetadata(
                    sourceFile,
                    approvedSeries,
                    null,
                    targetPath);
            }

            if (string.Equals(
                    change.RepairType,
                    "SeriesNumber",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (change.ApprovedValue is not string approvedSeriesNumber)
                    return false;

                return UpdateSeriesMetadata(
                    sourceFile,
                    null,
                    approvedSeriesNumber,
                    targetPath);
            }

            return false;
        }

        private static bool ReplaceSingleDcElement(
            FileContext sourceFile,
            string localName,
            string approvedValue,
            string targetPath)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (string.IsNullOrWhiteSpace(localName) ||
                string.IsNullOrWhiteSpace(approvedValue) ||
                string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            if (!sourceFile.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(targetPath))
            {
                return false;
            }

            string temporaryPath = targetPath + ".repairing";

            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                File.Copy(targetPath, temporaryPath, overwrite: false);

                using (ZipArchive archive =
                    ZipFile.Open(temporaryPath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry? containerEntry =
                        archive.GetEntry("META-INF/container.xml");

                    if (containerEntry == null)
                        return false;

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
                        return false;

                    ZipArchiveEntry? packageEntry =
                        archive.GetEntry(packagePath);

                    if (packageEntry == null)
                        return false;

                    XDocument package;
                    using (Stream stream = packageEntry.Open())
                    {
                        package = XDocument.Load(stream);
                    }

                    XNamespace dc =
                        "http://purl.org/dc/elements/1.1/";

                    XElement? metadata =
                        package.Root?
                            .Elements()
                            .FirstOrDefault(
                                element => element.Name.LocalName == "metadata");

                    if (metadata == null)
                        return false;

                    List<XElement> matches =
                        metadata.Elements(dc + localName).ToList();

                    // Do not silently destroy multi-author creator metadata.
                    if (matches.Count != 1)
                        return false;

                    matches[0].Value = approvedValue.Trim();

                    string packageText =
                        package.ToString(SaveOptions.DisableFormatting);

                    packageEntry.Delete();

                    ZipArchiveEntry replacement =
                        archive.CreateEntry(
                            packagePath,
                            CompressionLevel.Optimal);

                    using (Stream stream = replacement.Open())
                    using (StreamWriter writer = new(stream))
                    {
                        writer.Write(packageText);
                    }
                }

                File.Copy(temporaryPath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch { }
                }
            }
        }


        private static bool ReplaceOrAddSingleDcElement(
            FileContext sourceFile,
            string localName,
            string approvedValue,
            string targetPath)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (string.IsNullOrWhiteSpace(localName) ||
                string.IsNullOrWhiteSpace(approvedValue) ||
                string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            if (!sourceFile.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(targetPath))
            {
                return false;
            }

            string temporaryPath = targetPath + ".repairing";

            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                File.Copy(targetPath, temporaryPath, overwrite: false);

                using (ZipArchive archive =
                    ZipFile.Open(temporaryPath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry? containerEntry =
                        archive.GetEntry("META-INF/container.xml");

                    if (containerEntry == null)
                        return false;

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
                        return false;

                    ZipArchiveEntry? packageEntry =
                        archive.GetEntry(packagePath);

                    if (packageEntry == null)
                        return false;

                    XDocument package;
                    using (Stream stream = packageEntry.Open())
                    {
                        package = XDocument.Load(stream);
                    }

                    XNamespace dc =
                        "http://purl.org/dc/elements/1.1/";

                    XElement? metadata =
                        package.Root?
                            .Elements()
                            .FirstOrDefault(
                                element => element.Name.LocalName == "metadata");

                    if (metadata == null)
                        return false;

                    List<XElement> matches =
                        metadata.Elements(dc + localName).ToList();

                    if (matches.Count > 1)
                        return false;

                    if (matches.Count == 1)
                    {
                        matches[0].Value = approvedValue.Trim();
                    }
                    else
                    {
                        metadata.Add(
                            new XElement(
                                dc + localName,
                                approvedValue.Trim()));
                    }

                    string packageText =
                        package.ToString(SaveOptions.DisableFormatting);

                    packageEntry.Delete();

                    ZipArchiveEntry replacement =
                        archive.CreateEntry(
                            packagePath,
                            CompressionLevel.Optimal);

                    using (Stream stream = replacement.Open())
                    using (StreamWriter writer = new(stream))
                    {
                        writer.Write(packageText);
                    }
                }

                File.Copy(temporaryPath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch { }
                }
            }
        }

        private static bool UpdateSeriesMetadata(
            FileContext sourceFile,
            string? approvedSeries,
            string? approvedSeriesNumber,
            string targetPath)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (!sourceFile.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(targetPath) ||
                (string.IsNullOrWhiteSpace(approvedSeries) &&
                 string.IsNullOrWhiteSpace(approvedSeriesNumber)))
            {
                return false;
            }

            string temporaryPath = targetPath + ".repairing";

            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                File.Copy(targetPath, temporaryPath, overwrite: false);

                using (ZipArchive archive =
                    ZipFile.Open(temporaryPath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry? containerEntry =
                        archive.GetEntry("META-INF/container.xml");

                    if (containerEntry == null)
                        return false;

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
                        return false;

                    ZipArchiveEntry? packageEntry =
                        archive.GetEntry(packagePath);

                    if (packageEntry == null)
                        return false;

                    XDocument package;
                    using (Stream stream = packageEntry.Open())
                    {
                        package = XDocument.Load(stream);
                    }

                    XElement? metadata =
                        package.Root?
                            .Elements()
                            .FirstOrDefault(
                                element =>
                                    element.Name.LocalName == "metadata");

                    if (metadata == null)
                        return false;

                    bool changed = false;

                    //-------------------------------------------------
                    // Series: preserve an existing EPUB3 or Calibre
                    // representation when one exists. If neither is
                    // present, create the broadly supported Calibre form.
                    //-------------------------------------------------
                    if (!string.IsNullOrWhiteSpace(approvedSeries))
                    {
                        List<XElement> collectionMetas =
                            metadata
                                .Descendants()
                                .Where(element =>
                                    element.Name.LocalName.Equals(
                                        "meta",
                                        StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(
                                        (string?)element.Attribute("property"),
                                        "belongs-to-collection",
                                        StringComparison.OrdinalIgnoreCase))
                                .ToList();

                        if (collectionMetas.Count > 0)
                        {
                            collectionMetas[0].Value = approvedSeries.Trim();
                            changed = true;
                        }
                        else
                        {
                            List<XElement> calibreSeries =
                                metadata
                                    .Descendants()
                                    .Where(element =>
                                        element.Name.LocalName.Equals(
                                            "meta",
                                            StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(
                                            (string?)element.Attribute("name"),
                                            "calibre:series",
                                            StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                            if (calibreSeries.Count > 0)
                            {
                                calibreSeries[0].SetAttributeValue(
                                    "content",
                                    approvedSeries.Trim());
                                changed = true;
                            }
                            else
                            {
                                metadata.Add(
                                    new XElement(
                                        metadata.Name.Namespace + "meta",
                                        new XAttribute("name", "calibre:series"),
                                        new XAttribute("content", approvedSeries.Trim())));
                                changed = true;
                            }
                        }
                    }

                    //-------------------------------------------------
                    // SeriesNumber: preserve EPUB3 group-position or
                    // Calibre series_index when present. If neither exists,
                    // create the Calibre representation.
                    //-------------------------------------------------
                    if (!string.IsNullOrWhiteSpace(approvedSeriesNumber))
                    {
                        List<XElement> groupPositions =
                            metadata
                                .Descendants()
                                .Where(element =>
                                    element.Name.LocalName.Equals(
                                        "meta",
                                        StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(
                                        (string?)element.Attribute("property"),
                                        "group-position",
                                        StringComparison.OrdinalIgnoreCase))
                                .ToList();

                        if (groupPositions.Count > 0)
                        {
                            groupPositions[0].Value = approvedSeriesNumber.Trim();
                            changed = true;
                        }
                        else
                        {
                            List<XElement> calibreIndexes =
                                metadata
                                    .Descendants()
                                    .Where(element =>
                                        element.Name.LocalName.Equals(
                                            "meta",
                                            StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(
                                            (string?)element.Attribute("name"),
                                            "calibre:series_index",
                                            StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                            if (calibreIndexes.Count > 0)
                            {
                                calibreIndexes[0].SetAttributeValue(
                                    "content",
                                    approvedSeriesNumber.Trim());
                                changed = true;
                            }
                            else
                            {
                                metadata.Add(
                                    new XElement(
                                        metadata.Name.Namespace + "meta",
                                        new XAttribute("name", "calibre:series_index"),
                                        new XAttribute("content", approvedSeriesNumber.Trim())));
                                changed = true;
                            }
                        }
                    }

                    if (!changed)
                        return false;

                    string packageText =
                        package.ToString(SaveOptions.DisableFormatting);

                    packageEntry.Delete();

                    ZipArchiveEntry replacement =
                        archive.CreateEntry(
                            packagePath,
                            CompressionLevel.Optimal);

                    using (Stream stream = replacement.Open())
                    using (StreamWriter writer = new(stream))
                    {
                        writer.Write(packageText);
                    }
                }

                File.Copy(temporaryPath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch { }
                }
            }
        }

    }
}
