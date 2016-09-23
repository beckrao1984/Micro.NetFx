using System;
using System.Threading.Tasks;

namespace Micro.NetFx.Threading.Tasks
{
    /// <summary>
    /// for example:
    /// Task.Delay(100).Await(); //await Task.Delay(100);
    /// var client = new HttpClient();
    /// var result = client.GetStringAsync("https://www.baidu.com").Await(); // var result = await client.GetStringAsync("https://www.baidu.com");
    /// </summary>
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
