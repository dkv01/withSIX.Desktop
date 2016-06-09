// <copyright company="SIX Networks GmbH" file="DbContextFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Cache;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class DbContextFactory : IDbContextFactory, IInfrastructureService
    {
        readonly Lazy<ILocalCache> _cache;
        readonly Lazy<ISettingsStorage> _settingsContext;

        public DbContextFactory(Lazy<ILocalCache> cache, Lazy<ISettingsStorage> settingsContext) {
            _cache = cache;
            _settingsContext = settingsContext;
        }

        public IDbContextScope Create() => new DbContextScope(_cache.Value, _settingsContext.Value);

        public IDisposable SuppressAmbientContext() => new AmbientContextSuppressor();
    }

    public class AmbientContextSuppressor : IDisposable
    {
        private bool _disposed;
        private DbContextScope _savedScope;

        public AmbientContextSuppressor() {
            _savedScope = DbContextScope.GetAmbientScope();

            // We're hiding the ambient scope but not removing its instance
            // altogether. This is to be tolerant to some programming errors. 
            // 
            // Suppose we removed the ambient scope instance here. If someone
            // was to start a parallel task without suppressing
            // the ambient context and then tried to suppress the ambient
            // context within the parallel task while the original flow
            // of execution was still ongoing (a strange thing to do, I know,
            // but I'm sure this is going to happen), we would end up 
            // removing the ambient context instance of the original flow 
            // of execution from within the parallel flow of execution!
            // 
            // As a result, any code in the original flow of execution
            // that would attempt to access the ambient scope would end up 
            // with a null value since we removed the instance.
            //
            // It would be a fairly nasty bug to track down. So don't let
            // that happen. Hiding the ambient scope (i.e. clearing the CallContext
            // in our execution flow but leaving the ambient scope instance untouched)
            // is safe.
            DbContextScope.HideAmbientScope();
        }

        public void Dispose() {
            if (_disposed)
                return;

            if (_savedScope != null) {
                DbContextScope.SetAmbientScope(_savedScope);
                _savedScope = null;
            }

            _disposed = true;
        }
    }

    public class DbContextLocator : IDbContextLocator, IInfrastructureService
    {
        public IGameContext GetGameContext() => GetConfirmScope().GameContext();

        public IContentFolderLinkContext GetContentLinkContext() => GetConfirmScope().ContentLinkContext();

        // TODO: Get ReadOnly Scope.
        public IGameContextReadOnly GetReadOnlyGameContext() => GetConfirmScope().GameContext();

        public ISettingsStorage GetSettingsContext() => GetConfirmScope().SettingsContext();

        // TODO: Get ReadOnly Scope.
        public ISettingsStorageReadOnly GetReadOnlySettingsContext() => GetConfirmScope().SettingsContext();

        private static DbContextScope GetConfirmScope() {
            var scope = DbContextScope.GetAmbientScope();
            if (scope == null)
                throw new InvalidOperationException("No open DB Scope detected, did you forget to open one?");
            return scope;
        }
    }

    public class DbContextScope : IDbContextScope
    {
        static readonly ConditionalWeakTable<InstanceIdentifier, DbContextScope> dbContextScopeInstances =
            new ConditionalWeakTable<InstanceIdentifier, DbContextScope>();
        static readonly string ambientDbContextScopeKey = "ambientDbContextScope-" + Guid.NewGuid();
        readonly InstanceIdentifier _instanceIdentifier = new InstanceIdentifier();
        readonly bool _nested;
        readonly DbContextScope _parentScope;

        private DbContexts _contexts;
        bool _disposed;

        public DbContextScope(ILocalCache cache, ISettingsStorage settingsStorage) {
            _parentScope = GetAmbientScope();
            if (_parentScope == null)
                _contexts = new DbContexts(cache, settingsStorage);
            else {
                _contexts = _parentScope._contexts;
                _nested = true;
            }
            SetAmbientScope(this);
        }

        public void Dispose() {
            if (_disposed)
                return;

            // Commit / Rollback and dispose all of our DbContext instances
            /*
            if (!_nested) {
                if (!_completed)
                {
                    // Do our best to clean up as much as we can but don't throw here as it's too late anyway.
                    try
                    {
                        if (_readOnly)
                        {
                            // Disposing a read-only scope before having called its SaveChanges() method
                            // is the normal and expected behavior. Read-only scopes get committed automatically.
                            CommitInternal();
                        }
                        else
                        {
                            // Disposing a read/write scope before having called its SaveChanges() method
                            // indicates that something went wrong and that all changes should be rolled-back.
                            RollbackInternal();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }

                    _completed = true;
                }


                DisposeDbContexts();
            }
            */

            // Pop ourself from the ambient scope stack
            var currentAmbientScope = GetAmbientScope();
            if (currentAmbientScope != this) // This is a serious programming error. Worth throwing here.
            {
                throw new InvalidOperationException(
                    "DbContextScope instances must be disposed of in the order in which they were created!");
            }

            RemoveAmbientScope();

            if (_parentScope != null) {
                if (_parentScope._disposed) {
                    /*
                     * If our parent scope has been disposed before us, it can only mean one thing:
                     * someone started a parallel flow of execution and forgot to suppress the
                     * ambient context before doing so. And we've been created in that parallel flow.
                     * 
                     * Since the CallContext flows through all async points, the ambient scope in the 
                     * main flow of execution ended up becoming the ambient scope in this parallel flow
                     * of execution as well. So when we were created, we captured it as our "parent scope". 
                     * 
                     * The main flow of execution then completed while our flow was still ongoing. When 
                     * the main flow of execution completed, the ambient scope there (which we think is our 
                     * parent scope) got disposed of as it should.
                     * 
                     * So here we are: our parent scope isn't actually our parent scope. It was the ambient
                     * scope in the main flow of execution from which we branched off. We should never have seen 
                     * it. Whoever wrote the code that created this parallel task should have suppressed
                     * the ambient context before creating the task - that way we wouldn't have captured
                     * this bogus parent scope.
                     * 
                     * While this is definitely a programming error, it's not worth throwing here. We can only 
                     * be in one of two scenario:
                     * 
                     * - If the developer who created the parallel task was mindful to force the creation of 
                     * a new scope in the parallel task (with IDbContextScopeFactory.CreateNew() instead of 
                     * JoinOrCreate()) then no harm has been done. We haven't tried to access the same DbContext
                     * instance from multiple threads.
                     * 
                     * - If this was not the case, they probably already got an exception complaining about the same
                     * DbContext or ObjectContext being accessed from multiple threads simultaneously (or a related
                     * error like multiple active result sets on a DataReader, which is caused by attempting to execute
                     * several queries in parallel on the same DbContext instance). So the code has already blow up.
                     * 
                     * So just record a warning here. Hopefully someone will see it and will fix the code.
                     */

                    var message = @"PROGRAMMING ERROR - When attempting to dispose a DbContextScope, we found that our parent DbContextScope has already been disposed! This means that someone started a parallel flow of execution (e.g. created a TPL task, created a thread or enqueued a work item on the ThreadPool) within the context of a DbContextScope without suppressing the ambient context first. 

In order to fix this:
1) Look at the stack trace below - this is the stack trace of the parallel task in question.
2) Find out where this parallel task was created.
3) Change the code so that the ambient context is suppressed before the parallel task is created. You can do this with IDbContextScopeFactory.SuppressAmbientContext() (wrap the parallel task creation code block in this). 

Stack Trace:
" + Environment.StackTrace;

                    Debug.WriteLine(message);
                } else
                    SetAmbientScope(_parentScope);
            }

            _contexts = null;
            _disposed = true;
        }

        public void SaveChanges() => SaveChangesAsync().WaitAndUnwrapException();

        public async Task SaveChangesAsync() {
            if (_contexts._contentLinkContext.IsValueCreated)
                await _contexts._contentLinkContext.Value.SaveChanges().ConfigureAwait(false);
            if (_contexts._gameContext.IsValueCreated)
                await _contexts._gameContext.Value.SaveChanges().ConfigureAwait(false);
            if (_contexts._settingsContext.IsValueCreated)
                await _contexts._settingsContext.Value.SaveChanges().ConfigureAwait(false);
        }

        /// <summary>
        ///     Clears the ambient scope from the CallContext but keeps tracking its instance. Call this to temporarily
        ///     hide the ambient context (e.g. to prevent it from being captured by parallel task).
        /// </summary>
        internal static void HideAmbientScope() => SetId(null);

        public IGameContext GameContext() => _contexts._gameContext.Value;

        public ISettingsStorage SettingsContext() => _contexts._settingsContext.Value;

        /// <summary>
        ///     Makes the provided 'dbContextScope' available as the the ambient scope via the CallContext.
        /// </summary>
        internal static void SetAmbientScope(DbContextScope newAmbientScope) {
            if (newAmbientScope == null)
                throw new ArgumentNullException(nameof(newAmbientScope));

            var current = GetIdentifier();

            var id = newAmbientScope._instanceIdentifier;
            if (current == id)
                return;

            // Store the new scope's instance identifier in the CallContext, making it the ambient scope
            SetId(id);

            // Keep track of this instance (or do nothing if we're already tracking it)
            dbContextScopeInstances.GetValue(id, key => newAmbientScope);
        }

        private static void SetId(InstanceIdentifier id) => CallContext.LogicalSetData(ambientDbContextScopeKey, id);

        /// <summary>
        ///     Clears the ambient scope from the CallContext and stops tracking its instance.
        ///     Call this when a DbContextScope is being disposed.
        /// </summary>
        internal static void RemoveAmbientScope() {
            var current = GetIdentifier();
            SetId(null);

            // If there was an ambient scope, we can stop tracking it now
            if (current != null)
                dbContextScopeInstances.Remove(current);
        }

        /// <summary>
        ///     Get the current ambient scope or null if no ambient scope has been setup.
        /// </summary>
        internal static DbContextScope GetAmbientScope() {
            // Retrieve the identifier of the ambient scope (if any)
            var instanceIdentifier = GetIdentifier();
            if (instanceIdentifier == null) {
                return null;
                // Either no ambient context has been set or we've crossed an app domain boundary and have (intentionally) lost the ambient context
            }

            // Retrieve the DbContextScope instance corresponding to this identifier
            DbContextScope ambientScope;
            if (dbContextScopeInstances.TryGetValue(instanceIdentifier, out ambientScope))
                return ambientScope;

            // We have an instance identifier in the CallContext but no corresponding instance
            // in our DbContextScopeInstances table. This should never happen! The only place where
            // we remove the instance from the DbContextScopeInstances table is in RemoveAmbientScope(),
            // which also removes the instance identifier from the CallContext. 
            //
            // There's only one scenario where this could happen: someone let go of a DbContextScope 
            // instance without disposing it. In that case, the CallContext
            // would still contain a reference to the scope and we'd still have that scope's instance
            // in our DbContextScopeInstances table. But since we use a ConditionalWeakTable to store 
            // our DbContextScope instances and are therefore only holding a weak reference to these instances, 
            // the GC would be able to collect it. Once collected by the GC, our ConditionalWeakTable will return
            // null when queried for that instance. In that case, we're OK. This is a programming error 
            // but our use of a ConditionalWeakTable prevented a leak.
            Debug.WriteLine(
                "Programming error detected. Found a reference to an ambient DbContextScope in the CallContext but didn't have an instance for it in our DbContextScopeInstances table. This most likely means that this DbContextScope instance wasn't disposed of properly. DbContextScope instance must always be disposed. Review the code for any DbContextScope instance used outside of a 'using' block and fix it so that all DbContextScope instances are disposed of.");
            return null;
        }

        private static InstanceIdentifier GetIdentifier()
            => CallContext.LogicalGetData(ambientDbContextScopeKey) as InstanceIdentifier;

        public IContentFolderLinkContext ContentLinkContext()
            => _contexts._contentLinkContext.Value;

        internal class InstanceIdentifier : MarshalByRefObject {}
    }

    internal class DbContexts
    {
        private readonly ILocalCache _cache;
        internal readonly Lazy<IContentFolderLinkContext> _contentLinkContext;
        internal readonly Lazy<ISettingsStorage> _settingsContext;
        internal Lazy<IGameContext> _gameContext;

        public DbContexts(ILocalCache cache, ISettingsStorage settingsStorage) {
            _cache = cache;
            _gameContext = new Lazy<IGameContext>(Factory);
            _settingsContext = new Lazy<ISettingsStorage>(() => settingsStorage);
            _contentLinkContext =
                new Lazy<IContentFolderLinkContext>(
                    () =>
                        new ContentFolderLinkContext(Common.Paths.LocalDataPath.GetChildFileWithName("folderlink.json")));
        }

        GameContext Factory() {
            var gameContextJsonImplementation = new GameContextJsonImplementation(_cache);
            // Workaround for nasty issue where we get the DomainEventHandler from the same lazy instance during load :S
            _gameContext = new Lazy<IGameContext>(() => gameContextJsonImplementation);
            return gameContextJsonImplementation;
        }
    }
}