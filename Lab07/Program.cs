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
        private static int PoissonSmall(double lambda)
        {
            Random rand = new Random();
            double p = 1.0, L = Math.Exp(-lambda);
            int k = 0;
            do
            {
                k++;
                p *= rand.NextDouble();
            }
            while (p > L);
            return k - 1;
        }
        static void Main()
        {
            double servIntens = 2.0 / 1000;
            double reqIntens = 20.0 / 1000;
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

            Server server = new Server(N, servIntens);
            Client client = new Client(server);
            CancellationTokenSource tokenSource = new();
            Thread thread = new(
                () => ThreadsCounter.Run(server, tokenSource.Token)
            );
            thread.Start();
            for (int i = 0; i < reqN; i++)
            {
                client.send(i);
                int time = Convert.ToInt32(1 / reqIntens);
                Thread.Sleep(time);
            }
            tokenSource.Cancel();
            thread.Join();
            tokenSource.Dispose();

            P0 = ThreadsCounter.worknt / ThreadsCounter.ticks;
            Pn = (double)server.rejectedCount / server.requestCount;
            Q = (double)server.processedCount / server.requestCount;
            A = servIntens * server.processedCount / server.requestCount;
            k = ThreadsCounter.work / ThreadsCounter.ticks;
            Console.WriteLine($"\nВсего заявок: {server.requestCount}");
            Console.WriteLine($"Обработано заявок: {server.processedCount}");
            Console.WriteLine($"Отклонено заявок: {server.rejectedCount}");
            Console.WriteLine($"Вероятность простоя системы          : {Math.Round(P0, 5)}");
            Console.WriteLine($"Вероятность отказа системы           : {Math.Round(Pn, 5)}");
            Console.WriteLine($"Относительная пропускная способность : {Math.Round(Q, 5)}");
            Console.WriteLine($"Абсолютная пропускная способность    : {Math.Round(A, 5)}");
            Console.WriteLine($"Среднее число занятых каналов        : {Math.Round(k, 5)}");
        }
    }
    static class ThreadsCounter
    {
        public static double work = 0;
        public static double worknt = 0;
        public static int ticks = 0;
        public static void Run(Server server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                int w = server.Workers();
                if (w == 0)
                {
                    worknt++;
                }
                else
                {
                    work += w;
                }
                ticks++;
                Thread.Sleep(10);
            }
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
        public int serverTime;

        public Server(int n, double servIntens)
        {   
            pool = new PoolRecord[n];
            serverTime = Convert.ToInt32(1 / servIntens);
        }

        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                //Console.WriteLine("Заявка с номером: {0}", e.id);
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
            //Console.WriteLine("Обработка заявки: {0}", id);
            Thread.Sleep(serverTime);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i].thread == Thread.CurrentThread)
                    pool[i].in_use = false;
        }

        public int Workers()
        {
            int w = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].in_use) w++;
            }
            return w;
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