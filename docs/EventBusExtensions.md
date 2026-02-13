# EventBusExtensions

`EventBusExtensions` provides utility methods for interacting with an event bus system in the `grpc-web-bridge` project. These extensions enable checking for subscribers, conditionally publishing events, retrieving event history, and resetting the event bus state. The methods are designed to work with a generic event bus infrastructure, offering thread-safe operations where applicable.

## API

### `HasSubscribers<TEvent>`
