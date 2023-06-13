/*
    Copyright 2015 SuperNova
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace SuperNova.Modules.Relay2 
{ 
    /// <summary> Asynchronously sends data to an external communication service </summary>
    public abstract class RelayBotSender2<T> 
    {
        AutoResetEvent handle = new AutoResetEvent(false);
        volatile bool terminating;

        public Queue<T> requests = new Queue<T>();
        public readonly object reqLock = new object();

        public abstract void HandleNext();
        /// <summary> Name to assign the worker thread </summary>
        public abstract string ThreadName { get; }
        
        void SendLoop() {
            for (;;) {
                if (terminating) break;
                
                try {
                    HandleNext();
                } catch (Exception ex) {
                    Logger.LogError(ex);
                }
            }
            
            // cleanup state
            try {
                lock (reqLock) requests.Clear();
                handle.Close();
            } catch {
            }
        }      
        
        void WakeupWorker() {
            try {
                handle.Set();
            } catch (ObjectDisposedException) {
                // for very rare case where handle's already been destroyed
            }
        }

        public void WaitForWork() { handle.WaitOne(); }
        
        
        /// <summary> Starts the background worker thread </summary>
        public void RunAsync() {
            Thread worker = new Thread(SendLoop);
            worker.Name   = ThreadName;
            worker.IsBackground = true;
            worker.Start();
        }
        
        public void StopAsync() {
            terminating = true;
            WakeupWorker();
        }
        
        /// <summary> Asynchronously sends data </summary>
        public void SendAsync(T msg) {
            lock (reqLock) requests.Enqueue(msg);
            WakeupWorker();
        }
    }
}
