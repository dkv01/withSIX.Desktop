// <copyright company="SIX Networks GmbH" file="ReactiveScreen.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Caliburn.Micro;
using SmartAssembly.Attributes;

namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    [DoNotObfuscate]
    public class ReactiveScreen : ReactiveViewAware, IScreen, IChild
    {
        static readonly ILog Log = LogManager.GetLog(typeof (ReactiveScreen));
        string displayName;
        bool isActive;
        bool isInitialized;
        object parent;

        /// <summary>
        ///     Creates an instance of <see cref="ReactiveScreen" />.
        /// </summary>
        public ReactiveScreen() {
            displayName = GetType().FullName;
        }

        /// <summary>
        ///     Indicates whether or not this instance is currently initialized.
        /// </summary>
        public bool IsInitialized
        {
            get { return isInitialized; }
            private set { SetProperty(ref isInitialized, value); }
        }
        /// <summary>
        ///     Gets or Sets the Parent <see cref="IConductor" />
        /// </summary>
        public virtual object Parent
        {
            get { return parent; }
            set { SetProperty(ref parent, value); }
        }
        /// <summary>
        ///     Gets or Sets the Display Name
        /// </summary>
        public virtual string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }
        /// <summary>
        ///     Indicates whether or not this instance is currently active.
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            private set { SetProperty(ref isActive, value); }
        }
        /// <summary>
        ///     Raised after activation occurs.
        /// </summary>
        public event EventHandler<ActivationEventArgs> Activated = delegate { };
        /// <summary>
        ///     Raised before deactivation.
        /// </summary>
        public event EventHandler<DeactivationEventArgs> AttemptingDeactivation = delegate { };
        /// <summary>
        ///     Raised after deactivation.
        /// </summary>
        public event EventHandler<DeactivationEventArgs> Deactivated = delegate { };

        void IActivate.Activate() {
            if (IsActive)
                return;

            var initialized = false;

            if (!IsInitialized) {
                IsInitialized = initialized = true;
                OnInitialize();
            }

            IsActive = true;
            Log.Info("Activating {0}.", this);
            OnActivate();

            Activated(this, new ActivationEventArgs {
                WasInitialized = initialized
            });
        }

        void IDeactivate.Deactivate(bool close) {
            if (IsActive || (IsInitialized && close)) {
                AttemptingDeactivation(this, new DeactivationEventArgs {
                    WasClosed = close
                });

                IsActive = false;
                Log.Info("Deactivating {0}.", this);
                OnDeactivate(close);

                Deactivated(this, new DeactivationEventArgs {
                    WasClosed = close
                });

                if (close) {
                    Views.Clear();
                    Log.Info("Closed {0}.", this);
                }
            }
        }

        /// <summary>
        ///     Called to check whether or not this instance can close.
        /// </summary>
        /// <param name="callback">The implementor calls this action with the result of the close check.</param>
        public virtual void CanClose(Action<bool> callback) {
            callback(true);
        }

        /// <summary>
        ///     Tries to close this instance by asking its Parent to initiate shutdown or by asking its corresponding view to
        ///     close.
        ///     Also provides an opportunity to pass a dialog result to it's corresponding view.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        public virtual void TryClose(bool? dialogResult = null) {
            PlatformProvider.Current.GetViewCloseAction(this, Views.Values, dialogResult).OnUIThread();
        }

        /// <summary>
        ///     Called when initializing.
        /// </summary>
        protected virtual void OnInitialize() {}

        /// <summary>
        ///     Called when activating.
        /// </summary>
        protected virtual void OnActivate() {}

        /// <summary>
        ///     Called when deactivating.
        /// </summary>
        /// <param name="close">Inidicates whether this instance will be closed.</param>
        protected virtual void OnDeactivate(bool close) {}
    }
}