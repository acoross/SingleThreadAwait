using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Threading;

namespace SingleThreadAwait
{
    class MainClass
    {
        public static void Log(string txt)
        {
            var tid = Thread.CurrentThread.ManagedThreadId.ToString();
            Console.WriteLine($"[{tid}] {txt}");
        }

        public static void TestAsyncPump()
        {
            AsyncPump.Run(async () =>
            {
                AsyncPump.Log("in task before Task.Run");

                var t = Task.Run(() =>
                {
                    AsyncPump.Log("in task.run()");

                });

                AsyncPump.Log("in task before await t");
                //t.Wait();
                await t;

                //await Task.Yield();
                //await Task.Yield();

                AsyncPump.Log("in task before await Task.Delay");

                await Task.Delay(10);

                AsyncPump.Log("Hello World!");
            });
        }

        public static void TestAsyncWorker()
        {
            AsyncWorker worker = new AsyncWorker();

            Task.Run(() =>
            {
                while(true)
                {
                    Console.WriteLine("p to post, q to quit");
                    var key = Console.ReadKey().KeyChar;
                    if (key == 'p')
                    {
                        worker.Post(async () =>
                        {
                            Log("before delay");

                            await Task.Delay(10);

                            Log("after delay");
                        });    
                    }
                    else if (key == 'q')
                    {
                        Log("before push foo");
                        worker.Post(async () =>
                        {
                            await Task.Delay(1000);
                            Log("after delay in foo");
                        });
                        worker.Stop();
                        return;
                    }
                }
            });

            worker.Run();

            Log("all done");
        }

        public static void TestAsyncWorker2()
        {
            AsyncWorker worker = new AsyncWorker();

            Log("before job");

            Job();

            Log("after job");

            worker.Run();
        }

        static async void Job()
        {
            Log("before delay");

            await Task.Delay(10);

            Log("do job");
        }

        static IEnumerator<int> TestYield()
        {
            Console.WriteLine("yield before 1");
            yield return 1;

            Console.WriteLine("yield before 2");

            yield return 2;
        }

        public static void Main(string[] args)
        {
            var e = TestYield();

            Console.WriteLine("before move");

            e.MoveNext();
            var num = e.Current;
            TestAsyncWorker();

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
