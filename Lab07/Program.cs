using System;
using System.Threading;

namespace TPProj
{
    class Program
    {
        static long Factorial(int n)
        {
            if (n == 0) return 1;
            return n * Factorial(n - 1);
        }
        static void Main()
        {
            double servIntens = 20.0 / 1000;
            double reqIntens = 100.0 / 1000;
            int reqN = 100;
            int N = 3;

            double ro = reqIntens / servIntens;
            double P0 = 0;
            for (int i = 0; i <= N; i++)
            {
                P0 += Math.Pow(ro, i) / Factorial(i);
            }
            P0 = 1 / P0;
            double Pn = P0 * Math.Pow(ro, N) / Factorial(N);
            double Q = 1 - Pn;
            double A = reqIntens * Q;
            double k = A / servIntens;
            Console.WriteLine($"Вероятность простоя системы          : {Math.Round(P0, 5)}");
            Console.WriteLine($"Вероятность отказа системы           : {Math.Round(Pn, 5)}");
            Console.WriteLine($"Относительная пропускная способность : {Math.Round(Q, 5)}");
            Console.WriteLine($"Абсолютная пропускная способность    : {Math.Round(A, 5)}");
            Console.WriteLine($"Среднее число занятых каналов        : {Math.Round(k, 5)}");
        }
    }

    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
    }

    class Server
    {
        private PoolRecord[] pool;
        private object threadLock = new object();
        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;

        public Server(int n, int serverTime)
        {   
            pool = new PoolRecord[n];
        }

        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                Console.WriteLine("Заявка с номером: {0}", e.id);
                requestCount++;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        return;
                    }
                }
                rejectedCount++;
            }
        }

        public void Answer(object arg)
        {
            int id = (int)arg;
            Console.WriteLine("Обработка заявки: {0}", id);
            Thread.Sleep(500);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i].thread == Thread.CurrentThread)
                    pool[i].in_use = false;
        }
    }

    class Client
    {
        private Server server;
        public Client(Server server)
        {
            this.server = server;
            request += server.proc;
        }

        public void send(int id)
        {
            procEventArgs args = new procEventArgs();
            args.id = id;
            OnProc(args);
        }

        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<procEventArgs> request;
    }

    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
}