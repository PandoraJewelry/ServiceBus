// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License in the project root for license information.

using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Pandora.ServiceBus
{
    public static class BrokeredMessageExtensions
    {
        #region fields
        private static TraceSource _trace = new TraceSource(Consts.TraceName, SourceLevels.Error);
        #endregion

        #region auto renew
        public static Action AutoRenew(this BrokeredMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

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
                        _trace.TraceEvent(TraceEventType.Verbose, 6, string.Format("AutoRenew message ({0}) lock - re-acquired", id));
                    }
                    catch (TaskCanceledException) { }
                    if (token.IsCancellationRequested)
                        break;
                }
            }, token.Token);

            return () =>
            {
                token.Cancel();
                _trace.TraceEvent(TraceEventType.Verbose, 7, string.Format("AutoRenew message ({0}) lock - stoped", id));
                task.Wait();
            };
        }
        #endregion

        #region deseralize
        [Obsolete]
        public static T Deserialize<T>(this BrokeredMessage message)
        {
            var tmp = message.DeserializeAsync<T>();
            tmp.Wait();
            return tmp.Result;
        }
        public static async Task<T> DeserializeAsync<T>(this BrokeredMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if ((message.ContentType == Consts.JsonContentType) || (message.ContentType == Consts.PlainTextType))
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
        public static async Task<BrokeredMessage> CreateMessageAsync<T>(this T item, bool propertyConversion = false, bool serializeAsJson = true)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var msg = serializeAsJson ?
                await CreateJsonMessageAsync(item) :
                CreateBinaryMessageAsync(item);

            if (propertyConversion)
                PropertyConversion(msg, item);

            return msg;
        }
        internal static async Task<BrokeredMessage> CreateJsonMessageAsync<T>(this T item)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(item));
                await writer.FlushAsync();
                stream.Position = 0;

                return new BrokeredMessage(stream, true) { ContentType = Consts.JsonContentType };
            }
        }
        internal static BrokeredMessage CreateBinaryMessageAsync<T>(this T item)
        {
            var serializer = new DataContractSerializer(typeof(T));

            var stream = new MemoryStream();
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
            {
                serializer.WriteObject(writer, item);
                writer.Flush();
                stream.Position = 0;

                return new BrokeredMessage(stream, true);
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
