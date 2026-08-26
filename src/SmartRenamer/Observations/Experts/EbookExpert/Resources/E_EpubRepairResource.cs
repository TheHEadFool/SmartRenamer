using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// =========================================================================
    /// E_EpubRepairResource
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Performs safe, domain-specific modifications to EPUB files.
    ///
    /// Current Capability
    /// -------------------------------------------------------------------------
    /// Adds a missing ISBN to the EPUB package metadata.
    ///
    /// Safety Model
    /// -------------------------------------------------------------------------
    /// • The original EPUB is never modified in place.
    /// • A temporary repaired EPUB is created first.
    /// • The original package is copied into the temporary package.
    /// • Only the OPF metadata is changed.
    /// • The temporary package replaces the original only after the complete
    ///   operation succeeds.
    /// • Temporary files are cleaned up when the operation fails.
    ///
    /// This Resource does NOT
    /// -------------------------------------------------------------------------
    /// • Decide whether an EPUB needs repair.
    /// • Research ISBN information.
    /// • Decide which ISBN should be used.
    /// • Communicate with Scout.
    /// • Generate recommendations.
    ///
    /// Those responsibilities belong to the Repair Investigation,
    /// research services, Consultant, and Conversation Framework.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_EpubRepairResource
    {
        /// <summary>
        /// Adds an ISBN to an EPUB that currently has no matching ISBN
        /// identifier.
        ///
        /// The original EPUB is preserved unless the complete repair succeeds.
        ///
        /// Returns true when the EPUB was successfully rewritten or already
        /// contained the requested ISBN.
        /// </summary>
        public bool AddIsbn(
            FileContext file,
            string isbn)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            if (!file.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string sourcePath = file.CurrentFullPath;

            if (!File.Exists(sourcePath))
                return false;

            string temporaryPath =
                sourcePath + ".repairing";

            bool replacementCompleted = false;

            try
            {
                //---------------------------------------------------------
                // Remove any abandoned temporary repair package.
                //---------------------------------------------------------

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                //---------------------------------------------------------
                // Work on a copy.
                //---------------------------------------------------------

                File.Copy(
                    sourcePath,
                    temporaryPath,
                    overwrite: false);

                using (ZipArchive archive =
                    ZipFile.Open(
                        temporaryPath,
                        ZipArchiveMode.Update))
                {
                    //---------------------------------------------------------
                    // Locate the EPUB package document.
                    //---------------------------------------------------------

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

                    //---------------------------------------------------------
                    // Load the OPF package document.
                    //---------------------------------------------------------

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

                    //---------------------------------------------------------
                    // Do not create a duplicate identifier.
                    //---------------------------------------------------------

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
                    {
                        replacementCompleted = true;
                        return true;
                    }

                    //---------------------------------------------------------
                    // Add the new identifier to the OPF metadata.
                    //---------------------------------------------------------

                    XElement identifier =
                        new XElement(
                            dc + "identifier",
                            normalizedIsbn);

                    metadata.Add(identifier);

                    //---------------------------------------------------------
                    // Replace the OPF entry inside the temporary EPUB.
                    //---------------------------------------------------------

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
                // The temporary EPUB is complete.
                // Only now replace the original.
                //---------------------------------------------------------

                File.Replace(
                    temporaryPath,
                    sourcePath,
                    null);

                replacementCompleted = true;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                //---------------------------------------------------------
                // Never leave an incomplete temporary EPUB behind.
                //---------------------------------------------------------

                if (!replacementCompleted &&
                    File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch
                    {
                        // Preserve the original operation result.
                    }
                }
            }
        }
    }
}