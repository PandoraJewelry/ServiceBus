// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License in the project root for license information.

using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pandora.ServiceBus
{
    public static class ProcessExtensions
    {
        #region fields
        private static TraceSource _trace = new TraceSource(Consts.TraceName, SourceLevels.Error);
        #endregion

        #region process async
        public static async Task<bool> ProcessRequestAsync<T>(this BrokeredMessage message, Func<T, Task<bool>> callback)
        {
            return await message.ProcessRequestAsync(callback, _ => Task.FromResult(true));
        }
        public static async Task<TResult> ProcessRequestAsync<T, TResult>(this BrokeredMessage message, Func<T, Task<TResult>> callback) where TResult : class
        {
            return await message.ProcessRequestAsync(callback, p => Task.FromResult(p != null));
        }
        internal static async Task<TResult> ProcessRequestAsync<T, TResult>(this BrokeredMessage message, Func<T, Task<TResult>> callback, Func<TResult, Task<bool>> successdetect)
        {
            Action done = null;

            try
            {
                done = message.AutoRenew();

                _trace.TraceEvent(TraceEventType.Verbose, 1, string.Format("Start of Deseralise ({0}) processing.", message.MessageId));
                var t = await message.DeserializeAsync<T>();
                _trace.TraceEvent(TraceEventType.Verbose, 2, "End of Deseralise processing.");

                _trace.TraceEvent(TraceEventType.Verbose, 3, "Start of message processing.");
                var result = await callback(t);
                var success = await successdetect(result);
                _trace.TraceEvent(TraceEventType.Verbose, 4, string.Format("End of message processing. {0}", success ? "Success" : "Errors Detected"));

                if (!success)
                    throw new ApplicationException("Process did not complete successfully");

                return result;
            }
            catch (Exception ex)
            {
                /// log errors
                _trace.TraceEvent(TraceEventType.Error, 1, string.Format("ProcessRequest ({0}) - {1}", message.MessageId, ex));
                throw;
            }
            finally
            {
                done?.Invoke();
            }
        }
        #endregion
    }
}
