using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// Read-only resource for extracting opening content from an EPUB.
    ///
    /// This resource follows the EPUB package spine so that content is
    /// examined in reading order. It does not modify the EPUB and does not
    /// interpret the extracted text.
    ///
    /// The extracted content can later be supplied as additional evidence
    /// when an Ebook repair opportunity cannot be resolved from metadata alone.
    /// </summary>
    internal sealed class E_EpubContentResource
    {
        /// <summary>
        /// Represents one EPUB content document encountered in reading order.
        /// </summary>
        public sealed class ContentDocument
        {
            /// <summary>
            /// The EPUB path of the content document.
            /// </summary>
            public string Path { get; init; } = "";

            /// <summary>
            /// The extracted readable text from the content document.
            /// </summary>
            public string Text { get; init; } = "";
        }

        /// <summary>
        /// Extracts the first content documents from the EPUB in reading order.
        /// </summary>
        /// <param name="epubPath">Path to the EPUB file.</param>
        /// <param name="maxDocuments">
        /// Maximum number of content documents to inspect.
        /// </param>
        /// <returns>
        /// The extracted content documents in EPUB reading order.
        /// </returns>
        public IReadOnlyList<ContentDocument> ExtractOpeningDocuments(
            string epubPath,
            int maxDocuments = 10)
        {
            if (string.IsNullOrWhiteSpace(epubPath))
            {
                throw new ArgumentException(
                    "An EPUB path is required.",
                    nameof(epubPath));
            }

            if (!File.Exists(epubPath))
            {
                throw new FileNotFoundException(
                    "The EPUB file could not be found.",
                    epubPath);
            }

            if (maxDocuments < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDocuments),
                    "The maximum number of documents must be at least 1.");
            }

            using var archive = ZipFile.OpenRead(epubPath);

            string packagePath = FindPackagePath(archive);
            XDocument packageDocument = LoadXmlDocument(
                archive,
                packagePath);

            XNamespace packageNamespace =
                packageDocument.Root?.Name.Namespace
                ?? XNamespace.None;

            var manifest = packageDocument
                .Descendants(packageNamespace + "manifest")
                .Elements(packageNamespace + "item")
                .Where(item =>
                    !string.IsNullOrWhiteSpace(
                        (string?)item.Attribute("id")))
                .ToDictionary(
                    item => (string?)item.Attribute("id") ?? "",
                    item => (string?)item.Attribute("href") ?? "",
                    StringComparer.Ordinal);

            var spineIds = packageDocument
                .Descendants(packageNamespace + "spine")
                .Elements(packageNamespace + "itemref")
                .Select(item => (string?)item.Attribute("idref"))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Take(maxDocuments)
                .ToList();

            var documents = new List<ContentDocument>();

            foreach (string? idref in spineIds)
            {
                if (string.IsNullOrWhiteSpace(idref))
                    continue;

                if (!manifest.TryGetValue(idref, out string? href))
                    continue;

                if (string.IsNullOrWhiteSpace(href))
                    continue;

                string contentPath = ResolvePath(
                    packagePath,
                    href);

                var contentEntry = archive.GetEntry(contentPath);

                if (contentEntry == null)
                    continue;

                string documentText = ExtractDocumentText(contentEntry);

                documents.Add(new ContentDocument
                {
                    Path = contentPath,
                    Text = documentText
                });
            }

            return documents;
        }

        /// <summary>
        /// Extracts the opening text as one combined string.
        ///
        /// This method is retained as a convenience for callers that do not
        /// need individual document boundaries.
        /// </summary>
        public string ExtractOpeningText(
            string epubPath,
            int maxDocuments = 10)
        {
            var documents = ExtractOpeningDocuments(
                epubPath,
                maxDocuments);

            var text = new StringBuilder();

            foreach (ContentDocument document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.Text))
                    continue;

                if (text.Length > 0)
                    text.AppendLine();

                text.AppendLine(document.Text);
            }

            return text.ToString().Trim();
        }

        private static string FindPackagePath(
            ZipArchive archive)
        {
            var containerEntry =
                archive.GetEntry("META-INF/container.xml");

            if (containerEntry == null)
            {
                throw new InvalidDataException(
                    "The EPUB does not contain META-INF/container.xml.");
            }

            XDocument containerDocument;

            using (var stream = containerEntry.Open())
            {
                containerDocument = XDocument.Load(stream);
            }

            XNamespace containerNamespace =
                "urn:oasis:names:tc:opendocument:xmlns:container";

            string? packagePath = containerDocument
                .Descendants(containerNamespace + "rootfile")
                .Select(element =>
                    (string?)element.Attribute("full-path"))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new InvalidDataException(
                    "The EPUB package document could not be located.");
            }

            if (archive.GetEntry(packagePath) == null)
            {
                throw new InvalidDataException(
                    $"The EPUB package document '{packagePath}' could not be opened.");
            }

            return packagePath;
        }

        private static XDocument LoadXmlDocument(
            ZipArchive archive,
            string path)
        {
            var entry = archive.GetEntry(path);

            if (entry == null)
            {
                throw new InvalidDataException(
                    $"The EPUB document '{path}' could not be opened.");
            }

            using var stream = entry.Open();

            return XDocument.Load(stream);
        }

        private static string ResolvePath(
            string packagePath,
            string href)
        {
            string cleanHref = href
                .Split('#')[0]
                .Replace('\\', '/');

            string packageDirectory =
                Path.GetDirectoryName(packagePath)?
                    .Replace('\\', '/')
                ?? "";

            string combinedPath =
                string.IsNullOrWhiteSpace(packageDirectory)
                    ? cleanHref
                    : $"{packageDirectory}/{cleanHref}";

            var segments = new List<string>();

            foreach (string segment in combinedPath.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment) ||
                    segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count > 0)
                        segments.RemoveAt(segments.Count - 1);

                    continue;
                }

                segments.Add(segment);
            }

            return string.Join("/", segments);
        }

        private static string ExtractDocumentText(
            ZipArchiveEntry entry)
        {
            XDocument document;

            using (var stream = entry.Open())
            {
                document = XDocument.Load(stream);
            }

            var textNodes = document
                .DescendantNodes()
                .OfType<XText>()
                .Select(node => node.Value.Trim())
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value));

            return string.Join(
                " ",
                textNodes);
        }
    }
}