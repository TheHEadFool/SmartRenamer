using SmartRenamer.Capabilities;
using SmartRenamer.Models;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Converts recommendations from ProjectAnalyzer
    /// into executable workflow steps.
    /// </summary>
    public class CapabilityFactory
    {
        public WorkflowStep? Create(string capabilityName)
        {
            switch (capabilityName)
            {
                case "Read EXIF Metadata":
                    return new WorkflowStep(
                        new ReadExifCapability());

                case "Read Video Metadata":
                    return new WorkflowStep(
                        new ReadVideoMetadataCapability());

                case "Organize Documents":
                    return new WorkflowStep(
                        new OrganizeDocumentsCapability());

                case "Detect Duplicate Files":
                    return new WorkflowStep(
                        new DetectDuplicateFilesCapability());

                default:
                    return null;
            }
        }
    }
}