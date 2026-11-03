using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerLibrary
{
    public class StreamService<T>
    {
        // Синхронизация доступа к общему потоку
        private readonly SemaphoreSlim streamLock = new SemaphoreSlim(1, 1);

        // Ожидание завершения записи перед копированием
        private readonly TaskCompletionSource<bool> writeCompleted =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        // 1. Асинхронная запись данных в поток
        public async Task WriteToStreamAsync(
            Stream stream,
            IEnumerable<T> data,
            IProgress<string> progress)
        {
            progress?.Report(
                $"Поток {Thread.CurrentThread.ManagedThreadId}: Вход в WriteToStreamAsync");

            try
            {
                progress?.Report(
                    $"Поток {Thread.CurrentThread.ManagedThreadId}: Начало записи");

                // Имитация медленной записи (4 секунды)
                await Task.Delay(4000);

                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data);

                await streamLock.WaitAsync();

                try
                {
                    stream.Position = 0;
                    stream.SetLength(0);

                    await stream.WriteAsync(bytes);
                    await stream.FlushAsync();
                }
                finally
                {
                    streamLock.Release();
                }

                progress?.Report(
                    $"Поток {Thread.CurrentThread.ManagedThreadId}: Запись завершена");

                writeCompleted.TrySetResult(true);
            }
            catch (Exception ex)
            {
                writeCompleted.TrySetException(ex);
                throw;
            }
        }

        // 2. Асинхронное копирование данных из потока в файл
        public async Task CopyFromStreamAsync(
            Stream stream,
            string filename,
            IProgress<string> progress)
        {
            progress?.Report(
                $"Поток {Thread.CurrentThread.ManagedThreadId}: Вход в CopyFromStreamAsync");

            // Копирование начнётся только после завершения записи
            await writeCompleted.Task;

            progress?.Report(
                $"Поток {Thread.CurrentThread.ManagedThreadId}: Начало копирования");

            await streamLock.WaitAsync();

            try
            {
                stream.Position = 0;

                using FileStream fileStream = new FileStream(
                    filename,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true);

                await stream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            finally
            {
                streamLock.Release();
            }

            progress?.Report(
                $"Поток {Thread.CurrentThread.ManagedThreadId}: Копирование завершено");
        }

        // 3. Асинхронная статистика по заданному условию
        public async Task<int> GetStatisticsAsync(
            string filename,
            Func<T, bool> filter)
        {
            using FileStream fileStream = new FileStream(
                filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                useAsync: true);

            List<T> data =
                await JsonSerializer.DeserializeAsync<List<T>>(fileStream);

            if (data == null)
                return 0;

            return data.Count(filter);
        }
    }
}
