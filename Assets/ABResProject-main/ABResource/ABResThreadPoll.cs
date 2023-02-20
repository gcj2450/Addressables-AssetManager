using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace Asgard
{
    public class ABResThreadPoll
    {
        private Queue<AsyncTask> queue;
        private List<AsyncTask> tasks;
        private List<AsyncTask> finishTasks;
        private List<AsyncTask> errorTasks;

        private Thread thread;
        private bool ifRun = true;
        private bool ifPause = false;

        private static ABResThreadPoll instance;

        public static ABResThreadPoll Instance 
        {
            get 
            {
                if (instance == null) instance = new ABResThreadPoll();
                return instance; 
            }
        }

        public void InitData()
        {

        }

        public void InitSys()
        {
            ifRun = true;
            ifPause = false;
            queue = new Queue<AsyncTask>();
            tasks = new List<AsyncTask>();
            finishTasks = new List<AsyncTask>();
            errorTasks = new List<AsyncTask>();
            Thread thread = new Thread(ThreadAction);
            thread.Start();
        }

        public void AddTask(AsyncTask task)
        {
            queue.Enqueue(task);
        }

        public void ThreadAction()
        {
            while (ifRun)
            {
                if (!ifPause)
                {
                    if (queue.Count > 0)
                    {
                        AsyncTask task = queue.Dequeue();
                        task.Run();
                        tasks.Add(task);
                    }
                }
            }
        }

        public void DisposeData()
        {
            ifPause = true;
            ifRun = false;
            tasks.Clear();
            finishTasks.Clear();
            errorTasks.Clear();
            tasks = null;
            finishTasks = null;
            errorTasks = null;
        }

        public void DoFrameUpdate(int time, int delta)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].isDone)
                {
                    if (string.IsNullOrEmpty(tasks[i].errorStr))
                    {
                        finishTasks.Add(tasks[i]);
                    }
                    else
                    {
                        errorTasks.Add(tasks[i]);
                    }
                }
            }

            for (int j = 0; j < errorTasks.Count; j++)
            {
                AsyncTask t = errorTasks[j];
                if (tasks.Contains(t))
                    tasks.Remove(t);
                AddTask(t);
            }

            errorTasks.Clear();

            for (int k = 0; k < finishTasks.Count; k++)
            {
                AsyncTask t = finishTasks[k];
                if (tasks.Contains(t))
                    tasks.Remove(t);
                t.RunCallBack();
            }

            finishTasks.Clear();
        }

        public void DoFixedUpdate()
        {

        }

        public class AsyncTask
        {
            private System.Func<object> RunAction;
            private System.Action<object> CallBack;
            private object result;
            public bool isDone = false;
            public string errorStr = "";

            public AsyncTask(System.Func<object> RunAction, System.Action<object> CallBack)
            {
                this.RunAction = RunAction;
                this.CallBack = CallBack;
            }

            public void Init()
            {
                this.isDone = false;
                this.errorStr = "";
            }

            public void Run()
            {
                Init();
                try
                {
                    if (RunAction != null)
                    {
                        result = RunAction();
                    }
                }
                catch (Exception e)
                {
                    errorStr = e.ToString();
                    Debug.LogError(e.ToString());
                }

                this.isDone = true;
            }

            public void RunCallBack()
            {
                if (this.CallBack != null)
                    this.CallBack(this.result);
            }
        }
    }
}

