using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SmartRenamer.Interfaces;
using SmartRenamer.Models;
using System;
using System.IO;
using System.Linq;

namespace SmartRenamer.Capabilities
{
    public class ReadExifCapability : IPipelineStep
    {
        public string Name => "Read EXIF Metadata";

        public void Execute(FileContext context)
        {
            if (context == null)
                return;

            if (string.IsNullOrWhiteSpace(context.CurrentFullPath))
                return;

            if (!File.Exists(context.CurrentFullPath))
                return;

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(
                    context.CurrentFullPath);

                var subIfd = directories
                    .OfType<ExifSubIfdDirectory>()
                    .FirstOrDefault();

                if (subIfd == null)
                    return;

                if (subIfd.TryGetDateTime(
                    ExifDirectoryBase.TagDateTimeOriginal,
                    out DateTime dateTaken))
                {
                    context.Variables["CaptureDate"] = dateTaken;

                    context.Status =
                        $"Photo taken {dateTaken:d}";
                }
            }
            catch (Exception ex)
            {
                context.Warnings.Add(
                    $"EXIF: {ex.Message}");
            }
        }
    }
}