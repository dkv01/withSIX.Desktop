// <copyright company="SIX Networks GmbH" file="ReactiveViewAware.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

#if WinRT
using Windows.UI.Xaml;
#else

#endif
using System;
using System.Collections.Generic;
using Caliburn.Micro;
using SmartAssembly.Attributes;

namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    [DoNotObfuscate]
    public class ReactiveViewAware : ReactiveValidatableObjectBase, IViewAware
    {
        /// <summary>
        ///     The default view context.
        /// </summary>
        public static readonly object DefaultContext = new object();
        /// <summary>
        ///     Indicates whether or not implementors of <see cref="IViewAware" /> should cache their views by default.
        /// </summary>
        public static bool CacheViewsByDefault = true;
        /// <summary>
        ///     The view chache for this instance.
        /// </summary>
        protected readonly IDictionary<object, object> Views = new Dictionary<object, object>();
        bool cacheViews;

        /// <summary>
        ///     Creates an instance of <see cref="ReactiveViewAware" />.
        /// </summary>
        public ReactiveViewAware()
            : this(CacheViewsByDefault) {}

        /// <summary>
        ///     Creates an instance of <see cref="ReactiveViewAware" />.
        /// </summary>
        /// <param name="cacheViews">Indicates whether or not this instance maintains a view cache.</param>
        public ReactiveViewAware(bool cacheViews) {
            CacheViews = cacheViews;
        }

        /// <summary>
        ///     Indicates whether or not this instance maintains a view cache.
        /// </summary>
        protected bool CacheViews
        {
            get { return cacheViews; }
            set
            {
                cacheViews = value;
                if (!cacheViews)
                    Views.Clear();
            }
        }
        /// <summary>
        ///     Raised when a view is attached.
        /// </summary>
        public event EventHandler<ViewAttachedEventArgs> ViewAttached = delegate { };

        void IViewAware.AttachView(object view, object context) {
            if (CacheViews)
                Views[context ?? DefaultContext] = view;

            var nonGeneratedView = PlatformProvider.Current.GetFirstNonGeneratedView(view);
            PlatformProvider.Current.ExecuteOnFirstLoad(nonGeneratedView, OnViewLoaded);
            OnViewAttached(nonGeneratedView, context);
            ViewAttached(this, new ViewAttachedEventArgs {View = nonGeneratedView, Context = context});

            var activatable = this as IActivate;
            if (activatable == null || activatable.IsActive)
                PlatformProvider.Current.ExecuteOnLayoutUpdated(nonGeneratedView, OnViewReady);
            else
                AttachViewReadyOnActivated(activatable, nonGeneratedView);
        }

        /// <summary>
        ///     Gets a view previously attached to this instance.
        /// </summary>
        /// <param name="context">The context denoting which view to retrieve.</param>
        /// <returns>The view.</returns>
        public virtual object GetView(object context = null) {
            object view;
            Views.TryGetValue(context ?? DefaultContext, out view);
            return view;
        }

        static void AttachViewReadyOnActivated(IActivate activatable, object nonGeneratedView) {
            var viewReference = new WeakReference(nonGeneratedView);
            EventHandler<ActivationEventArgs> handler = null;
            handler = (s, e) => {
                ((IActivate) s).Activated -= handler;
                var view = viewReference.Target;
                if (view != null)
                    PlatformProvider.Current.ExecuteOnLayoutUpdated(view, ((ReactiveViewAware) s).OnViewReady);
            };
            activatable.Activated += handler;
        }

        /// <summary>
        ///     Called when a view is attached.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="context">The context in which the view appears.</param>
        protected virtual void OnViewAttached(object view, object context) {}

        /// <summary>
        ///     Called when an attached view's Loaded event fires.
        /// </summary>
        /// <param name="view"></param>
        protected virtual void OnViewLoaded(object view) {}

        /// <summary>
        ///     Called the first time the page's LayoutUpdated event fires after it is navigated to.
        /// </summary>
        /// <param name="view"></param>
        protected virtual void OnViewReady(object view) {}
    }
}