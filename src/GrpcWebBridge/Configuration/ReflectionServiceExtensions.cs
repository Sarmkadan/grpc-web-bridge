#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Provides extension methods for configuring gRPC reflection services in the ASP.NET Core pipeline.
/// </summary>
public static class ReflectionServiceExtensions
{
    /// <summary>
    /// Seals the class to prevent inheritance, as this is a pure utility class with extension methods.
    /// </summary>
    static ReflectionServiceExtensions() {}

    /// <summary>
    /// Registers <see cref="ReflectionService"/> as a singleton in the service container.
    /// Call this after <see cref="DependencyInjection.AddGrpcWebBridge"/> to enable
    /// runtime discovery of registered gRPC services via the reflection REST API.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddGrpcWebBridgeReflection(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ReflectionService>();

        return services;
    }

    /// <summary>
    /// Maps the reflection REST endpoints under <c>/api/reflection</c>.
    /// </summary>
    /// <list type="table">
    /// <listheader>
    /// <term>Route</term><description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>GET /api/reflection/services</term>
    /// <description>Returns the full names of all registered services.</description>
    /// </item>
    /// <item>
    /// <term>GET /api/reflection/services/{fullName}</term>
    /// <description>Returns the descriptor for a single service.</description>
    /// </item>
    /// <item>
    /// <term>GET /api/reflection/services/{fullName}/methods/{methodName}</term>
    /// <description>Returns the descriptor for a single method on a service.</description>
    /// </item>
    /// <item>
    /// <term>GET /api/reflection/descriptors</term>
    /// <description>Returns descriptors for all registered services in one call.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is <c>null</c>.</exception>
    public static IEndpointRouteBuilder MapGrpcReflectionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/reflection")
            .WithTags("Reflection")
            .WithOpenApi();

        group.MapGet("/services", async (
            ReflectionService reflection,
            CancellationToken ct) =>
        {
            var result = await reflection.ListServiceNamesAsync(ct).ConfigureAwait(false);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ListServiceNames")
        .WithSummary("List the names of all registered gRPC services");

        group.MapGet("/services/{fullName}", async (
            string fullName,
            ReflectionService reflection,
            CancellationToken ct) =>
        {
            var decoded = Uri.UnescapeDataString(fullName);
            var result = await reflection.GetServiceDescriptorAsync(decoded, ct).ConfigureAwait(false);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithName("GetServiceDescriptor")
        .WithSummary("Get the reflection descriptor for a registered gRPC service");

        group.MapGet("/services/{fullName}/methods/{methodName}", async (
            string fullName,
            string methodName,
            ReflectionService reflection,
            CancellationToken ct) =>
        {
            var decodedService = Uri.UnescapeDataString(fullName);
            var result = await reflection.GetMethodDescriptorAsync(decodedService, methodName, ct).ConfigureAwait(false);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithName("GetMethodDescriptor")
        .WithSummary("Get the reflection descriptor for a specific gRPC method");

        group.MapGet("/descriptors", async (
            ReflectionService reflection,
            CancellationToken ct) =>
        {
            var result = await reflection.GetAllDescriptorsAsync(ct).ConfigureAwait(false);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("GetAllDescriptors")
        .WithSummary("Get reflection descriptors for all registered gRPC services");

        return endpoints;
    }
}