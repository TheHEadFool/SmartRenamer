namespace SmartRenamer.Services.Execution
{
    internal class ScoutPlanExecutor
    {
        private readonly DestinationFolderResolver _resolver = new();
        private readonly DestinationFolderCreator _creator = new();
        private readonly FileCopyExecutor _copyExecutor = new();

        public string PrepareDestination(string sourceFolder)
        {
            string destination = _resolver.GetDestinationRoot(sourceFolder);

            _creator.Create(destination);

            return destination;
        }
    }
}