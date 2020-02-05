using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingTut {
    class Program {


        static void Main(string[] args) {

            OnFinishedMain();
            
        }
        
        static void OnFinishedMain() {
            Console.WriteLine("Main program done");
            Console.ReadKey();
        }

        static void ExceptionHandlingExample() {
            
            try {
                ExceptionHandleTest();
            }
            catch (AggregateException ae) {
                foreach (var e in ae.InnerExceptions) {
                    Console.WriteLine($"Handled elsewhere: {e.GetType()}");
                }
            }
            
            static void ExceptionHandleTest() {
                var t1 = Task.Factory.StartNew(() =>
                    throw new AccessViolationException("No access") {Source = "t1"});
                var t2 = Task.Factory.StartNew(() =>
                    throw new InvalidOperationException("Not allowed") {
                        Source = "t2"
                    });

                try {
                    Task.WaitAll(t1, t2);
                }
                catch (AggregateException ae) {
                    ae.Handle(e => {
                        if (e is InvalidOperationException)
                            return true;
                        return false;
                    });
                }
            }
        }
        
        static void LinkedCTS() {
            
            var planned = new  CancellationTokenSource();
            var preventative = new CancellationTokenSource();
            var emergency = new CancellationTokenSource();

            var paranoid = CancellationTokenSource.CreateLinkedTokenSource(
                planned.Token, preventative.Token, emergency.Token);

            Task.Factory.StartNew(() => {
                int i = 0;
                while (true) {
                    paranoid.Token.ThrowIfCancellationRequested();
                    Console.WriteLine($"{i++}\t");
                    Thread.Sleep(100);
                }
            }, paranoid.Token);

            Task.Factory.StartNew(() => {
                paranoid.Token.WaitHandle.WaitOne();
                Console.WriteLine("Paranoid broke");
            });
            
            Task.Factory.StartNew(() => {
                emergency.Token.WaitHandle.WaitOne();
                Console.WriteLine("Paranoid broke");
            });
            
            Console.ReadKey();
            preventative.Cancel();
            
        }

        static void DiffuseTheBomb() {
            var cts = new CancellationTokenSource();

            var t = new Task(() => {
                Console.WriteLine("You have 5 seconds to disarm the bomb!");
                bool cancelled = cts.Token.WaitHandle.WaitOne(5000);
                Console.WriteLine(cancelled ? "Bomb disarmed." : "BOOM!!!");
            }, cts.Token);

            t.Start();
            
            Console.ReadKey();
            cts.Cancel();
        }

        static void WaitingAndStatus() {
            var cts = new CancellationTokenSource();
            
            var t = new Task(() => {
                Console.WriteLine("I take 5 seconds");

                for (int i = 0; i < 5; i++) {
                    cts.Token.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }

                Console.WriteLine("I'm done");
                
            }, cts.Token);
            
            t.Start();

            Task t2 = Task.Factory.StartNew(() => Thread.Sleep(3000), cts.Token);

            Task.WaitAll(new[] {t, t2}, 4000, cts.Token);

            Console.WriteLine($"Task t is {t.Status}");
            Console.WriteLine($"Task t2 status is {t2.Status}");
        }
        
        
    }
    
}