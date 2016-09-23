using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Micro.NetFx.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static void Await(this Task task)
        {
            if(task==null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            TaskHelpers.RunSync(() => task);
        }

        public static T Await<T>(this Task<T> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            return TaskHelpers.RunSync(() => task);
        }
    }
}
