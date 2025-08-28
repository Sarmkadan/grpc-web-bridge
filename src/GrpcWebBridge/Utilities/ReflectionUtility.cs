#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Reflection utilities for type inspection and dynamic invocation.
/// Provides helpers for working with types, methods, and properties.
/// </summary>
public static class ReflectionUtility
{
    /// <summary>
    /// Gets all public methods of a type with optional filtering.
    /// </summary>
    public static List<MethodInfo> GetPublicMethods(
        Type type,
        Func<MethodInfo, bool>? filter = null)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        if (filter is not null)
            methods = methods.Where(filter).ToArray();

        return methods.ToList();
    }

    /// <summary>
    /// Gets all public properties of a type.
    /// </summary>
    public static List<PropertyInfo> GetPublicProperties(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    public static bool ImplementsInterface(Type type, Type interfaceType)
    {
        if (type is null || interfaceType is null)
            return false;

        return interfaceType.IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the generic type arguments of a type.
    /// </summary>
    public static List<Type> GetGenericArguments(Type type)
    {
        if (type is null)
            return new List<Type>();

        return type.GetGenericArguments().ToList();
    }

    /// <summary>
    /// Invokes a method on an instance with specified parameters.
    /// </summary>
    public static object? InvokeMethod(
        object instance,
        string methodName,
        params object?[]? parameters)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

        var type = instance.GetType();
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        if (method is null)
            throw new MethodAccessException($"Method not found: {methodName}");

        return method.Invoke(instance, parameters);
    }

    /// <summary>
    /// Gets a property value from an instance.
    /// </summary>
    public static object? GetPropertyValue(object instance, string propertyName)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var type = instance.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
            return null;

        return property.GetValue(instance);
    }

    /// <summary>
    /// Sets a property value on an instance.
    /// </summary>
    public static void SetPropertyValue(object instance, string propertyName, object? value)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var type = instance.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
            throw new PropertyAccessException($"Property not found: {propertyName}");

        if (!property.CanWrite)
            throw new PropertyAccessException($"Property is read-only: {propertyName}");

        property.SetValue(instance, value);
    }

    /// <summary>
    /// Converts an object to a dictionary of its public properties.
    /// </summary>
    public static Dictionary<string, object?> ObjectToDictionary(object instance)
    {
        var dictionary = new Dictionary<string, object?>();

        if (instance is null)
            return dictionary;

        var properties = GetPublicProperties(instance.GetType());
        foreach (var prop in properties)
        {
            try
            {
                dictionary[prop.Name] = prop.GetValue(instance);
            }
            catch
            {
                // Skip properties that throw exceptions
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Creates a new instance of a type with constructor parameters.
    /// </summary>
    public static object? CreateInstance(Type type, params object?[]? constructorParams)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        try
        {
            return Activator.CreateInstance(type, constructorParams ?? []);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {type.Name}", ex);
        }
    }

    /// <summary>
    /// Gets custom attributes of a type or member.
    /// </summary>
    public static List<T> GetCustomAttributes<T>(MemberInfo member) where T : Attribute
    {
        if (member is null)
            return new List<T>();

        return member.GetCustomAttributes(typeof(T), true)
            .Cast<T>()
            .ToList();
    }

    /// <summary>
    /// Checks if a type is a primitive type or common value type.
    /// </summary>
    public static bool IsPrimitiveOrValueType(Type type)
    {
        if (type is null)
            return false;

        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               type.IsValueType;
    }

    /// <summary>
    /// Gets the base type hierarchy of a type.
    /// </summary>
    public static List<Type> GetTypeHierarchy(Type type)
    {
        var hierarchy = new List<Type> { type };

        var current = type.BaseType;
        while (current is not null && current != typeof(object))
        {
            hierarchy.Add(current);
            current = current.BaseType;
        }

        return hierarchy;
    }

    /// <summary>
    /// Finds a type by name in loaded assemblies.
    /// </summary>
    public static Type? FindType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Try built-in types first
        var type = Type.GetType(typeName);
        if (type is not null)
            return type;

        // Search in loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type is not null)
                return type;
        }

        return null;
    }

    /// <summary>
    /// Gets assembly version information.
    /// </summary>
    public static string? GetAssemblyVersion(Type type)
    {
        if (type is null)
            return null;

        return type.Assembly.GetName().Version?.ToString();
    }

    /// <summary>
    /// Checks if a method is async.
    /// </summary>
    public static bool IsAsyncMethod(MethodInfo method)
    {
        if (method is null)
            return false;

        return method.ReturnType == typeof(Task) ||
               (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }
}

/// <summary>
/// Exception for property access errors.
/// </summary>
public class PropertyAccessException : Exception
{
    public PropertyAccessException(string message) : base(message) { }
}
