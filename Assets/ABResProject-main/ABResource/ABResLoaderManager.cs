using System;
using System.Collections.Generic;
using Asgard.Resource;
using UnityEngine;

namespace Asgard
{
    public class ABResLoaderManager
    {

        private static ABResLoaderManager instance = null;
        public static ABResLoaderManager Instance 
        {
            get 
            {
                if (ABResLoaderManager.instance == null) 
                {
                    instance = new ABResLoaderManager();
                }

                return instance;
            }
        }
        public void InitSys()
        {

            downLoadTasks = new List<MainDownLoadTask>();
            completeTasks = new List<MainDownLoadTask>();
        }

        private List<MainDownLoadTask> downLoadTasks;
        private List<MainDownLoadTask> completeTasks;

        public void LoadResource(BaseResource resource, Action<BaseResource> allFinishAction, Action<BaseResource> itemFinishAction = null, bool ifDownLoadTask = false)
        {
            DownLoadTask task = new DownLoadTask(resource, itemFinishAction, ifDownLoadTask);
            MainDownLoadTask mainTask = new MainDownLoadTask(task, allFinishAction);
            downLoadTasks.Add(mainTask);
        }

        public void LoadResources(List<BaseResource> resources, Action allFinishAction, Action<BaseResource> itemFinishAction = null, bool ifDownLoadTask = false)
        {
            List<DownLoadTask> tasks = new List<DownLoadTask>();
            for (int i = 0; i < resources.Count; i++)
            {
                DownLoadTask task = new DownLoadTask(resources[i], itemFinishAction, ifDownLoadTask);
                tasks.Add(task);
            }
            MainDownLoadTask mainTask = new MainDownLoadTask(tasks, allFinishAction);
            downLoadTasks.Add(mainTask);
        }

        public void LoadResources(List<BaseResource> resources, Action<List<BaseResource>> allFinishAction, Action<BaseResource> itemFinishAction = null, bool ifDownLoadTask = false)
        {
            List<DownLoadTask> tasks = new List<DownLoadTask>();
            for (int i = 0; i < resources.Count; i++)
            {
                DownLoadTask task = new DownLoadTask(resources[i], itemFinishAction, ifDownLoadTask);
                tasks.Add(task);
            }
            MainDownLoadTask mainTask = new MainDownLoadTask(tasks, allFinishAction);
            downLoadTasks.Add(mainTask);
        }

        public void InitData()
        {
        }

        public void DisposeData()
        {
            ABResDownLoadResourcePool.Instance.DisposeData();
            for (int i = 0; i < downLoadTasks.Count; i++)
            {
                downLoadTasks[i].DisposeData();
            }
            downLoadTasks.Clear();
            downLoadTasks = null;
            completeTasks.Clear();
            completeTasks = null;
        }

        public void DoFrameUpdate(int time, int delta)
        {
            ABResDownLoadResourcePool.Instance.DoFrameUpdate(time, delta);
            for (int i = 0; i < downLoadTasks.Count; i++)
            {
                MainDownLoadTask mainTask = downLoadTasks[i];
                mainTask.DoFrameUpdate(time, delta);
                if (mainTask.ifComplete)
                {
                    completeTasks.Add(mainTask);
                }
            }

            for (int j = 0; j < completeTasks.Count; j++)
            {
                MainDownLoadTask mainTask = completeTasks[j];
                mainTask.StartAllFinish();
                mainTask.DisposeData();
                downLoadTasks.Remove(mainTask);
            }

            completeTasks.Clear();
        }

        public void DoFixedUpdate()
        {
        }


        private class MainDownLoadTask
        {
            private List<DownLoadTask> downLoadTasks = new List<DownLoadTask>();

            private System.Action allFinishAction = null;

            private List<BaseResource> resources
            {
                get
                {
                    List<BaseResource> result = new List<BaseResource>();
                    for (int i = 0; i < downLoadTasks.Count; i++)
                    {
                        result.Add(downLoadTasks[i].resource);
                    }
                    return result;
                }
            }

            public void StartAllFinish()
            {
                if (allFinishAction != null)
                {
                    try
                    {
                        allFinishAction();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
                }
            }

            public MainDownLoadTask(DownLoadTask downLoadTask, System.Action<BaseResource> allFinishAction)
            {
                this.downLoadTasks.Add(downLoadTask);
                this.allFinishAction = () =>
                {
                    if (allFinishAction != null)
                        allFinishAction(this.resources[0]);
                };
            }

            public MainDownLoadTask(List<DownLoadTask> downLoadTasks, System.Action<List<BaseResource>> allFinishAction)
            {
                this.downLoadTasks = downLoadTasks;
                this.allFinishAction = () =>
                {
                    if (allFinishAction != null)
                        allFinishAction(this.resources);
                };
            }

            public MainDownLoadTask(List<DownLoadTask> downLoadTasks, System.Action allFinishAction)
            {
                this.downLoadTasks = downLoadTasks;
                this.allFinishAction = () =>
                {
                    if (allFinishAction != null)
                        allFinishAction();
                };
            }

            public void DoFrameUpdate(int time, int delta)
            {
                for (int i = 0; i < downLoadTasks.Count; i++)
                {
                    downLoadTasks[i].DoFrameUpdate(time, delta);
                }
            }

            public bool ifComplete
            {
                get
                {
                    for (int i = 0; i < downLoadTasks.Count; i++)
                    {
                        DownLoadTask task = downLoadTasks[i];
                        if (task.state == DownLoadTask.DownLoadTaskState.Create || task.state == DownLoadTask.DownLoadTaskState.Downing)
                        {
                            return false;
                        }
                    }
                    return true;
                }

            }

            public void DisposeData()
            {
                foreach (DownLoadTask task in downLoadTasks)
                {
                    task.Dispose();
                }
                downLoadTasks.Clear();
            }
        }

        private class DownLoadTask
        {
            private bool ifDownLoadTask = false;
            internal BaseResource resource;
            private System.Action<BaseResource> itemFinishAction = null;

            internal enum DownLoadTaskState
            {
                Create,
                Downing,
                Finish,
                Error,
            }

            internal DownLoadTaskState state;

            internal void DoFrameUpdate(int time, int delta)
            {
                if (this.state == DownLoadTaskState.Finish || this.state == DownLoadTaskState.Error)
                    return;
                if (this.resource.ifError)
                {
                    //出现资源下载错误 需要处理 //这里也许要添加 error回调 先强制执行完成回调
                    Debug.LogError("下载任务出现error状态资源 强制执行回调!");
                    StartItemFinishAction();
                    this.state = DownLoadTaskState.Error;
                }
                else
                {
                    if (!this.resource.ifCreate)
                        return;
                    if (!ifDownLoadTask)
                        this.resource.Load();
                    StartItemFinishAction();
                    this.state = DownLoadTaskState.Finish;
                }
            }

            private void StartItemFinishAction()
            {
                if (this.itemFinishAction != null)
                {
                    try
                    {
                        this.itemFinishAction(this.resource);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }

                }
                this.resource.ReduceReferenceCount();
            }

            internal DownLoadTask(BaseResource resource, System.Action<BaseResource> itemFinishAction, bool ifDownLoadTask = false)
            {
                this.ifDownLoadTask = ifDownLoadTask;
                this.resource = resource;
                this.itemFinishAction = itemFinishAction;
                this.state = DownLoadTaskState.Create;
                ABResDownLoadResourcePool.Instance.AddDownLoadResource(this.resource);
                this.state = DownLoadTaskState.Downing;
                this.resource.AddReferenceCount();
            }

            internal void Dispose()
            {
                this.resource = null;
                this.itemFinishAction = null;
                this.state = DownLoadTaskState.Create;
            }
        }
    }
}

