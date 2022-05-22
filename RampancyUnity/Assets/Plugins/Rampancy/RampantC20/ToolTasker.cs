using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Rampancy.RampantC20
{
    // Tool commands can be slow to run so I run many at once, this class is to help manage them
    // tasks can have an action that will be run on the main thread after, since unity needs most calls to be made from the main thread :<
    public class ToolTasker
    {
        private int                       MaxNumWorkers;
        private ConcurrentQueue<ToolTask> TaskQueue       = new();
        private ConcurrentQueue<ToolTask> MainThreadQueue = new();
        public  int                       ActiveTasksCount { get; private set; }

        public ToolTasker(int maxNumWorkers = 12)
        {
            MaxNumWorkers = maxNumWorkers;
        }

        public void Queue(ToolTask task)
        {
            TaskQueue.Enqueue(task);
            RunNextTask();
        }

        // Call this on tick on the main thread
        // Will check for work to be done there
        public void MainThreadTick()
        {
            while (!MainThreadQueue.IsEmpty) {
                if (MainThreadQueue.TryDequeue(out var toolTask)) {
                    try {
                        toolTask.MainThreadTask(toolTask.State);
                    }
                    catch (Exception) { }
                }
            }
        }
        
        private void RunNextTask()
        {
            if (ActiveTasksCount < MaxNumWorkers) {
                if (TaskQueue.TryDequeue(out var toolTask)) {
                    ActiveTasksCount++;
                    var task = Task.Factory.StartNew(() =>
                    {
                        try {
                            toolTask.State = toolTask.Task();
                            if (toolTask.MainThreadTask != null)
                                MainThreadQueue.Enqueue(toolTask);
                        }
                        catch (Exception) {}
                        
                        ActiveTasksCount--;
                        System.Diagnostics.Debug.WriteLine("Ran task");
                        RunNextTask();
                    });
                }
            }
        }

        public class ToolTask
        {
            public object         State;
            public Func<object>   Task;
            public Action<object> MainThreadTask;

            public ToolTask(Action task, Action mainThreadTask = null)
            {
                Task = () =>
                {
                    task();
                    return default;
                };
                MainThreadTask = (state) => mainThreadTask?.Invoke();
            }

            public ToolTask(Func<object> task, Action<object> mainThreadTask = null)
            {
                Task           = task;
                MainThreadTask = mainThreadTask;
            }
        }
    }
}