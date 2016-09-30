using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Games.Library;

namespace withSIX.Play.Applications.Services
{
    public static class BusyDialogExtensions
    {
        public static void BusyDialog(this IDialogManager dialogManager) {
            dialogManager.MessageBox(new MessageBoxDialogParams(
                "Cannot perform action; already busy.\nPlease wait until current action has completed",
                "Warning, cannot perform action while already busy")).WaitSpecial();
        }
    }
}