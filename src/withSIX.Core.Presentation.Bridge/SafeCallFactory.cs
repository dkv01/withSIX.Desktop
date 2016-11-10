// <copyright company="SIX Networks GmbH" file="SafeCallFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.ExceptionServices;
using withSIX.Core.Services;

namespace withSIX.Core.Presentation.Bridge
{
    public class SafeCallFactory : ISafeCallFactory, IPresentationService
    {
        public ISafeCall Create() => new SafeCall();
    }

    public class SafeCall : ISafeCall
    {
        [HandleProcessCorruptedStateExceptions]
        public void Do(Action act) {
            try {
                act();
            } catch (AccessViolationException ex) {
                // Not setting the exception as inner exception because it probably still terminates the process.
                throw new Exception($"Native exception ocurred while SafeCall.Do(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanaged ex");
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public TResult Do<TResult>(Func<TResult> act) {
            try {
                return act();
            } catch (AccessViolationException ex) {
                // Not setting the exception as inner exception because it probably still terminates the process.
                throw new Exception($"Native exception ocurred while SafeCall.Do<TResult>(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanged ex");
            }
        }
    }
}