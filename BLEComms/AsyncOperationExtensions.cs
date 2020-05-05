﻿using System.Threading.Tasks;
using Windows.Foundation;

namespace BLE
{
    /// <summary>
    /// Provides helper methods for the <see cref="IAsyncOperation{TResult}"/>
    /// </summary>
    public static class AsyncOperationExtensions
    {
        /// <summary>
        /// Convert an <see cref="IAsyncOperation{TResult}"/>
        /// into a <see cref="Task{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of result expected</typeparam>
        /// <param name="operation">The Async operation</param>
        /// <returns></returns>
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> operation)
        {
            // Create task completion result
            var tcs = new TaskCompletionSource<TResult>();

            // When operation is completed
            operation.Completed += delegate
            {
                switch(operation.Status)
                {
                    // If successful...
                    case AsyncStatus.Completed:
                        // Set result
                        tcs.TrySetResult(operation.GetResults());
                        break;
                    // If exception...
                    case AsyncStatus.Error:
                        // Set exception
                        tcs.TrySetException(operation.ErrorCode);
                        break;
                    // If canceled...
                    case AsyncStatus.Canceled:
                        // Set task as canceled
                        tcs.SetCanceled();
                        break;
                }
            };

            // Return the task
            return tcs.Task;
        }
    }
}
