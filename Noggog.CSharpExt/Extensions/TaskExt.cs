#if NETSTANDARD2_0
#else 
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource;
#endif

namespace Noggog
{
    public static class TaskExt
    {
        public static async Task<T?> AwaitOrDefault<T>(Task<T> t)
            where T : class
        {
            if (t == null) return default;
            return await t.ConfigureAwait(false);
        }

        public static async Task<T?> AwaitOrDefaultValue<T>(Task<TryGet<T>> t)
            where T : class
        {
            if (t == null) return default;
            return (await t.ConfigureAwait(false)).Value;
        }

        public static Task<bool> Timeout(this Task task, int? timeoutMS, bool throwIfTimeout = false)
        {
            return Timeout(task, timeoutMS, "Task", throwIfTimeout);
        }

        public static async Task<bool> Timeout(this Task task, int? timeoutMS, string taskMessage, bool throwIfTimeout = false)
        {
            if (!timeoutMS.HasValue)
            {
                await task.ConfigureAwait(false);
                return false;
            }
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutMS.Value)).ConfigureAwait(false);
            if (retTask == task)
            {
                await task.ConfigureAwait(false);
                return false;
            }
            if (throwIfTimeout) throw new TimeoutException($"{taskMessage} took longer than {timeoutMS.Value}ms.");
            return true;
        }

        public static Task<TryGet<T>> TryTimeout<T>(this Task<T> task, int? timeoutMS, bool throwIfTimeout = false)
        {
            return TryTimeout(task, timeoutMS, "Task", throwIfTimeout);
        }

        public static async Task<TryGet<T>> TryTimeout<T>(this Task<T> task, int? timeoutMS, string taskMessage, bool throwIfTimeout = false)
        {
            if (!timeoutMS.HasValue)
            {
                return TryGet<T>.Fail(await task.ConfigureAwait(false));
            }
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutMS.Value)).ConfigureAwait(false);
            if (retTask == task)
            {
                return TryGet<T>.Fail(await task.ConfigureAwait(false));
            }
            if (throwIfTimeout) throw new TimeoutException($"{taskMessage} took longer than {timeoutMS.Value}ms.");
            return TryGet<T>.Create(successful: true);
        }

        public static Task<T> Timeout<T>(this Task<T> task, int? timeoutMS)
        {
            return Timeout(task, timeoutMS, "Task");
        }

        public static async Task<T> Timeout<T>(this Task<T> task, int? timeoutMS, string taskMessage)
        {
            if (!timeoutMS.HasValue)
            {
                return await task.ConfigureAwait(false);
            }
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutMS.Value)).ConfigureAwait(false);
            if (retTask == task)
            {
                return await task.ConfigureAwait(false);
            }
            throw new TimeoutException($"{taskMessage} took longer than {timeoutMS.Value}ms.");
        }

        public static async Task<bool> TimeoutButContinue(this Task task, int? timeoutMS, Action timeout)
        {
            if (!timeoutMS.HasValue)
            {
                await task.ConfigureAwait(false);
                return false;
            }
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutMS.Value)).ConfigureAwait(false);
            if (retTask == task)
            {
                await task.ConfigureAwait(false);
                return false;
            }
            timeout();
            await task.ConfigureAwait(false);
            return true;
        }

        public static async Task<T> TimeoutButContinue<T>(this Task<T> task, int? timeoutMS, Action timeout)
        {
            if (!timeoutMS.HasValue)
            {
                return await task.ConfigureAwait(false);
            }
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutMS.Value)).ConfigureAwait(false);
            if (retTask == task)
            {
                return await task.ConfigureAwait(false);
            }
            timeout();
            return await task.ConfigureAwait(false);
        }

#if NETSTANDARD2_0
        public static async Task DoThenComplete(TaskCompletionSource tcs, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                tcs?.SetResult();
            }
            catch (Exception ex)
            {
                tcs?.SetException(ex);
                throw;
            }
        }
#else
        public static async Task DoThenComplete(TaskCompletionSource tcs, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                tcs?.SetResult();
            }
            catch (Exception ex)
            {
                tcs?.SetException(ex);
                throw;
            }
        }
#endif

        public static async void FireAndForget(this Task task, Action<Exception>? onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            when (onException != null)
            {
                onException(ex);
            }
        }

        public static async Task Run(bool doThreading, Func<Task> toDo)
        {
            if (doThreading)
            {
                await Task.Run(toDo);
            }
            else
            {
                await toDo();
            }
        }

        public static async Task Run(bool doThreading, Action toDo)
        {
            if (doThreading)
            {
                await Task.Run(toDo);
            }
            else
            {
                toDo();
            }
        }
    }
}
