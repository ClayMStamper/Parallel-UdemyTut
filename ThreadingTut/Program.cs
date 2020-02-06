using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingTut {
    class Program {


        public class BankAccount {
            
            public object padlock = new object();

            private int balance;

            public int Balance {
                get {
                    return balance;
                }
                private set {
                    balance = value;
                }
            }

            public void Deposit(int amount) {
                balance += amount;
            }

            public void Withdraw(int amount) {
                balance -= amount;
            }

            public void Transfer(BankAccount other, int amount) {
                Withdraw(amount);
                other.Deposit(amount);
            }

            public void Print() {
                Console.WriteLine($"{ToString()}'s balance is: ${balance}");
            }
            
        }
        
        static void Main(string[] args) {

            UseMutexToTransferFunds();
            
        }

        
        static void OnFinishedMain() {
            Console.WriteLine("Main program done");
            Console.ReadKey();
        }
        
        static void UseMutexToTransferFunds() {
            var tasks = new List<Task>();
            var ba = new BankAccount();
            var ba2 = new BankAccount();

//            SpinLock sl = new SpinLock();
            Mutex mutex = new Mutex();
            Mutex mutex2 = new Mutex();
            
            for (int i = 0; i < 10; i++) {
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++) {
                        bool haveLock = mutex.WaitOne();
                        try {
                            ba.Deposit(100);
                        }
                        finally {
                            if (haveLock) mutex.ReleaseMutex();
                        }
                    }
                }));
                
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++) {
                        bool haveLock = mutex.WaitOne();
                        try {
                            ba2.Deposit(100);
                        }
                        finally {
                            if (haveLock) mutex.ReleaseMutex();
                        }
                    }
                }));
                
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++) {
                        //can only get this lock if both mutex' are available
                        Mutex[] muts = new[] {mutex, mutex2};
                        bool haveLock = WaitHandle.WaitAll(muts);
                        try {
                            ba.Transfer(ba2, 1);
                        }
                        finally {
                            if (haveLock) {
                                foreach (Mutex mut in muts) 
                                    mut.ReleaseMutex();
                            }
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            
            ba.Print();
            ba2.Print();
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