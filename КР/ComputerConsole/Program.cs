
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ComputerLibrary;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine(
            $"Поток {Thread.CurrentThread.ManagedThreadId}: Начало работы программы");

        // Создаём 1000 компьютеров
        List<Computer> computers = CreateComputers(1000);

        Console.WriteLine($"Создано компьютеров: {computers.Count}");

        // Объект для вывода сообщений о ходе выполнения
        IProgress<string> progress = new Progress<string>(
            message => Console.WriteLine(message));

        StreamService<Computer> service = new StreamService<Computer>();

        // Общий поток для записи и копирования
        using MemoryStream memoryStream = new MemoryStream();

        string fileName = "Computers.json";

        Console.WriteLine(
            $"Поток {Thread.CurrentThread.ManagedThreadId}: Запуск операций");

        // Запускаем первую операцию
        Task writeTask = Task.Run(() =>
            service.WriteToStreamAsync(memoryStream, computers, progress));

        // Задержка между запусками
        await Task.Delay(150);

        // Запускаем вторую операцию
        Task copyTask = Task.Run(() =>
            service.CopyFromStreamAsync(memoryStream, fileName, progress));

        Console.WriteLine(
            $"Поток {Thread.CurrentThread.ManagedThreadId}: Обе операции запущены");

        // Ожидаем завершения обеих операций
        await Task.WhenAll(writeTask, copyTask);

        Console.WriteLine("\nЗапись и копирование завершены.");

        // Получаем статистику по всем маркам компьютеров
        string[] brands =
        {
            "Lenovo", "HP", "ASUS", "Acer", "Dell", "Apple"
        };

        Console.WriteLine("\n=== Статистическая информация ===");

        foreach (string brand in brands)
        {
            int statistics = await service.GetStatisticsAsync(
                fileName,
                computer => computer.Brand == brand);

            Console.WriteLine(
                $"Количество компьютеров марки {brand}: {statistics}");
        }

        Console.WriteLine("\n=== Работа программы завершена ===");
    }

    // Создание коллекции компьютеров
    static List<Computer> CreateComputers(int count)
    {
        string[] brands =
        {
            "Lenovo", "HP", "ASUS", "Acer", "Dell", "Apple"
        };

        List<Computer> computers = new List<Computer>();

        for (int i = 1; i <= count; i++)
        {
            string brand = brands[Random.Shared.Next(brands.Length)];

            Computer computer = new Computer(
                i,
                $"Computer {i}",
                brand);

            computers.Add(computer);
        }

        return computers;
    }
}
