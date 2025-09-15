# Swagger / OpenAPI Integration

This document explains how to enable Swagger UI and the OpenAPI 3.0 specification document
for gRPC-Web Bridge, and describes the known limitations when documenting streaming RPC patterns.

## Enabling Swagger / OpenAPI

gRPC-Web Bridge ships with a built-in helper that calls `AddOpenApi()` from
`Microsoft.AspNetCore.OpenApi`.

### 1. Register the services

```csharp
// Program.cs
builder.Services.AddGrpcWebBridge(options => options.WithDevelopment());

// Register OpenAPI document generation
builder.Services.AddGrpcWebBridgeSwagger(
    title: "My gRPC-Web Bridge API",
    version: "1.0.0");

builder.Services.AddControllers();
```

### 2. Map the OpenAPI endpoint

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Serves the raw OpenAPI JSON at /openapi/v1.json
    app.MapOpenApi();
}
```

### 3. Adding Swagger UI

`Microsoft.AspNetCore.OpenApi` provides the specification document, but the visual Swagger UI
requires an additional package.

**Option A — Swashbuckle.AspNetCore**

```bash
dotnet add package Swashbuckle.AspNetCore
```

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "gRPC-Web Bridge", Version = "v1" });

    // Propagate JWT bearer credentials from the UI to bridge endpoints
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer <token>'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();            // /swagger/v1/swagger.json
    app.UseSwaggerUI();          // /swagger/index.html
}
```

**Option B — Scalar (modern alternative)**

```bash
dotnet add package Scalar.AspNetCore
```

```csharp
using Scalar.AspNetCore;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();  // /scalar/v1
}
```

---

## appsettings.json flag

The bridge respects the `EnableSwagger` flag so you can keep OpenAPI generation off in
production without changing code:

```json
{
  "BridgeConfiguration": {
    "EnableSwagger": false
  }
}
```

---

## Known Limitations with Streaming Endpoints

OpenAPI 3.0 has **no native representation for long-lived HTTP streams**.  The following
limitations apply to any bridge deployment that uses streaming RPC patterns.

### Unary RPCs — fully supported

`POST /api/bridge/invoke` is a standard request/response operation and appears correctly in
Swagger UI.  Request and response bodies are JSON-serialisable and round-trip cleanly.

### Server-streaming RPCs — partially representable

`POST /api/bridge/stream` is documented as a normal `POST` operation in the OpenAPI
specification.  Swagger UI will show it as a single request → single response, which does
**not** convey that the HTTP connection stays open and multiple JSON objects are flushed
over time.

Mitigations:
- Annotate the endpoint summary with "**Server-streaming** — response body is a sequence of
  newline-delimited JSON objects."
- Consider using `text/event-stream` (Server-Sent Events) as the response content type so
  that browsers and tooling understand the long-lived nature of the connection.

### Client-streaming and bidirectional streaming — not representable

OpenAPI 3.0 cannot describe full-duplex streaming.  Bidirectional streams require
WebSocket or HTTP/2 server-push, neither of which is expressible in an OpenAPI document.
Bridge endpoints that proxy bidirectional gRPC streams will appear as plain `POST` operations.

Alternatives:
- Document bidirectional streaming endpoints separately in an `AsyncAPI` specification.
- Use the `externalDocs` field on the OpenAPI operation object to link to extended docs.

### gRPC-Web trailers

gRPC status codes and trailing metadata are not visible in the HTTP response body exposed by
the OpenAPI document.  Clients must read the `grpc-status` and `grpc-message` **trailers**
(or the encoded trailer frame in the response body for gRPC-Web-Text) to determine the
outcome of a streaming call.

---

## Customising the OpenAPI Document

You can enrich the generated schema with XML doc-comments by enabling documentation file
generation in the project file:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Then pass the XML file to Swashbuckle:

```csharp
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
```

---

## Security Considerations

- Do **not** expose Swagger UI in production unless the `/swagger` and `/openapi` paths are
  protected behind authentication or a network boundary.
- Set `EnableSwagger = false` in `appsettings.Production.json` and enable it only through
  environment-specific overrides.
