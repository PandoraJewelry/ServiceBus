// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License in the project root for license information.

using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pandora.ServiceBus
{
    public static class SubscriptionClientExtensions
    {
        #region fields
        private static TraceSource _trace = new TraceSource(Consts.TraceName, SourceLevels.Error); 
        #endregion

        public static async Task<SubscriptionClient> DrainAsync(this SubscriptionClient client, TimeSpan ttl)
        {
            _trace.TraceEvent(TraceEventType.Information, 4, "Draining subscription [{0}/{1}] - starting", client.TopicPath, client.Name);

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

            _trace.TraceEvent(TraceEventType.Information, 4, "Draining subscription - done");

            return client;
        }
    }
}
