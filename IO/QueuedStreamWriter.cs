// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System.IO;
using System.Threading.Tasks;

namespace Celeste.Mods.InputHistory.IO
{
    internal class QueuedStreamWriter : StreamWriter
    {
        private Task currentTask;

        public QueuedStreamWriter(string path) : base(path)
        {
        }

        public void WriteLineQueued(string value)
        {
            if (currentTask == null) currentTask = WriteLineAsync(value);
            else
            {
                Task previous = currentTask;
                currentTask = Task.Run(async () =>
                {
                    await previous;
                    await WriteLineAsync(value);
                });
            }
        }

        public void CloseQueued()
        {
            if (currentTask == null) Close();
            else
            {
                Task previous = currentTask;
                currentTask = Task.Run(async () =>
                {
                    await previous;
                    Close();
                });
            }
        }

        public void FlushQueued()
        {
            if (currentTask == null) currentTask = FlushAsync();
            else
            {
                Task previous = currentTask;
                currentTask = Task.Run(async () =>
                {
                    await previous;
                    await FlushAsync();
                });
            }
        }
    }
}
