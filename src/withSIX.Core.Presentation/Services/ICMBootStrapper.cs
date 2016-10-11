namespace withSIX.Core.Presentation.Services
{
    public interface ICMBootStrapper<T>
    {
        void InitializeCM();
        bool DisplayRootView { get; }
    }
}