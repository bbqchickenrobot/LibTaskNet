﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Austin.LibTaskNet
{
    internal abstract class FdTask : IDisposable
    {
        public FdTask()
        {
            this.MyTask = CoopScheduler.Self;
        }

        public InternalTask MyTask { get; private set; }

        public abstract bool IsDone { get; }
        public abstract WaitHandle Event { get; }
        public abstract void Dispose();
    }

    public static partial class FD
    {
        private static bool StartedFdTask = false;
        private static List<FdTask> sleeping = new List<FdTask>();

        private static bool wakeSleeping()
        {
            var done = sleeping.Where(t => t.IsDone).ToList();
            foreach (var t in done)
            {
                sleeping.Remove(t);
                CoopScheduler.Ready(t.MyTask);
                t.Dispose();
            }
            return done.Count != 0;
        }

        private static void FdTask()
        {
            while (true)
            {
                while (CoopScheduler.Yield() > 0)
                    ;

                if (wakeSleeping())
                    continue;

                WaitHandle.WaitAny(sleeping.Select(t => t.Event).ToArray(), 10);

                wakeSleeping();
            }
        }

        private static void StartFdTask()
        {
            if (!StartedFdTask)
            {
                CoopScheduler.Create(FdTask);
                StartedFdTask = true;
            }
        }

        private static void Wait(FdTask t)
        {
            StartFdTask();
            sleeping.Add(t);
            CoopScheduler.State("fd wait");
            CoopScheduler.Switch();
            CoopScheduler.State("fd done");
        }
    }
}
