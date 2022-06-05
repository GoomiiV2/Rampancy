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
        public  Action                    OnBatchStart;
        public  Action<int, TimeSpan>               OnBatchFinished; // When a batch of tasks have been completed and nothing else has happened for 2 seconds
        public  Action<int, int, string>  OnTaskComplete;

        private int               TotalBatchTasksCount;
        private int               ColmepetedTasksCount;
        private bool              IsBatchStarted = false;
        private DateTime          BatchStartTime;
        private PostponableAction BatchFinished;

        public ToolTasker(int maxNumWorkers = 12)
        {
            MaxNumWorkers = maxNumWorkers;
            BatchFinished = new PostponableAction(TimeSpan.FromSeconds(2), OnBatchFinishedInternal);
        }

        public void Queue(ToolTask task)
        {
            if (TaskQueue.IsEmpty && !IsBatchStarted) {
                IsBatchStarted = true;
                OnBatchStart?.Invoke();
                BatchStartTime = DateTime.Now;
            }

            TaskQueue.Enqueue(task);
            TotalBatchTasksCount++;
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
                        OnTaskCompleteInternal();
                    }
                    catch (Exception ex) {
                        OnTaskCompleteInternal(ex.ToString());
                    }
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
                            if (toolTask.MainThreadTask != null) {
                                MainThreadQueue.Enqueue(toolTask);
                            }
                            else {
                                OnTaskCompleteInternal();
                            }
                        }
                        catch (Exception ex) {
                            OnTaskCompleteInternal(ex.ToString());
                        }

                        ActiveTasksCount--;
                        System.Diagnostics.Debug.WriteLine("Ran task");
                        RunNextTask();
                    });
                }
            }
        }

        private void OnTaskCompleteInternal(string error = null)
        {
            ColmepetedTasksCount++;
            OnTaskComplete.Invoke(ColmepetedTasksCount, TotalBatchTasksCount, error);
            BatchFinished.Invoke();
        }

        private void OnBatchFinishedInternal()
        {
            if (ColmepetedTasksCount == TotalBatchTasksCount) {
                var batchDuration = DateTime.Now - BatchStartTime;
                OnBatchFinished?.Invoke(ColmepetedTasksCount, batchDuration);

                TotalBatchTasksCount = 0;
                ColmepetedTasksCount = 0;
                IsBatchStarted       = false;
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