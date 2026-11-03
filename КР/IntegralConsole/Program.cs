using System;
using System.Threading;
using IntegralLibrary;

class Program
{
    private static readonly object consoleLock = new object();
    private static int activeThreads = 0;
    private static int maxActiveThreads = 0;

    static void Main()
    {
        Console.WriteLine("=== Два потока с разными приоритетами ===");

        RunTwoThreads();

        Console.WriteLine();
        Console.WriteLine("=== Пять потоков: допускается только один ===");

        RunFiveThreads(1);

        Console.WriteLine();
        Console.WriteLine("=== Пять потоков: допускается три ===");

        RunFiveThreads(3);

        Console.WriteLine("\nРабота завершена.");
        Console.ReadKey();
    }

    static void RunTwoThreads()
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(2, 2);

        Thread t1 = CreateThread(semaphore, "Highest");
        Thread t2 = CreateThread(semaphore, "Lowest");

        t1.Priority = ThreadPriority.Highest;
        t2.Priority = ThreadPriority.Lowest;

        t1.Start();
        t2.Start();

        t1.Join();
        t2.Join();
    }

    static void RunFiveThreads(int maxParallel)
    {
        activeThreads = 0;
        maxActiveThreads = 0;
        SemaphoreSlim semaphore =
            new SemaphoreSlim(maxParallel, maxParallel);

        Thread[] threads = new Thread[5];

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = CreateThread(semaphore, $"Поток-{i + 1}");
            threads[i].Start();
        }

        foreach (Thread thread in threads)
            thread.Join();
        Console.WriteLine(
             $"Максимальное количество одновременно работающих потоков: " +
             $"{Volatile.Read(ref maxActiveThreads)}");

        Console.WriteLine(
            $"Установленное ограничение: {maxParallel}");
    }


    static Thread CreateThread(
        SemaphoreSlim semaphore, string name)
    {
        return new Thread(() =>
        {
            // Выбираем режим вычисления
            // true — тестовый режим
            // false — режим сдачи
            bool testMode = false;
            int delay = testMode ? 0 : 100000;

            IntegralCalculator calculator =
                new IntegralCalculator(semaphore, delay);

            // Событие начала выполнения
            calculator.ExecutionStarted += (threadId) =>
            {
                int current = Interlocked.Increment(ref activeThreads);

                int previous;

                do
                {
                    previous = Volatile.Read(ref maxActiveThreads);

                    if (current <= previous)
                        break;

                } while (Interlocked.CompareExchange(
                    ref maxActiveThreads, current, previous) != previous);

                lock (consoleLock)
                {
                    Console.WriteLine(
                        $"Поток {name} начал работу. " +
                        $"Одновременно работают: {current}");
                }
            };

            // Событие окончания выполнения
            calculator.ExecutionEnded += (threadId) =>
            {
                int current = Interlocked.Decrement(ref activeThreads);

                lock (consoleLock)
                {
                    Console.WriteLine(
                        $"Поток {name} завершил работу. " +
                        $"Одновременно работают: {current}");
                }
            };

            // Событие прогресса
            calculator.ProgressChanged += (sender, e) =>
            {
                lock (consoleLock)
                {
                    Console.WriteLine(
                        $"Поток {name}: " +
                        $"{MakeProgressBar(e.Percent)} " +
                        $"{e.Percent:F0}%");
                }
            };

            // Событие завершения вычисления
            calculator.CalculationCompleted += (sender, e) =>
            {
                lock (consoleLock)
                {
                    Console.WriteLine(
                        $"Поток {name}: Завершен " +
                        $"с результатом: {e.Result:F10}");

                    Console.WriteLine(
                        $"Время выполнения: {e.ElapsedTicks} ticks");
                }
            };

            // Запуск вычисления
            calculator.Calculate();
        });
    }

    static string MakeProgressBar(double percent)
    {
        const int width = 20;

        int filled = (int)(percent / 100.0 * width);

        if (filled > width)
            filled = width;

        return "[" +
               new string('=', filled) +
               ">" +
               new string(' ', width - filled) +
               "]";
    }
}

