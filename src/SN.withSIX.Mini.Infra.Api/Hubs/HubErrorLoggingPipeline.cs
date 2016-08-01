// <copyright company="SIX Networks GmbH" file="HubErrorLoggingPipeline.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class HubErrorLoggingPipelineModule : HubPipelineModule
    {
        //static readonly ILog logger = LogManager.GetLogger(typeof(HubErrorLoggingPipelineModule));

        protected override void OnIncomingError(ExceptionContext exceptionContext,
            IHubIncomingInvokerContext invokerContext) {
            // Don't log user exceptions...
            var hubEx = exceptionContext.Error as HubException;
            if (hubEx != null) {
                var data = hubEx.ErrorData as UserException;
                if (data != null) {
                    MainLog.Logger.FormattedWarnException(data, "UserException catched in Hub Pipeline");
                    return;
                }

                var ex = hubEx.ErrorData as Exception;
                if (ex != null) {
                    MainLog.Logger.FormattedErrorException(ex,
                        "An error occurred on signalr hub: " + GetInvocationInfo(invokerContext));
                }
                return;
            }

            if (exceptionContext.Error is UserException || exceptionContext.Error.InnerException is UserException)
                return;
            MainLog.Logger.FormattedErrorException(exceptionContext.Error,
                "An error occurred on signalr hub: " + GetInvocationInfo(invokerContext));
        }

        static string GetInvocationInfo(IHubIncomingInvokerContext invokerContext)
            => invokerContext.Hub.GetType() + "." +
               invokerContext.MethodDescriptor.Name + "(" +
               string.Join(", ", PrettyPrintParameters(invokerContext)) + ")";

        static IEnumerable<string> PrettyPrintParameters(IHubIncomingInvokerContext invokerContext)
            => GetMethodParameterInfo(invokerContext)
                .Select(x => x.Key.Name + ": (" + x.Value.GetType() + ") " + x);

        static IEnumerable<KeyValuePair<ParameterDescriptor, object>> GetMethodParameterInfo(
            IHubIncomingInvokerContext invokerContext)
            => invokerContext.MethodDescriptor.Parameters.Zip(invokerContext.Args,
                (descriptor, o) => new KeyValuePair<ParameterDescriptor, object>(descriptor, o));
    }
}