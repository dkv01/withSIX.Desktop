// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using System.Threading.Tasks;
using ShortBus;

namespace SN.withSIX.Core.Extensions
{
    public static class MediatorExtensions
    {
        public static async Task NotifyEnMass<T>(this IMediator mediator, T message) {
            // TODO: Remove need for Aggregate or TargetInvocationEx...
            try {
                mediator.Notify(message);
                await mediator.NotifyAsync(message).ConfigureAwait(false);
                Common.App.PublishEvent(message);
                    // Let's publish the UI stuff last... Logic: UI should update after other elements are done. Also fixes some timing issues with e.g SubGamesChanged
            } catch (AggregateException ex) {
                // TSK!!
                var first = ex.GetFirstException() as TargetInvocationException;
                if (first != null)
                    ProcessTargetInvocation<T>(first);
                else
                    throw ex.ThrowFirstInner();
            } catch (TargetInvocationException ex) {
                ProcessTargetInvocation<T>(ex);
            }
        }

        static void ProcessTargetInvocation<T>(TargetInvocationException ex) {
            var agex = ex.InnerException as AggregateException;
            if (agex != null)
                throw agex.ThrowFirstInner();
            throw ex.ReThrowInner();
        }
    }
}