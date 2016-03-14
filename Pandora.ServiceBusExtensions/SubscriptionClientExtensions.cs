using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pandora.ServiceBusExtensions
{
    public static class SubscriptionClientExtensions
    {
        private static TraceSource source = new TraceSource(typeof(SubscriptionClientExtensions).FullName, SourceLevels.Error);

        public static async Task<SubscriptionClient> DrainAsync(this SubscriptionClient client, TimeSpan ttl)
        {
            source.TraceEvent(TraceEventType.Information, 4, "Draining subscription [{0}/{1}] - starting", client.TopicPath, client.Name);

            bool hadmessages = false;

            do
            {
                hadmessages = false;
                var msgs = await client.ReceiveBatchAsync(10, ttl);

                if (msgs != null)
                    foreach (var msg in msgs)
                    {
                        hadmessages = true;
                        msg.Complete();
                    }

            } while (hadmessages);

            source.TraceEvent(TraceEventType.Information, 4, "Draining subscription - done");

            return client;
        }
    }
}
