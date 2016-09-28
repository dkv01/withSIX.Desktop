// <copyright company="SIX Networks GmbH" file="ExDialog.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows;
using System.Windows.Threading;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using Splat;

namespace SN.withSIX.Core.Presentation.Wpf.Services
{
    public class ExDialog
    {
        public static void SetupExceptionHandler(Application app) {
            app.DispatcherUnhandledException += OnDispatcherUnhandledException;
            //AppDomain.CurrentDomain.UnhandledException += ExDialog.CurrentDomainOnUnhandledException;
        }

        public static void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
            HandleEx((Exception) unhandledExceptionEventArgs.ExceptionObject);
        }

        public static void OnDispatcherUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs) {
            HandleEx(dispatcherUnhandledExceptionEventArgs.Exception);
        }

        static void HandleEx(Exception ex) {
            MainLog.Logger.Fatal(ex.Format(), LogLevel.Fatal);
            MessageBox.Show(
                "Info: " + ex.Message + "\n\n"
                +
                "The application will exit now. We've been notified about the problem. Sorry for the inconvenience\n\nIf the problem persists, please contact Support: http://community.withsix.com",
                "Unrecoverable error occurred");

            Environment.Exit(1);
        }
    }
}