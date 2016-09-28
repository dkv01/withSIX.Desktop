// <copyright company="SIX Networks GmbH" file="WCFClient.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Presentation.Bridge
{
    /*
    public class WCFClient : IDomainService, IWCFClient
    {
        readonly ChannelFactory<IUpdaterWCF> _pipeFactory;
        readonly IUpdaterWCF _pipeProxy;

        public WCFClient() {
            _pipeFactory = new ChannelFactory<IUpdaterWCF>(new NetNamedPipeBinding(), new EndpointAddress(
                "net.pipe://localhost/UpdaterWCF_Pipe"));

            _pipeProxy = _pipeFactory.CreateChannel();
        }

        public int PerformOperation(params string[] args) {
            try {
                return _pipeProxy.PerformOperation(args);
            } catch (Exception e) {
                if (!(e is EndpointNotFoundException) && !(e is PipeException))
                    throw;

                Tools.InformUserError("This exception is usually due to \"Play withSIX Updater Service\" not running or malfunctioning.\n" +
                    "If you keep having issues please disable the 'Use elevated service' option",
                    "WCF pipe not responding properly.", e);
                return 1;
            }
        }

        public Process LaunchGame(params string[] args) {
            try {
                return Process.GetProcessById(_pipeProxy.LaunchGame(args));
            } catch (Exception e) {
                if (!(e is EndpointNotFoundException) && !(e is PipeException))
                    throw;

                Tools.InformUserError(
                    "This exception is usually due to \"Play withSIX Updater Service\" not running or malfunctioning.\n" +
                    "If you keep having issues please disable the 'Use elevated service' option",
                    "WCF pipe not responding properly.", e);

                return null;
            }
        }
    }
    */
}