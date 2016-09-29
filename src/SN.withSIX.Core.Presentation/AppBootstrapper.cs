﻿// <copyright company="SIX Networks GmbH" file="AppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using Akavache;
using ReactiveUI;
using SimpleInjector;
using Splat;
using System.Linq;
using System.Reactive;

namespace SN.withSIX.Core.Presentation
{
    public static class AppBootstrapper
    {
        public static void RegisterMessageBus(this Container container) {
            container.RegisterSingleton(new MessageBus());
            container.RegisterSingleton<IMessageBus>(container.GetInstance<MessageBus>);

            var l = Locator.CurrentMutable;
            // cant refer ReactiveUI atm until we put it into a package :)
            l.Register(() => new SimpleFilesystemProvider(), typeof(IFilesystemProvider), null);
            l.Register(() => TaskPoolScheduler.Default, typeof(IScheduler), "Taskpool");
            SQLitePCL.Batteries.Init();
        }
    }

    static class Utility
    {
        public static string GetMd5Hash(string input) {
#if WINRT
            // NB: Technically, we could do this everywhere, but if we did this
            // upgrade, we may return different strings than we used to (i.e. 
            // formatting-wise), which would break old caches.
            return MD5Core.GetHashString(input, Encoding.UTF8);
#else
            using (var md5Hasher = MD5.Create()) {
                // Convert the input string to a byte array and compute the hash.
                var data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sBuilder = new StringBuilder();
                foreach (var item in data) {
                    sBuilder.Append(item.ToString("x2"));
                }
                return sBuilder.ToString();
            }
#endif
        }

#if !WINRT
        public static IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler = null) {
            scheduler = scheduler ?? BlobCache.TaskpoolScheduler;
            var ret = new AsyncSubject<Stream>();

            Observable.Start(() => {
                try {
                    var createModes = new[]
                    {
                        FileMode.Create,
                        FileMode.CreateNew,
                        FileMode.OpenOrCreate,
                    };


                    // NB: We do this (even though it's incorrect!) because
                    // throwing lots of 1st chance exceptions makes debugging
                    // obnoxious, as well as a bug in VS where it detects
                    // exceptions caught by Observable.Start as Unhandled.
                    if (!createModes.Contains(mode) && !File.Exists(path)) {
                        ret.OnError(new FileNotFoundException());
                        return;
                    }

#if SILVERLIGHT
                    Observable.Start(() => new FileStream(path, mode, access, share, 4096), scheduler).Select(x => (Stream)x).Subscribe(ret);
#else
                    Observable.Start(() => new FileStream(path, mode, access, share, 4096, false), scheduler).Cast<Stream>().Subscribe(ret);
#endif
                } catch (Exception ex) {
                    ret.OnError(ex);
                }
            }, scheduler);

            return ret;
        }

        public static void CreateRecursive(this DirectoryInfo This) {
            This.SplitFullPath().Aggregate((parent, dir) => {
                var path = Path.Combine(parent, dir);

                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }

                return path;
            });
        }

        public static IEnumerable<string> SplitFullPath(this DirectoryInfo This) {
            var root = Path.GetPathRoot(This.FullName);
            var components = new List<string>();
            for (var path = This.FullName; path != root && path != null; path = Path.GetDirectoryName(path)) {
                var filename = Path.GetFileName(path);
                if (String.IsNullOrEmpty(filename))
                    continue;
                components.Add(filename);
            }
            components.Add(root);
            components.Reverse();
            return components;
        }
#endif

        public static IObservable<T> LogErrors<T>(this IObservable<T> This, string message = null) {
            return Observable.Create<T>(subj => {
                return This.Subscribe(subj.OnNext,
                    ex => {
                        var msg = message ?? "0x" + This.GetHashCode().ToString("x");
                        LogHost.Default.Info("{0} failed with {1}:\n{2}", msg, ex.Message, ex.ToString());
                        subj.OnError(ex);
                    }, subj.OnCompleted);
            });
        }

        public static IObservable<Unit> CopyToAsync(this Stream This, Stream destination, IScheduler scheduler = null) {
#if WINRT
            return This.CopyToAsync(destination).ToObservable()
                .Do(x =>
                {
                    try
                    {
                        This.Dispose();
                        destination.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.WarnException("CopyToAsync failed", ex);
                    }
                });
#endif

            return Observable.Start(() => {
                try {
                    This.CopyTo(destination);
                } catch (Exception ex) {
                    LogHost.Default.WarnException("CopyToAsync failed", ex);
                } finally {
                    This.Dispose();
                    destination.Dispose();
                }
            }, scheduler ?? BlobCache.TaskpoolScheduler);
        }
    }

    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler) {
            return Utility.SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, scheduler);
        }

        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler) {
            return Utility.SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.None, scheduler);
        }

        public IObservable<Unit> CreateRecursive(string path) {
            new DirectoryInfo(path).CreateRecursive();
            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Delete(string path) {
            return Observable.Start(() => File.Delete(path), BlobCache.TaskpoolScheduler);
        }

        public string GetDefaultRoamingCacheDirectory() {
            return null;
        }

        public string GetDefaultSecretCacheDirectory() {
            return null;
        }

        public string GetDefaultLocalMachineCacheDirectory() {
            return null;
        }

        protected static string GetAssemblyDirectoryName() {
            return null;
        }
    }
}