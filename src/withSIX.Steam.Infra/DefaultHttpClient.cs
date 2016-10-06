using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;

namespace withSIX.Steam.Infra
{
    /// <summary>
    /// The default <see cref="IHttpClient"/> implementation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:Implement IDisposable", Justification = "Response task returned to the caller so cannot dispose Http Client")]
    public class DefaultHttpClient : IHttpClient
    {
        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;

        private IConnection _connection;

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public void Initialize(IConnection connection) {
            _connection = connection;

            _longRunningClient = new HttpClient(CreateHandler());

            // Disabling the Http Client timeout 
            _longRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);

            _shortRunningClient = new HttpClient(CreateHandler());
            _shortRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);
        }

        protected virtual HttpMessageHandler CreateHandler() {
            return new DefaultHttpHandler(_connection);
        }

        public class DefaultHttpHandler : HttpClientHandler
        {
            private readonly IConnection _connection;

            public DefaultHttpHandler(IConnection connection) {
                if (connection != null) {
                    _connection = connection;
                } else {
                    throw new ArgumentNullException("connection");
                }

                Credentials = _connection.Credentials;

                //if (this.SupportsPreAuthenticate())
                //{
                    PreAuthenticate = true;
                //}

                if (_connection.CookieContainer != null) {
                    CookieContainer = _connection.CookieContainer;
                }

#if !PORTABLE
                if (_connection.Proxy != null) {
                    Proxy = _connection.Proxy;
                }
#endif

#if (NET4 || NET45)
            foreach (X509Certificate cert in _connection.Certificates)
            {
                ClientCertificates.Add(cert);
            }
#endif
            }
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning) {
            if (prepareRequest == null) {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            var request = new HttpRequestMessageWrapper(requestMessage, () => {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = GetHttpClient(isLongRunning);

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .Then(responseMessage => {
                    if (responseMessage.IsSuccessStatusCode) {
                        responseDisposer.Set(responseMessage);
                    } else {
                        throw new HttpClientException(responseMessage);
                    }

                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning) {
            if (prepareRequest == null) {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null) {
                requestMessage.Content = new StringContent(String.Empty);
            } else {
                requestMessage.Content = new ByteArrayContent(HttpHelper.ProcessPostData(postData));
            }

            var request = new HttpRequestMessageWrapper(requestMessage, () => {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = GetHttpClient(isLongRunning);

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .Then(responseMessage => {
                    if (responseMessage.IsSuccessStatusCode) {
                        responseDisposer.Set(responseMessage);
                    } else {
                        throw new HttpClientException(responseMessage);
                    }

                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });
        }

        /// <summary>
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private HttpClient GetHttpClient(bool isLongRunning) {
            return isLongRunning ? _longRunningClient : _shortRunningClient;
        }
    }


    internal static class HttpHelper
    {
#if CLIENT_NET4

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<HttpWebResponse>(ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<Stream> GetHttpRequestStreamAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<Stream>(ex);
            }
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetHttpResponseAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (requestPreparer != null)
            {
                requestPreparer(request);
            }

            byte[] buffer = ProcessPostData(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the content length if the buffer is non-null
            request.ContentLength = buffer != null ? buffer.LongLength : 0;

            if (buffer == null)
            {
                // If there's nothing to be written to the request then just get the response
                return request.GetHttpResponseAsync();
            }

            // Write the post data to the request stream
            return request.GetHttpRequestStreamAsync()
                .Then(stream => stream.WriteAsync(buffer).Then(() => stream.Dispose()))
                .Then(() => request.GetHttpResponseAsync());
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Text.StringBuilder.AppendFormat(System.String,System.Object[])", Justification = "This will never be localized.")]
        public static byte[] ProcessPostData(IDictionary<string, string> postData) {
            if (postData == null || postData.Count == 0) {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var pair in postData) {
                if (sb.Length > 0) {
                    sb.Append("&");
                }

                if (String.IsNullOrEmpty(pair.Value)) {
                    continue;
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", pair.Key, UrlEncoder.UrlEncode(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }

    internal static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeTask<object>(null);
        private static readonly Task<bool> _trueTask = MakeTask<bool>(true);
        private static readonly Task<bool> _falseTask = MakeTask<bool>(false);

        private static Task<T> MakeTask<T>(T value) {
            return FromResult<T>(value);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<bool> True
        {
            get
            {
                return _trueTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<bool> False
        {
            get
            {
                return _falseTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task OrEmpty(this Task task) {
            return task ?? Empty;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> OrEmpty<T>(this Task<T> task) {
            return task ?? TaskCache<T>.Empty;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) {
            try {
                return Task.Factory.FromAsync(beginMethod, endMethod, state);
            } catch (Exception ex) {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<T> FromAsync<T>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod, object state) {
            try {
                return Task.Factory.FromAsync<T>(beginMethod, endMethod, state);
            } catch (Exception ex) {
                return TaskAsyncHelper.FromError<T>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
#if SERVER
        public static TTask Catch<TTask>(this TTask task, TraceSource traceSource = null) where TTask : Task
        {
            return Catch(task, ex => { }, traceSource);
        }
#else
        public static TTask Catch<TTask>(this TTask task, IConnection connection = null) where TTask : Task {
            return Catch(task, ex => { }, connection);
        }
#endif

#if PERFCOUNTERS
        public static TTask Catch<TTask>(this TTask task, TraceSource traceSource, params IPerformanceCounter[] counters) where TTask : Task
        {
            return Catch(task, _ =>
                {
                    if (counters == null)
                    {
                        return;
                    }
                    for (var i = 0; i < counters.Length; i++)
                    {
                        counters[i].Increment();
                    }
                },
                traceSource);
        }
#endif

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
#if SERVER
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException, object> handler, object state, TraceSource traceSource = null) where TTask : Task
#else
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException, object> handler, object state, IConnection connection = null) where TTask : Task
#endif
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion) {
                if (task.Status == TaskStatus.Faulted) {
#if SERVER
                    ExecuteOnFaulted(handler, state, task.Exception, traceSource);
#else
                    ExecuteOnFaulted(handler, state, task.Exception, connection);
#endif
                } else {
#if SERVER
                    AttachFaultedContinuation<TTask>(task, handler, state, traceSource);
#else
                    AttachFaultedContinuation<TTask>(task, handler, state, connection);
#endif
                }
            }

            return task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
#if SERVER
        private static void AttachFaultedContinuation<TTask>(TTask task, Action<AggregateException, object> handler, object state, TraceSource traceSource) where TTask : Task
#else      
        private static void AttachFaultedContinuation<TTask>(TTask task, Action<AggregateException, object> handler, object state, IConnection connection) where TTask : Task
#endif
        {
            task.ContinueWith(innerTask => {
#if SERVER
                ExecuteOnFaulted(handler, state, innerTask.Exception, traceSource);
#else
                ExecuteOnFaulted(handler, state, innerTask.Exception, connection);
#endif
            },
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
#if SERVER
        private static void ExecuteOnFaulted(Action<AggregateException, object> handler, object state, AggregateException exception, TraceSource traceSource)
#else
        private static void ExecuteOnFaulted(Action<AggregateException, object> handler, object state, AggregateException exception, IConnection connection)
#endif
        {
            // Observe Exception
#if SERVER
            if (traceSource != null)
            {
                traceSource.TraceEvent(TraceEventType.Warning, 0, "Exception thrown by Task: {0}", exception);
            }
#else
            if (connection != null) {
                connection.Trace(TraceLevels.Messages, "Exception thrown by Task: {0}", new[] { exception });
            }
#endif
            handler(exception, state);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
#if SERVER
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException> handler, TraceSource traceSource = null) where TTask : Task
#else
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException> handler, IConnection connection = null) where TTask : Task
#endif
        {
            return task.Catch((ex, state) => ((Action<AggregateException>)state).Invoke(ex),
                              handler,
#if SERVER
                              traceSource
#else
                              connection
#endif
                              );
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task ContinueWithNotComplete(this Task task, Action action) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    try {
                        action();
                        return task;
                    } catch (Exception e) {
                        return FromError(e);
                    }
                case TaskStatus.RanToCompletion:
                    return task;
                default:
                    var tcs = new TaskCompletionSource<object>();

                    task.ContinueWith(t => {
                        if (t.IsFaulted || t.IsCanceled) {
                            try {
                                action();

                                if (t.IsFaulted) {
                                    tcs.TrySetUnwrappedException(t.Exception);
                                } else {
                                    tcs.TrySetCanceled();
                                }
                            } catch (Exception e) {
                                tcs.TrySetException(e);
                            }
                        } else {
                            tcs.TrySetResult(null);
                        }
                    },
                    TaskContinuationOptions.ExecuteSynchronously);

                    return tcs.Task;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static void ContinueWithNotComplete(this Task task, TaskCompletionSource<object> tcs) {
            task.ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.SetUnwrappedException(t.Exception);
                } else if (t.IsCanceled) {
                    tcs.SetCanceled();
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task ContinueWith(this Task task, TaskCompletionSource<object> tcs) {
            task.ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.TrySetUnwrappedException(t.Exception);
                } else if (t.IsCanceled) {
                    tcs.TrySetCanceled();
                } else {
                    tcs.TrySetResult(null);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static void ContinueWith<T>(this Task<T> task, TaskCompletionSource<T> tcs) {
            task.ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.TrySetUnwrappedException(t.Exception);
                } else if (t.IsCanceled) {
                    tcs.TrySetCanceled();
                } else {
                    tcs.TrySetResult(t.Result);
                }
            });
        }

        // Then extesions
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task task, Action successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult>(this Task task, Func<TResult> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, TResult>.RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1>(this Task task, Action<T1> successor, T1 arg1) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, object, T1, object, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1, T2>(this Task task, Action<T1, T2> successor, T1 arg1, T2 arg2) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, object, T1, T2, object>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1>(this Task task, Func<T1, Task> successor, T1 arg1) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, Task, T1, object, object>.ThenWithArgs(task, successor, arg1)
                                                                     .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1, T2>(this Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, Task, T1, T2, object>.ThenWithArgs(task, successor, arg1, arg2)
                                                                 .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1, T2, T3>(this Task task, Func<T1, T2, T3, Task> successor, T1 arg1, T2 arg2, T3 arg3) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2, arg3);

                default:
                    return GenericDelegates<object, Task, T1, T2, T3>.ThenWithArgs(task, successor, arg1, arg2, arg3)
                                                                 .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, Task<TResult>> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result))
                                                        .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, TResult> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, T1, TResult>(this Task<T> task, Func<T, T1, TResult> successor, T1 arg1) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, TResult, T1, object, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, T1, T2, TResult>(this Task<T> task, Func<T, T1, T2, TResult> successor, T1 arg1, T2 arg2) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1, arg2);

                default:
                    return GenericDelegates<T, TResult, T1, T2, object>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task task, Func<Task> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task>.RunTask(task, successor)
                                                    .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task<TResult>>.RunTask(task, successor)
                                                             .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, object>.RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T, T1>(this Task<T> task, Action<T, T1> successor, T1 arg1) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, object, T1, object, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<TResult>(this Task<TResult> task, Func<TResult, Task> successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, Task>.RunTask(task, t => successor(t.Result))
                                                     .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult, T1>(this Task<TResult> task, Func<Task<TResult>, T1, Task<TResult>> successor, T1 arg1) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task, arg1);

                default:
                    return GenericDelegates<TResult, Task<TResult>, T1, object, object>.ThenWithArgs(task, successor, arg1)
                                                                               .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Finally(this Task task, Action<object> next, object state) {
            try {
                switch (task.Status) {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        next(state);
                        return task;
                    case TaskStatus.RanToCompletion:
                        return FromMethod(next, state);

                    default:
                        return RunTaskSynchronously(task, next, state, onlyOnSuccess: false);
                }
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task RunSynchronously(this Task task, Action successor) {
            switch (task.Status) {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTaskSynchronously(task, state => ((Action)state).Invoke(), successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task FastUnwrap(this Task<Task> task) {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task) {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Delay(TimeSpan timeOut) {
#if NETFX_CORE
            return Task.Delay(timeOut);
#else
            var tcs = new TaskCompletionSource<object>();

            var timer = new Timer(tcs.SetResult,
            null,
            timeOut,
            TimeSpan.FromMilliseconds(-1));

            return tcs.Task.ContinueWith(_ => {
                timer.Dispose();
            },
            TaskContinuationOptions.ExecuteSynchronously);
#endif
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod(Action func) {
            try {
                func();
                return Empty;
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1>(Action<T1> func, T1 arg) {
            try {
                func(arg);
                return Empty;
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1, T2>(Action<T1, T2> func, T1 arg1, T2 arg2) {
            try {
                func(arg1, arg2);
                return Empty;
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod(Func<Task> func) {
            try {
                return func();
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<TResult>(Func<Task<TResult>> func) {
            try {
                return func();
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<TResult>(Func<TResult> func) {
            try {
                return FromResult<TResult>(func());
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1>(Func<T1, Task> func, T1 arg) {
            try {
                return func(arg);
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1, T2>(Func<T1, T2, Task> func, T1 arg1, T2 arg2) {
            try {
                return func(arg1, arg2);
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1, T2, T3>(Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3) {
            try {
                return func(arg1, arg2, arg3);
            } catch (Exception ex) {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, TResult>(Func<T1, Task<TResult>> func, T1 arg) {
            try {
                return func(arg);
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, TResult>(Func<T1, TResult> func, T1 arg) {
            try {
                return FromResult<TResult>(func(arg));
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func, T1 arg1, T2 arg2) {
            try {
                return func(arg1, arg2);
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            try {
                return FromResult<TResult>(func(arg1, arg2));
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            try {
                return FromResult<TResult>(func(arg1, arg2, arg3));
            } catch (Exception ex) {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> FromResult<T>(T value) {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task FromError(Exception e) {
            return FromError<object>(e);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task<T> FromError<T>(Exception e) {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetUnwrappedException<T>(e);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static void SetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e) {
            var aggregateException = e as AggregateException;
            if (aggregateException != null) {
                tcs.SetException(aggregateException.InnerExceptions);
            } else {
                tcs.SetException(e);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static bool TrySetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e) {
            var aggregateException = e as AggregateException;
            if (aggregateException != null) {
                return tcs.TrySetException(aggregateException.InnerExceptions);
            } else {
                return tcs.TrySetException(e);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task Canceled() {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task<T> Canceled<T>() {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        private static Task RunTask(Task task, Action successor) {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.SetUnwrappedException(t.Exception);
                } else if (t.IsCanceled) {
                    tcs.SetCanceled();
                } else {
                    try {
                        successor();
                        tcs.SetResult(null);
                    } catch (Exception ex) {
                        tcs.SetUnwrappedException(ex);
                    }
                }
            });

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task RunTaskSynchronously(Task task, Action<object> next, object state, bool onlyOnSuccess = true) {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t => {
                try {
                    if (t.IsFaulted) {
                        if (!onlyOnSuccess) {
                            next(state);
                        }

                        tcs.SetUnwrappedException(t.Exception);
                    } else if (t.IsCanceled) {
                        if (!onlyOnSuccess) {
                            next(state);
                        }

                        tcs.SetCanceled();
                    } else {
                        next(state);
                        tcs.SetResult(null);
                    }
                } catch (Exception ex) {
                    tcs.SetUnwrappedException(ex);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private static class TaskRunners<T, TResult>
        {
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task RunTask(Task<T> task, Action<T> successor) {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(t => {
                    if (t.IsFaulted) {
                        tcs.SetUnwrappedException(t.Exception);
                    } else if (t.IsCanceled) {
                        tcs.SetCanceled();
                    } else {
                        try {
                            successor(t.Result);
                            tcs.SetResult(null);
                        } catch (Exception ex) {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }


            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task RunTask(Task<T> task, Action<Task<T>> successor) {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(t => {
                    if (task.IsFaulted) {
                        tcs.SetUnwrappedException(t.Exception);
                    } else if (task.IsCanceled) {
                        tcs.SetCanceled();
                    } else {
                        try {
                            successor(t);
                            tcs.SetResult(null);
                        } catch (Exception ex) {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task<TResult> RunTask(Task task, Func<TResult> successor) {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t => {
                    if (t.IsFaulted) {
                        tcs.SetUnwrappedException(t.Exception);
                    } else if (t.IsCanceled) {
                        tcs.SetCanceled();
                    } else {
                        try {
                            tcs.SetResult(successor());
                        } catch (Exception ex) {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task<TResult> RunTask(Task<T> task, Func<Task<T>, TResult> successor) {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t => {
                    if (task.IsFaulted) {
                        tcs.SetUnwrappedException(t.Exception);
                    } else if (task.IsCanceled) {
                        tcs.SetCanceled();
                    } else {
                        try {
                            tcs.SetResult(successor(t));
                        } catch (Exception ex) {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }
        }

        private static class GenericDelegates<T, TResult, T1, T2, T3>
        {
            internal static Task ThenWithArgs(Task task, Action<T1> successor, T1 arg1) {
                return RunTask(task, () => successor(arg1));
            }

            internal static Task ThenWithArgs(Task task, Action<T1, T2> successor, T1 arg1, T2 arg2) {
                return RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task ThenWithArgs(Task<T> task, Action<T, T1> successor, T1 arg1) {
                return TaskRunners<T, object>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<TResult> ThenWithArgs(Task task, Func<T1, TResult> successor, T1 arg1) {
                return TaskRunners<object, TResult>.RunTask(task, () => successor(arg1));
            }

            internal static Task<TResult> ThenWithArgs(Task task, Func<T1, T2, TResult> successor, T1 arg1, T2 arg2) {
                return TaskRunners<object, TResult>.RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<TResult> ThenWithArgs(Task<T> task, Func<T, T1, TResult> successor, T1 arg1) {
                return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<TResult> ThenWithArgs(Task<T> task, Func<T, T1, T2, TResult> successor, T1 arg1, T2 arg2) {
                return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result, arg1, arg2));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, Task> successor, T1 arg1) {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2) {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, T2, T3, Task> successor, T1 arg1, T2 arg2, T3 arg3) {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1, arg2, arg3));
            }

            internal static Task<Task<TResult>> ThenWithArgs(Task<T> task, Func<T, T1, Task<TResult>> successor, T1 arg1) {
                return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<Task<T>> ThenWithArgs(Task<T> task, Func<Task<T>, T1, Task<T>> successor, T1 arg1) {
                return TaskRunners<T, Task<T>>.RunTask(task, t => successor(t, arg1));
            }
        }

        private static class TaskCache<T>
        {
            public static Task<T> Empty = MakeTask<T>(default(T));
        }
    }

    internal class Disposer : IDisposable
    {
        private static readonly object _disposedSentinel = new object();

        private object _disposable;

        public void Set(IDisposable disposable) {
            if (disposable == null) {
                throw new ArgumentNullException("disposable");
            }

            object originalFieldValue = Interlocked.CompareExchange(ref _disposable, disposable, null);
            if (originalFieldValue == null) {
                // this is the first call to Set() and Dispose() hasn't yet been called; do nothing
            } else if (originalFieldValue == _disposedSentinel) {
                // Dispose() has already been called, so we need to dispose of the object that was just added
                disposable.Dispose();
            } else {
#if !PORTABLE && !NETFX_CORE
                // Set has been called multiple times, fail
                Debug.Fail("Multiple calls to Disposer.Set(IDisposable) without calling Disposer.Dispose()");
#endif
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                var disposable = Interlocked.Exchange(ref _disposable, _disposedSentinel) as IDisposable;
                if (disposable != null) {
                    disposable.Dispose();
                }
            }
        }

        public void Dispose() {
            Dispose(true);
        }
    }

    internal static class UrlEncoder
    {
        // The implementation below is ported from WebUtility for use in .Net 4

        public static string UrlEncode(string str) {
            if (str == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] encodedBytes = UrlEncode(bytes, 0, bytes.Length, alwaysCreateNewReturnValue: false);
            return Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);
        }

        #region UrlEncode implementation

        private static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue) {
            byte[] encoded = UrlEncode(bytes, offset, count);

            return (alwaysCreateNewReturnValue && (encoded != null) && (encoded == bytes))
                ? (byte[])encoded.Clone()
                : encoded;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count) {
            if (!ValidateUrlEncodingParameters(bytes, offset, count)) {
                return null;
            }

            int cSpaces = 0;
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++) {
                char ch = (char)bytes[offset + i];

                if (ch == ' ')
                    cSpaces++;
                else if (!IsUrlSafeChar(ch))
                    cUnsafe++;
            }

            // nothing to expand?
            if (cSpaces == 0 && cUnsafe == 0)
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++) {
                byte b = bytes[offset + i];
                char ch = (char)b;

                if (IsUrlSafeChar(ch)) {
                    expandedBytes[pos++] = b;
                } else if (ch == ' ') {
                    expandedBytes[pos++] = (byte)'+';
                } else {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }

            return expandedBytes;
        }

        #endregion

        #region Helper methods

        private static char IntToHex(int n) {
            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'a');
        }

        // Set of safe chars, from RFC 1738.4 minus '+'
        private static bool IsUrlSafeChar(char ch) {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                return true;

            switch (ch) {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return false;
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }
            if (offset < 0 || offset > bytes.Length) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || offset + count > bytes.Length) {
                throw new ArgumentOutOfRangeException("count");
            }

            return true;
        }

        #endregion
    }
}