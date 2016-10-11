using System.Windows;
using Caliburn.Micro;
using withSIX.Core.Presentation.Services;

namespace withSIX.Core.Presentation.Wpf.Services
{
    public class CMBootstrapper<T> : BootstrapperBase
    {
        private readonly ICMBootStrapper<T> _bs;

        public CMBootstrapper(ICMBootStrapper<T> bs) : base(true) {
            _bs = bs;
            bs.InitializeCM();
            // Legacy
            Initialize(); // initialize CM framework
        }

        protected override void OnStartup(object sender, StartupEventArgs e) {
            base.OnStartup(sender, e);
            if (_bs.DisplayRootView)
                DisplayRootViewFor<T>();
        }
    }
}