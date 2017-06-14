using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SingleThreadAwait
{
    // Post job, and 
    // await inside that job will be done on single thread (the AsyncWorker.Run thread)
    class AsyncWorker
    {
        SynchronizationContext prev;
        readonly SingleThreadSyncCtx syncCtx = new SingleThreadSyncCtx();

        public void Run()
        {
            prev = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            syncCtx.RunOnCurrentThread();
        }

        public void Post(Action job)
        {
            syncCtx.Post((obj) =>
            {
                job();
            }, job);
        }

        public void Stop()
        {
            Post(() =>
            {
                syncCtx.Complete();
                SynchronizationContext.SetSynchronizationContext(prev);
            });
        }

        class SingleThreadSyncCtx : SynchronizationContext
        {
            /// <summary>The queue of work items.</summary>
            private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
                new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
            /// <summary>The processing thread.</summary>
            private readonly Thread m_thread = Thread.CurrentThread;

            /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
            /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
            /// <param name="state">The object passed to the delegate.</param>
            public override void Post(SendOrPostCallback d, object state)
            {
                try
                {
                    if (d == null) throw new ArgumentNullException(nameof(d));
                    m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
                }
                catch
                {
                    Console.WriteLine("SingleThreadSyncCtx.Post(): it's closed!");
                }
            }

            /// <summary>Not supported.</summary>
            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("Synchronously sending is not supported.");
            }

            /// <summary>Runs an loop to process all queued work items.</summary>
            public void RunOnCurrentThread()
            {
                foreach (var workItem in m_queue.GetConsumingEnumerable())
                    workItem.Key(workItem.Value);
            }

            /// <summary>Notifies the context that no more work will arrive.</summary>
            public void Complete() { m_queue.CompleteAdding(); }
        }
    }

}
