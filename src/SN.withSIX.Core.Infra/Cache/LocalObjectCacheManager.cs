// <copyright company="SIX Networks GmbH" file="LocalObjectCacheManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using Akavache;
using SN.withSIX.Core.Infra.Services;

namespace SN.withSIX.Core.Infra.Cache
{
    public class LocalObjectCacheManager : IInfrastructureService, ILocalObjectCacheManager
    {
        readonly ILocalCache _localCache;

        public LocalObjectCacheManager(ILocalCache localCache) {
            _localCache = localCache;
        }

        public IObservable<T> GetObject<T>(string key) => _localCache.GetObject<T>(key);

        public IObservable<Unit> SetObject<T>(string key, T value) => _localCache.InsertObject(key, value);

        public IObservable<Unit> SetObject<T>(string key, T value, DateTimeOffset? absoluteExpiration)
            => _localCache.InsertObject(key, value, absoluteExpiration);

        public IObservable<T> GetOrCreateObject<T>(string key, Func<T> createFunc)
            => _localCache.GetOrCreateObject(key, createFunc);

        public IObservable<T> GetOrCreateObject<T>(string key, Func<T> createFunc, DateTimeOffset? absoluteExpiration)
            => _localCache.GetOrCreateObject(key, createFunc, absoluteExpiration);
    }
}