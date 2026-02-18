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
        /// <typeparam name="TEvent">The event type to check for subscribers.</typeparam>
        /// <param name="bus">The event bus instance.</param>
        /// <returns><see langword="true"/> if there are subscribers for the event type; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bus"/> is <see langword="null"/>.</exception>
        public static bool HasSubscribers<TEvent>(this EventBus bus)
            where TEvent : EventBase
        {
            ArgumentNullException.ThrowIfNull(bus);

            return bus.GetSubscriberCount<TEvent>() > 0;
        }

        /// <summary>
        /// Publishes the event only when there are subscribers for its type.
        /// </summary>
        /// <typeparam name="TEvent">The event type to publish.</typeparam>
        /// <param name="bus">The event bus instance.</param>
        /// <param name="event">The event to publish.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bus"/> is <see langword="null"/>.
        /// <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static async Task PublishIfHasSubscribersAsync<TEvent>(this EventBus bus, TEvent @event)
            where TEvent : EventBase
        {
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(@event);

            if (bus.HasSubscribers<TEvent>())
            {
                await bus.PublishAsync(@event).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the complete event history as a JSON string.
        /// </summary>
        /// <param name="bus">The event bus instance.</param>
        /// <returns>A JSON string representation of the event history.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bus"/> is <see langword="null"/>.</exception>
        public static string GetEventHistoryJson(this EventBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            // The EventBus exposes a List<EventRecord> via GetEventHistory.
            // Serializing it with the default options provides a quick snapshot.
            return JsonSerializer.Serialize(bus.GetEventHistory());
        }

        /// <summary>
        /// Clears all subscribers and, if the implementation supports it, the event history.
        /// The EventBus does not expose a direct method to clear history, so this method
        /// only clears the subscriber collection. It exists as a convenient single call.
        /// </summary>
        /// <param name="bus">The event bus instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bus"/> is <see langword="null"/>.</exception>
        public static void Reset(this EventBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus);

            bus.ClearSubscribers();
            // If a future version adds a ClearHistory method, it can be called here.
        }
    }
}