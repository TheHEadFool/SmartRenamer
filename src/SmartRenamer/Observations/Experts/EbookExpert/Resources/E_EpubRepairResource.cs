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
    }
}