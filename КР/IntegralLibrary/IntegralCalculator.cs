using System;
using System.Diagnostics;
using System.Threading;

namespace IntegralLibrary
{
    public class ProgressEventArgs : EventArgs
    {
        public int ThreadId { get; }
        public double Percent { get; }

        public ProgressEventArgs(int threadId, double percent)
        {
            ThreadId = threadId;
            Percent = percent;
        }
    }

    public class CalculationCompletedEventArgs : EventArgs
    {
        public int ThreadId { get; }
        public double Result { get; }
        public long ElapsedTicks { get; }

        public CalculationCompletedEventArgs(
            int threadId, double result, long elapsedTicks)
        {
            ThreadId = threadId;
            Result = result;
            ElapsedTicks = elapsedTicks;
        }
    }

    public class IntegralCalculator
    {
        private readonly SemaphoreSlim semaphore;
        private readonly int delayIterations;

        private const double A = 0.0;
        private const double B = 1.0;
        private const double H = 0.00000001;

        private static double dummy;

        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<CalculationCompletedEventArgs>
            CalculationCompleted;

        public event Action<int> ExecutionStarted;
        public event Action<int> ExecutionEnded;

        public IntegralCalculator(
            SemaphoreSlim semaphore,
            int delayIterations = 100000)
        {
            this.semaphore = semaphore;
            this.delayIterations = delayIterations;
        }

        public void Calculate()
        {
            semaphore.Wait();

            try
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                ExecutionStarted?.Invoke(threadId);
                Stopwatch stopwatch = Stopwatch.StartNew();

                long count = (long)((B - A) / H);
                double sum = 0.0;

                for (long i = 0; i < count; i++)
                {
                    double x = A + (i + 0.5) * H;
                    sum += Math.Sin(x);

                    double temp = 1.0;

                    for (int j = 0; j < delayIterations; j++)
                    {
                        temp *= 1.0000000001;
                        temp /= 1.0000000001;
                    }

                    dummy = temp;

                    if (i % (count / 100) == 0)
                    {
                        double percent = (double)i / count * 100.0;

                        ProgressChanged?.Invoke(
                            this,
                            new ProgressEventArgs(threadId, percent));
                    }
                }

                double result = sum * H;
                stopwatch.Stop();

                CalculationCompleted?.Invoke(
                    this,
                    new CalculationCompletedEventArgs(
                        threadId, result, stopwatch.ElapsedTicks));
            }
            finally
            {
                ExecutionEnded?.Invoke(Thread.CurrentThread.ManagedThreadId);
                semaphore.Release();
            }
        }
    }
}

