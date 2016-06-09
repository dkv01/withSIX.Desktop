using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Presentation.Wpf
{
    public class UiRoot : IPresentationService
    {
        public UiRoot(IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            ErrorHandler = new WpfErrorHandler(dialogManager, specialDialogManager);
        }

        public WpfErrorHandler ErrorHandler { get; }
        public static UiRoot Main { get; set; }
    }
}