using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Rampancy
{
    // A task that will be postproned each time Invoke is called
    // Useful to save changes to a object after a period of no more changes
    public class PostponableAction
    {
        private Action   Act;
        private TimeSpan Delay;
        private DateTime LastInvoke = DateTime.MinValue;
        private Task     ActionTask;

        public PostponableAction(TimeSpan delay, Action act)
        {
            Delay = delay;
            Act   = act;
        }

        public void Invoke()
        {
            LastInvoke = DateTime.Now;
            if (ActionTask == null) {
                ActionTask = Task.Factory.StartNew(PerformAction);
            }
        }

        private async void PerformAction()
        {
            while (LastInvoke + Delay > DateTime.Now) {
                await Task.Delay(Delay);
            }
            
            Act?.Invoke();
            ActionTask = null;
        }
    }
}