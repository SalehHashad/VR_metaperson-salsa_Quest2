using System;
using System.Collections;
using System.Threading.Tasks;

public static class TaskExtensions
{
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            throw task.Exception ?? new Exception("Task failed with no exception details.");
        }
    }
}
