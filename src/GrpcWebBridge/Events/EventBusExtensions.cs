using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace GrpcWebBridge.Events
{
    /// <summary>
    /// Extension methods that add convenient helpers to <see cref="EventBus"/>.
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// Determines whether there is at least one subscriber for the specified event type.
        /// </summary>
        public static bool HasSubscribers<TEvent>(this EventBus bus)
            where TEvent : EventBase
        {
            return bus.GetSubscriberCount<TEvent>() > 0;
        }

        /// <summary>
        /// Publishes the event only when there are subscribers for its type.
        /// </summary>
        public static async Task PublishIfHasSubscribersAsync<TEvent>(this EventBus bus, TEvent @event)
            where TEvent : EventBase
        {
            if (bus.HasSubscribers<TEvent>())
            {
                await bus.PublishAsync(@event).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the complete event history as a JSON string.
        /// </summary>
        public static string GetEventHistoryJson(this EventBus bus)
        {
            // The EventBus exposes a List<EventRecord> via GetEventHistory.
            // Serializing it with the default options provides a quick snapshot.
            return JsonSerializer.Serialize(bus.GetEventHistory);
        }

        /// <summary>
        /// Clears all subscribers and, if the implementation supports it, the event history.
        /// The EventBus does not expose a direct method to clear history, so this method
        /// only clears the subscriber collection. It exists as a convenient single call.
        /// </summary>
        public static void Reset(this EventBus bus)
        {
            bus.ClearSubscribers();
            // If a future version adds a ClearHistory method, it can be called here.
        }
    }
}
