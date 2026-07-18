using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    public class OrganizationPlanner
    {
        public List<RenamePreview> Build(
            ProjectContext context,
            ScoutPlan plan)
        {
            RenamePreviewBuilder builder = new();

            return builder.Build(context, plan);
        }
    }
}