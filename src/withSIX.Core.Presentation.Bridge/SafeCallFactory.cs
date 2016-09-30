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
                throw new Exception($"Native exception ocurred while SteamAPI.RunCallbacks(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanged ex");
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public TResult Do<TResult>(Func<TResult> act) {
            try {
                return act();
            } catch (AccessViolationException ex) {
                throw new Exception($"Native exception ocurred while SteamAPI.RunCallbacks(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanged ex");
            }
        }
    }
}