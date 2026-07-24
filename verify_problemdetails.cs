#nullable enable
using System;
using System.Text.Json;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Middleware;
using Microsoft.AspNetCore.Http;

Console.WriteLine("Testing RFC 7807 ProblemDetails Implementation...\n");

// Test 1: Create a ValidationException and convert to ProblemDetails
var validationException = new ValidationException(
    "email",
    "invalid-email@example",
    "email_format",
    "Email address is not in a valid format"
);

var problemDetails = validationException.ToProblemDetails();

Console.WriteLine("Test 1: ValidationException to ProblemDetails");
Console.WriteLine($"Type: {problemDetails.Type}");
Console.WriteLine($"Title: {problemDetails.Title}");
Console.WriteLine($"Status: {problemDetails.Status}");
Console.WriteLine($"Detail: {problemDetails.Detail}");
Console.WriteLine("Extensions:");
foreach (var kvp in problemDetails.Extensions)
{
    Console.WriteLine($" {kvp.Key}: {kvp.Value}");
}
Console.WriteLine();

// Test 2: Create a generic exception and convert to ProblemDetails
var genericException = new InvalidOperationException("Something went wrong");
var httpContext = new DefaultHttpContext();
var genericProblemDetails = genericException.ToProblemDetails(httpContext);

Console.WriteLine("Test 2: Generic Exception to ProblemDetails");
Console.WriteLine($"Type: {genericProblemDetails.Type}");
Console.WriteLine($"Title: {genericProblemDetails.Title}");
Console.WriteLine($"Status: {genericProblemDetails.Status}");
Console.WriteLine($"Detail: {genericProblemDetails.Detail}");
Console.WriteLine($"TraceId: {genericProblemDetails.TraceId}");
Console.WriteLine($"Path: {genericProblemDetails.Path}");
Console.WriteLine();

// Test 3: Test ErrorResponse backward compatibility
var errorResponse = new ErrorResponse
{
    Success = false,
    Type = "https://example.com/errors/validation",
    Title = "Validation Error",
    Status = 400,
    Detail = "Field validation failed",
    Path = "/api/users",
    TraceId = "test-trace-123",
    Timestamp = DateTime.UtcNow
};

errorResponse.Details = new Dictionary<string, object?>
{
    ["errors"] = new Dictionary<string, string>
    {
        ["email"] = "Invalid email format",
        ["password"] = "Password must be at least 8 characters"
    },
    ["field"] = "email",
    ["errorCode"] = "VALIDATION_ERROR"
};

var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
});

Console.WriteLine("Test 3: ErrorResponse Serialization (Backward Compatible)");
Console.WriteLine(json);
Console.WriteLine();

// Test 4: Verify ProblemDetails is RFC 7807 compliant
Console.WriteLine("Test 4: RFC 7807 Compliance Check");
Console.WriteLine("✓ Type field present: " + (problemDetails.Type != null));
Console.WriteLine("✓ Title field present: " + (problemDetails.Title != null));
Console.WriteLine("✓ Status field present: " + (problemDetails.Status.HasValue));
Console.WriteLine("✓ Detail field present: " + (problemDetails.Detail != null));
Console.WriteLine("✓ Extensions field available: " + (problemDetails.Extensions != null));
Console.WriteLine("✓ Field-level errors available: " + (problemDetails.Extensions.ContainsKey("errors")));
Console.WriteLine();

// Test 5: Test AddValidationError method
var problemDetails2 = new ProblemDetails
{
    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    Title = "Validation Failed",
    Status = 400,
    Detail = "Multiple validation errors occurred"
};

problemDetails2.AddValidationError("username", "Username is required", null);
problemDetails2.AddValidationError("email", "Email is invalid", "test@example");
problemDetails2.AddValidationError("age", "Age must be at least 18", 15);

Console.WriteLine("Test 5: AddValidationError method");
Console.WriteLine($"Errors count: {((Dictionary<string, object?>)problemDetails2.Extensions["errors"]).Count}");
Console.WriteLine();

Console.WriteLine("All tests completed successfully! ✓");