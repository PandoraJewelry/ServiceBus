using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pandora.ServiceBusExtensions
{
    public static class BrokeredMessageExtensions
    {
        #region fields
        private static TraceSource source = new TraceSource(typeof(BrokeredMessageExtensions).FullName, SourceLevels.Error);
        private const string JsonContentType = "application/json";
        private const string PlainTextType = "text/plain";
        #endregion

        #region process async
        public static async Task<bool> ProcessRequestAsync<T>(this BrokeredMessage message, Func<T, Task<bool>> callback)
        {
            return await message.ProcessRequestAsync(callback, async _ => true);
        }
        public static async Task<TResult> ProcessRequestAsync<T, TResult>(this BrokeredMessage message, Func<T, Task<TResult>> callback) where TResult : class
        {
            return await message.ProcessRequestAsync(callback, async p => p != null);
        }
        internal static async Task<TResult> ProcessRequestAsync<T, TResult>(this BrokeredMessage message, Func<T, Task<TResult>> callback, Func<TResult, Task<bool>> successdetect)
        {
            Action done = null;

            try
            {
                done = message.AutoRenew();

                source.TraceEvent(TraceEventType.Verbose, 1, string.Format("Start of Deseralise ({0}) processing.", message.MessageId));
                var t = await message.DeseraliseAsync<T>();
                source.TraceEvent(TraceEventType.Verbose, 2, "End of Deseralise processing.");

                source.TraceEvent(TraceEventType.Verbose, 3, "Start of message processing.");
                var result = await callback(t);
                var success = await successdetect(result);
                source.TraceEvent(TraceEventType.Verbose, 4, string.Format("End of message processing. {0}", success ? "Success" : "Errors Detected"));

                if (!success)
                    throw new ApplicationException("Process did not complete successfully");

                return result;
            }
            catch (Exception ex)
            {
                /// log errors
                source.TraceEvent(TraceEventType.Error, 1, string.Format("ProcessRequest ({0}) - {1}", message.MessageId, ex));
                throw;
            }
            finally
            {
                if (done != null)
                    done();
            }
        }
        #endregion

        #region auto renew
        public static Action AutoRenew(this BrokeredMessage message)
        {
            var halflife = (int)((message.LockedUntilUtc - DateTime.Now.ToUniversalTime()).TotalMilliseconds / 2);
            var token = new CancellationTokenSource();
            var id = message.MessageId;

            var task = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(halflife, token.Token);
                        await message.RenewLockAsync();
                        source.TraceEvent(TraceEventType.Verbose, 6, string.Format("AutoRenew message ({0}) lock - re-acquired", id));
                    }
                    catch (TaskCanceledException) { }
                    if (token.IsCancellationRequested)
                        break;
                }
            }, token.Token);

            return () =>
            {
                token.Cancel();
                source.TraceEvent(TraceEventType.Verbose, 7, string.Format("AutoRenew message ({0}) lock - stoped", id));
                task.Wait();
            };
        }
        #endregion

        #region deseralize
        [Obsolete]
        public static T Deseralisec<T>(this BrokeredMessage message)
        {
            var tmp = message.DeseraliseAsync<T>();
            tmp.Wait();
            return tmp.Result;
        }
        public static async Task<T> DeseraliseAsync<T>(this BrokeredMessage message)
        {
            if ((message.ContentType == JsonContentType) || (message.ContentType == PlainTextType))
            {
                using (var stream = message.GetBody<Stream>())
                using (var reader = new StreamReader(stream))
                {
                    var body = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<T>(body);
                }
            }
            else
                return message.GetBody<T>();
        }
        #endregion

        #region CreateMessage
        [Obsolete]
        public static BrokeredMessage CreateMessage<T>(this T item)
        {
            var tmp = item.CreateMessageAsync();
            tmp.Wait();
            return tmp.Result;
        }
        public static async Task<BrokeredMessage> CreateMessageAsync<T>(this T item, bool propertyConversion = false)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(item));
                await writer.FlushAsync();
                stream.Position = 0;

                var msg = new BrokeredMessage(stream, true) { ContentType = JsonContentType };

                if (propertyConversion)
                    PropertyConversion(msg, item);

                return msg;
            }
        }
        private static void PropertyConversion<T>(BrokeredMessage message, T item)
        {
            var type = typeof(T);

            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(item);

                if (value != null)
                    message.Properties[type.Name + prop.Name] = value.ToString();
            }
        }
        #endregion
    }
}
