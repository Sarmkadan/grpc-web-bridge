# ProtocolTranslationServiceTests

Unit test suite for the `ProtocolTranslationService` class, which provides bidirectional conversion between HTTP/JSON and gRPC/protobuf protocols. This class validates the correctness of translation logic, metadata handling, and error response generation under various input conditions.

## API

### `ProtocolTranslationServiceTests()`
Constructor for the test fixture. Initializes the test context and dependencies required for protocol translation validation.

### `void TranslateHttpToGrpc_WithValidInput_ReturnsGrpcRequest()`
Validates that HTTP request data is correctly translated into a gRPC request structure. Ensures headers, body, and method information are preserved or transformed according to protocol rules.

### `void ConvertProtobufToJson_WithEmptyArray_ReturnsEmptyJson()`
Tests conversion of an empty protobuf array into JSON format. Verifies that the output is a valid JSON representation of an empty array (`[]`) without additional metadata or formatting artifacts.

### `void ConvertJsonToProtobuf_WithEmptyArray_ReturnsEmptyArray()`
Ensures that an empty JSON array (`[]`) is correctly parsed and converted into an equivalent protobuf array structure. Validates that no extraneous elements or errors are introduced during conversion.

### `void TranslateMetadata_WithNullMetadata_ReturnsEmptyDictionary()`
Confirms that null metadata input results in an empty dictionary output. Tests the service's handling of missing or undefined metadata without throwing exceptions.

### `void TranslateMetadata_WithMixedCaseKeys_ReturnsLowercasedKeys()`
Validates that metadata keys with mixed casing (e.g., `Content-Type`, `content-type`) are normalized to lowercase in the output dictionary. Ensures consistent key handling regardless of input casing.

### `void TranslateMetadata_WithGrpcTimeout_RemovesTimeoutHeader()`
Tests that gRPC-specific timeout headers are removed during metadata translation. Ensures compatibility with gRPC protocols that do not support or recognize timeout headers in metadata.

### `void CreateErrorResponse_WithValidInput_ReturnsGrpcResponseWithError()`
Validates the generation of a gRPC-compliant error response from a given error input. Ensures the response includes proper status code, message, and metadata fields as defined by the gRPC protocol.

### `void AsBytes_ExtensionMethod_ConvertsStringToByteArray()`
Tests the `AsBytes` extension method, which converts a string into a byte array using UTF-8 encoding. Validates correct byte representation and handles edge cases such as empty strings or strings with special characters.

## Usage
