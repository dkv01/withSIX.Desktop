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