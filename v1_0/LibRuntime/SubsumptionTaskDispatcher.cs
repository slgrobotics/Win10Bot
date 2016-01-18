/*
 * Copyright (c) 2016..., Sergei Grichine   http://trackroamer.com
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *    
 * this is a no-warranty no-liability permissive license - you do not have to publish your changes,
 * although doing so, donating and contributing is always appreciated
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace slg.LibRuntime
{
    /*
    See https://en.wikipedia.org/wiki/Subsumption_architecture
    Here is an interesting short article about the deficiency of subsumption architecture
       and the role of memorization in AI: http://adamilab.msu.edu/intelligence-with-representations/
    For those brave and furious - http://arxiv.org/pdf/1206.5771 - “The Evolution of Representation in Simple Cognitive Networks” full article PDF 3MB.
    */
    public class SubsumptionTaskDispatcher
    {
        /// <summary>
        /// list of tasks
        /// </summary>
        private List<ISubsumptionTask> tasks = new List<ISubsumptionTask>();

        public List<ISubsumptionTask> Tasks { get { return tasks; } }

        /// <summary>
        /// list of active tasks states
        /// </summary>
        private List<IteratorState> iStates = new List<IteratorState>();

        public int ActiveTasksCount { get { return iStates.Count; } }

        /// <summary>
        /// adds a task to the list of active tasks
        /// </summary>
        /// <param name="task"></param>
        public void Dispatch(ISubsumptionTask task)
        {
            Debug.WriteLine("TaskDispatcher: Dispatch: " + tasks.Count + " tasks, adding: " + task.name);

            tasks.Add(task);

            //lock(iStates)
            {
                iStates.Add(new IteratorState() { taskName = task.name, iterator = task.Execute() });
            }
        }

        /// <summary>
        /// calls every task in the list and removes all finished tasks
        /// </summary>
        /// <returns>true if at least one of the tasks has things to do</returns>
        public bool Process()
        {
            bool ret = false;

            foreach (var ist in iStates)
            {
                ret |= Process(ist);
            }

            //lock (iStates)
            {
                foreach (var a in iStates.Where(a => a.iterator == null))
                {
                    Debug.WriteLine("TaskDispatcher: Dispatch: task '" + a.taskName + "' finished and will be removed");
                }

                // cleanup tasks which have finished:
                iStates.RemoveAll(a => a.iterator == null);
            }

            //if (ActiveTasksCount == 0)
            //{
            //    Debug.WriteLine("Warning: TaskDispatcher: Dispatch: active tasks count zero");
            //}

            return ret;
        }

        /// <summary>
        /// nicely finish processing, allowing all tasks to close
        /// </summary>
        public void Close()
        {
            Debug.WriteLine("TaskDispatcher: Close: " + tasks.Count + " tasks");

            foreach (var task in tasks)
            {
                task.Close();
            }
            tasks.Clear();
            iStates.Clear();
        }

        /// <summary>
        /// propagates device commands to task events
        /// </summary>
        /// <param name="command"></param>
        public void ControlDeviceCommand(string command)
        {
            //Debug.WriteLine("TaskDispatcher: ControlDeviceCommand: " + command);

            ControlDeviceEventArgs args = new ControlDeviceEventArgs() { command = command };

            foreach (var task in tasks)
            {
                task.OnControlDeviceCommand(args);
            }
        }

        /// <summary>
        /// calls iterator for drill down and manages its return path via a stack
        /// </summary>
        /// <param name="istate"></param>
        /// <returns></returns>
        private bool Process(IteratorState istate)
        {
            // stack and one-way (drill down) recursion:

            IEnumerator<ISubsumptionTask> iterator2 = this.iterate(istate.iterator);

            if (iterator2 == null)
            {
                istate.iterator = istate.stack.Count > 0 ? istate.stack.Pop() : null;
            }
            else if (iterator2 != istate.iterator)
            {
                istate.stack.Push(istate.iterator);
                istate.iterator = iterator2;
            }

            return istate.iterator != null;
        }


        /// <summary>
        /// one-way (drill down) recursion, no wait allowed
        /// </summary>
        /// <param name="iter">Task iterator</param>
        /// <returns>null when yield break or end of method is encountered</returns>
        private IEnumerator<ISubsumptionTask> iterate(IEnumerator<ISubsumptionTask> iter)
        {
            //Debug.WriteLine("TaskDispatcher: iterate()    type: " + iter.GetType());

            if (iter != null && iter.MoveNext())
            {
                ISubsumptionTask task = iter.Current;

                //Debug.WriteLine("...iterating...   task: " + task);

                bool doContinie = task == RobotTask.Continue;

                if (doContinie)
                {
                    //Debug.WriteLine("TaskDispatcher: iterate() - task: " + task + "     Task.Continue - doing nothing");

                    return iter;
                }

                iter = task.Execute();

                //Debug.WriteLine("TaskDispatcher: iterate() - task: " + task + " - iterating inside inner loop");

                return iterate(iter);
            }

            //Debug.WriteLine("TaskDispatcher: iterate() finished task");

            return null;
        }
    }
}
