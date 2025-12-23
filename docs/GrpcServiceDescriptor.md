# GrpcServiceDescriptor

Represents metadata describing a gRPC service exposed via the `grpc-web-bridge` proxy. This descriptor captures service-level and method-level details required to route, validate, and forward gRPC requests between clients and backend services.

## API

### Properties

- **`FullName`** (string)
  The fully qualified name of the service, including the package (e.g., `package.ServiceName`). Used as a unique identifier for routing and discovery.

- **`Name`** (string)
  The short name of the service without the package prefix. Useful for display or logging purposes.

- **`PackageName`** (string)
  The package name under which the service is defined. Determines logical grouping and namespace resolution.

- **`Description`** (string?)
  An optional human-readable description of the service. May be `null` if not provided in the proto definition.

- **`Endpoint`** (string)
  The network endpoint (hostname or IP) where the gRPC service is exposed. May include a port (e.g., `localhost:50051`).

- **`Port`** (int)
  The port number on which the service listens. Derived from `Endpoint`; may be `0` if not specified.

- **`UseTls`** (bool)
  Indicates whether the connection to the backend service should use TLS. When `true`, secure channels are established.

- **`Methods`** (IReadOnlyCollection<MethodDescriptor>)
  A read-only collection of method descriptors describing each exposed gRPC method. Methods are ordered as defined in the proto file.

---

### Nested Type: `MethodDescriptor`

#### Properties

- **`Name`** (string)
  The short name of the gRPC method (e.g., `GetData`). Used in routing and logging.

- **`FullName`** (string)
  The fully qualified method name, including service and package (e.g., `package.ServiceName.GetData`).

- **`ServiceFullName`** (string)
  The fully qualified name of the parent service. Identical across all methods of the same service.

- **`MethodType`** (string)
  The type of gRPC method: `"UNARY"`, `"CLIENT_STREAMING"`, `"SERVER_STREAMING"`, or `"BIDI_STREAMING"`. Determines how the request and response streams are handled.

- **`IsClientStreaming`** (bool)
  `true` if the method accepts a stream of input messages from the client. Used to configure proxy behavior.

- **`IsServerStreaming`** (bool)
  `true` if the method streams output messages to the client. Used to configure proxy behavior.

- **`InputMessageType`** (string)
  The fully qualified name of the input message type (e.g., `.package.InputType`). Used for serialization and validation.

- **`OutputMessageType`** (string)
  The fully qualified name of the output message type (e.g., `.package.OutputType`). Used for serialization and validation.

- **`IsDeprecated`** (bool)
  Indicates whether the method is marked as deprecated in the proto definition.

- **`Description`** (string?)
  An optional human-readable description of the method. May be `null` if not provided.

- **`TimeoutMilliseconds`** (int)
  The default timeout in milliseconds for calls to this method. Used by the proxy to enforce timeouts.

- **`Data`** (T?)
  An optional user-defined property of type `T`. Not used by the bridge; reserved for application-specific metadata.

## Usage

### Example 1: Inspecting a service descriptor
