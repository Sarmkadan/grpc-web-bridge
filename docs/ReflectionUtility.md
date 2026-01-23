# ReflectionUtility
Utility class that simplifies common reflection tasks such as querying members, invoking methods, getting/setting property values, and converting objects to dictionaries.

## API
### GetPublicMethods
```csharp
public static List<MethodInfo> GetPublicMethods(Type type)
```
Returns a list of all public instance and static methods declared on `type` (including inherited members).  
**Parameters**  
- `type`: The type to inspect.  
**Return value**  
- `List<MethodInfo>` containing the methods; empty list if none are found.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.

### GetPublicProperties
```csharp
public static List<PropertyInfo> GetPublicProperties(Type type)
```
Returns a list of all public instance and static properties declared on `type` (including inherited members).  
**Parameters**  
- `type`: The type to inspect.  
**Return value**  
- `List<PropertyInfo>` containing the properties; empty list if none are found.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.

### ImplementsInterface
```csharp
public static bool ImplementsInterface(Type type, Type interfaceType)
```
Determines whether `type` implements `interfaceType` directly or through inheritance.  
**Parameters**  
- `type`: The type to test.  
- `interfaceType`: The interface type to check for.  
**Return value**  
- `true` if `type` implements `interfaceType`; otherwise `false`.  
**Exceptions**  
- `ArgumentNullException` if either parameter is `null`.

### GetGenericArguments
```csharp
public static List<Type> GetGenericArguments(Type type)
```
Returns the generic type arguments of `type` if it is a constructed generic type; otherwise returns an empty list.  
**Parameters**  
- `type`: The type to inspect.  
**Return value**  
- `List<Type>` of generic arguments.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.

### InvokeMethod
```csharp
public static object? InvokeMethod(object target, string methodName, params object[] parameters)
```
Invokes the method named `methodName` on `target` using reflection and returns its result.  
**Parameters**  
- `target`: The object whose method is invoked; `null` for static methods.  
- `methodName`: Name of the method to invoke.  
- `parameters`: Arguments to pass to the method.  
**Return value**  
- The method's return value, or `null` if the method returns `void`.  
**Exceptions**  
- `ArgumentNullException` if `target` is `null` for an instance method or `methodName` is `null`.  
- `ArgumentException` if no matching method is found.  
- `TargetInvocationException` wrapping any exception thrown by the invoked method.

### GetPropertyValue
```csharp
public static object? GetPropertyValue(object target, string propertyName)
```
Retrieves the value of the property named `propertyName` on `target`.  
**Parameters**  
- `target`: The object containing the property.  
- `propertyName`: Name of the property to read.  
**Return value**  
- The property value, or `null` if the property's type is a reference type and the value is `null`.  
**Exceptions**  
- `ArgumentNullException` if `target` or `propertyName` is `null`.  
- `PropertyAccessException` if the property does not exist, is not readable, or an error occurs during retrieval.

### SetPropertyValue
```csharp
public static void SetPropertyValue(object target, string propertyName, object value)
```
Sets the property named `propertyName` on `target` to `value`.  
**Parameters**  
- `target`: The object containing the property.  
- `propertyName`: Name of the property to set.  
- `value`: The value to assign.  
**Exceptions**  
- `ArgumentNullException` if `target` or `propertyName` is `null`.  
- `PropertyAccessException` if the property does not exist, is not writable, or the value type is incompatible.

### ObjectToDictionary
```csharp
public static Dictionary<string, object?> ObjectToDictionary(object obj)
```
Creates a dictionary where each key is the name of a public readable property of `obj` and each value is the property's current value.  
**Parameters**  
- `obj`: The object to convert.  
**Return value**  
- `Dictionary<string, object?>` representing the object's public property state; empty dictionary if `obj` has no readable public properties.  
**Exceptions**  
- `ArgumentNullException` if `obj` is `null`.

### CreateInstance
```csharp
public static object? CreateInstance(Type type, params object[] ctorArgs)
```
Creates an instance of `type` using a constructor that matches `ctorArgs`.  
**Parameters**  
- `type`: The type to instantiate.  
- `ctorArgs`: Arguments to pass to the constructor.  
**Return value**  
- The newly created instance, or `null` if `type` cannot be instantiated.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.  
- `TargetInvocationException` if the constructor throws an exception.  
- `MissingMethodException` if no matching constructor is found.

### GetCustomAttributes<T>
```csharp
public static List<T> GetCustomAttributes<T>(MemberInfo member) where T : Attribute
```
Returns all custom attributes of type `T` applied to `member`.  
**Parameters**  
- `member`: The member (type, method, property, etc.) to inspect.  
**Return value**  
- `List<T>` containing the attributes; empty list if none are present.  
**Exceptions**  
- `ArgumentNullException` if `member` is `null`.

### IsPrimitiveOrValueType
```csharp
public static bool IsPrimitiveOrValueType(Type type)
```
Determines whether `type` is a primitive type (e.g., `int`, `bool`, `char`) or a value type (`struct`).  
**Parameters**  
- `type`: The type to test.  
**Return value**  
- `true` if `type` is primitive or a value type; otherwise `false`.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.

### GetTypeHierarchy
```csharp
public static List<Type> GetTypeHierarchy(Type type)
```
Returns the inheritance chain of `type` from itself up to `System.Object`.  
**Parameters**  
- `type`: The type to inspect.  
**Return value**  
- `List<Type>` where the first element is `type` and the last is `System.Object`; empty list if `type` is `null`.  
**Exceptions**  
- `ArgumentNullException` if `type` is `null`.

### FindType
```csharp
public static Type? FindType(string typeName)
```
Searches all loaded assemblies for a type with the specified name and returns the first match.  
**Parameters**  
- `typeName`: The full name of the type to locate (e.g., `"System.Collections.Generic.List`1"`).  
**Return value**  
- The `Type` if found; otherwise `null`.  
**Exceptions**  
- `ArgumentNullException` if `typeName` is `null`.

### GetAssemblyVersion
```csharp
public static string? GetAssemblyVersion(Assembly assembly)
```
Retrieves the version string of the supplied assembly.  
**Parameters**  
- `assembly`: The assembly to query.  
**Return value**  
- The version as a string (e.g., `"1.2.3.0"`), or `null` if the assembly has no version or is `null`.  
**Exceptions**  
- None; returns `null` for invalid input.

### IsAsyncMethod
```csharp
public static bool IsAsyncMethod(MethodInfo method)
```
Indicates whether `method` returns a `Task` or `Task<T>` result, marking it as asynchronous.  
**Parameters**  
- `method`: The method to evaluate.  
**Return value**  
- `true` if the method's return type is `Task` or derives from `Task`; otherwise `false`.  
**Exceptions**  
- `ArgumentNullException` if `method` is `null`.

### PropertyAccessException
```csharp
public PropertyAccessException(string message) : base(message)
```
Exception thrown by `GetPropertyValue` and `SetPropertyValue` when a property cannot be accessed.  
**Parameters**  
- `message`: Descriptive error message.  

## Usage
### Example 1: Converting a POCO to a dictionary for serialization
```csharp
public static Dictionary<string, object?> ToDictionary<T>(T obj) where T : class
{
    if (obj == null) throw new ArgumentNullException(nameof(obj));
    return ReflectionUtility.ObjectToDictionary(obj);
}

// Usage
var person = new Person { Id = 42, Name = "Ada" };
var dict = ToDictionary(person);
// dict contains { ["Id"] = 42, ["Name"] = "Ada" }
```

### Example 2: Invoking a method dynamically based on configuration
```csharp
public static object? InvokeConfiguredMethod(object service, string methodName, object[] args)
{
    var method = ReflectionUtility.GetPublicMethods(service.GetType())
                                  .FirstOrDefault(m => m.Name == methodName);
    if (method == null)
        throw new InvalidOperationException($"Method {methodName} not found.");

    return ReflectionUtility.InvokeMethod(service, methodName, args);
}

// Usage
var result = InvokeConfiguredMethod(myService, "ProcessData", new object[] { inputData });
```

## Notes
- All static methods are thread‑safe; they do not retain state and rely only on the inputs provided.  
- Passing `null` for any required reference parameter will result in an `ArgumentNullException`.  
- `GetPublicMethods` and `GetPublicProperties` return only members marked `public`; non‑public members are ignored.  
- `GetTypeHierarchy` follows the base‑type chain only; it does not include implemented interfaces.  
- `FindType` scans the currently loaded assemblies; types in assemblies not yet loaded will not be found unless those assemblies are loaded beforehand.  
- `InvokeMethod` wraps any exception thrown by the target method in a `TargetInvocationException`; inspect the `InnerException` for the actual error.  
- `PropertyAccessException` is used exclusively for property read/write failures; it does not wrap other exception types.  
- Generic methods like `GetCustomAttributes<T>` require the type argument to be an `Attribute`; supplying a non‑attribute type will cause a compile‑time error.  
- `IsPrimitiveOrValueType` returns `true` for enumerations because they are value types; if enum detection is needed separately, check `type.IsEnum` after this call.
