using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Micro.NetFx.Threading.Tasks
{
    /// <summary>
    /// Source code : https://social.msdn.microsoft.com/Forums/en-US/163ef755-ff7b-4ea5-b226-bbe8ef5f4796/is-there-a-pattern-for-calling-an-async-method-synchronously?forum=async
    /// </summary>
    internal static class TaskHelpers
    {
        /// <summary>
        /// Method for performing asynchronous method in a synchronous manner
        /// </summary>
        /// <param name="taskFactory"></param>
        internal static void RunSync(Func<Task> taskFactory)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException("taskFactory");
            }
            using (var executeContext = new ExecuteSynchronizationContext())
            {
                executeContext.Post(async d =>
                {
                    using (executeContext.MessageLoopScoped())
                    {
                        var task = taskFactory();
                        if (task == null)
                        {
                            throw new ArgumentException("taskFactory must get effective results");
                        }
                        await task;
                    }
                }, null);
                executeContext.BeginMessageLoop();
            }
        }

        /// <summary>
        /// Method for performing asynchronous method in a synchronous manner
        /// </summary>
        internal static T RunSync<T>(Func<Task<T>> taskFactory)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException("taskFactory");
            }
            using (var executeContext = new ExecuteSynchronizationContext())
            {
                T result = default(T);
                executeContext.Post(async d =>
                {
                    using (executeContext.MessageLoopScoped())
                    {
                        var task = taskFactory();
                        if (task == null)
                        {
                            throw new ArgumentException("taskFactory must get effective results");
                        }
                        result = await task;
                    }
                }, null);
                executeContext.BeginMessageLoop();
                return result;
            }
        }

        /// <summary>
        /// Perform asynchronous task context
        /// </summary>
        private sealed class ExecuteSynchronizationContext : SynchronizationContext, IDisposable
        {
            private bool done;
            private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            private readonly Queue<SendOrPostCallbackEntry> entries = new Queue<SendOrPostCallbackEntry>();
            private readonly object lockObj = new object();
            private readonly SynchronizationContext currentSynchronizationContext;

            public ExecuteSynchronizationContext()
            {
                currentSynchronizationContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(this);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("cannot send to same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (lockObj)
                {
                    entries.Enqueue(new SendOrPostCallbackEntry(d, state));
                }
                autoResetEvent.Set();
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    SendOrPostCallbackEntry entry = default(SendOrPostCallbackEntry);
                    lock (lockObj)
                    {
                        if (entries.Count > 0)
                        {
                            entry = entries.Dequeue();
                        }
                    }
                    if (entry != null)
                    {
                        entry.Invoke();
                    }
                    else
                    {
                        autoResetEvent.WaitOne();
                    }
                }
            }

            public void EndMessageLoop()
            {
                Post(d => done = true, null);
            }

            /// <summary>
            /// auto invoke EndMessageLoop
            /// </summary>
            /// <returns></returns>
            public IDisposable MessageLoopScoped()
            {
                return new Disposable(this);
            }

            public void Dispose()
            {
                autoResetEvent.Dispose();
                SynchronizationContext.SetSynchronizationContext(currentSynchronizationContext);
            }

            /// <summary>
            /// Scope dispose
            /// </summary>
            private sealed class Disposable : IDisposable
            {
                private readonly ExecuteSynchronizationContext context;

                public Disposable(ExecuteSynchronizationContext context)
                {
                    this.context = context;
                }

                public void Dispose()
                {
                    context.EndMessageLoop();
                }
            }
        }

        /// <summary>
        /// sendOrPostCallback entry class
        /// </summary>
        private class SendOrPostCallbackEntry
        {
            private readonly SendOrPostCallback callback;
            private readonly object state;

            public SendOrPostCallbackEntry(SendOrPostCallback callback, object state)
            {
                this.callback = callback;
                this.state = state;
            }

            public void Invoke()
            {
                callback.Invoke(state);
            }
        }
    }
}
