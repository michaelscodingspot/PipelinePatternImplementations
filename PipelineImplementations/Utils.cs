using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations
{
    public class Utils
    {
        public static int GetThreadPoolThreadsInUse()
        {
            int max, max2;
            ThreadPool.GetMaxThreads(out max, out max2);
            int available, available2;
            ThreadPool.GetAvailableThreads(out available, out available2);
            int running = max - available;
            return running;
        }

}
}
