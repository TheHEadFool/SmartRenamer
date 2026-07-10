using System.Windows.Forms;

namespace SmartRenamer.Services
{
    public class FolderPicker
    {
        public string? PickFolder()
        {
            using FolderBrowserDialog dialog = new();

            dialog.Description =
                "Show me the folder you'd like us to work with.";

            dialog.UseDescriptionForTitle = true;

            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
                return dialog.SelectedPath;

            return null;
        }
    }
}