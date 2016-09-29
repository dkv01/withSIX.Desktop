﻿// <copyright company="SIX Networks GmbH" file="BrowserView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SN.withSIX.Core;

namespace SN.withSIX.Play.Presentation.Wpf.Views
{
    public class DummyWc {}

    /// <summary>
    ///     Interaction logic for BrowserView.xaml
    /// </summary>
    public partial class BrowserView : Window
    {
        static BrowserView() {
            OpenInSystemBrowser = new RoutedUICommand("Open in System Browser", "OpenInSystemBrowser",
                typeof (UserControl));
            CopyToClipboard = new RoutedUICommand("Open in System Browser", "OpenInSystemBrowser", typeof (UserControl));
        }

        public BrowserView() {
            InitializeComponent();
            //wc.WindowClose += WcOnWindowClose;
            wc.RegisterJsObject("six_client", new DummyWc());

            CommandBindings.Add(new CommandBinding(CopyToClipboard, OnCopyToClipboard, CanCopyToClipboard));
            CommandBindings.Add(new CommandBinding(OpenInSystemBrowser, OnOpenInSystemBrowser, CanOpenInSystemBrowser));
        }

        public static RoutedUICommand OpenInSystemBrowser { get; }
        public static RoutedUICommand CopyToClipboard { get; }

        void CanOpenInSystemBrowser(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        void OnOpenInSystemBrowser(object sender, ExecutedRoutedEventArgs e) {
            Tools.Generic.TryOpenUrl(wc.Address);
        }

        void OnCopyToClipboard(object sender, ExecutedRoutedEventArgs e) {
            Clipboard.SetText(wc.Address);
        }

        void CanCopyToClipboard(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
    }
}