using System;

namespace NexusMods.HyperDuck.Adaptor;

public static class ReflectionHelpers
{
    public static bool HasImplicitConversionFrom(this Type dest, Type src)
    {
        if (src.IsAssignableFrom(dest))
            return true;

        foreach (var method in dest.GetMethods())
        {
            if (method.Name == "op_Implicit" && method.ReturnType == dest && method.GetParameters()[0].ParameterType == src)
                return true;
        }

        return false;
    }
    
    
    /// <summary>
    /// Searches for the first time the src type inherits (or implements) the genericType. If found, the
    /// generic arguments to that interface/class are returned. Starts by searching for interfaces on the current
    /// class (and any sub-interfaces of that class), then goes to the base class (if any) and searches it and its
    /// interfaces, walking up the tree. The result is a depth-first search for the given generic interface/class
    /// </summary>
    public static bool TryExtractGenericInterfaceArguments(this Type src, Type genericType, out Type[] genericArguments)
    {
        genericArguments = [];
        var currentType = src;
        
        // Walk up the inheritance hierarchy
        while (currentType != null)
        {
            // First check if the current type itself matches (for classes)
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == genericType)
            {
                genericArguments = currentType.GetGenericArguments();
                return true;
            }
            
            // Then check all interfaces implemented by the current type (depth-first)
            if (TryFindInInterfacesDepthFirst(currentType.GetInterfaces(), genericType, out genericArguments))
            {
                return true;
            }
            
            // Move to the base class
            currentType = currentType.BaseType;
        }
        
        return false;
    }

    private static bool TryFindInInterfacesDepthFirst(Type[] interfaces, Type genericType, out Type[] genericArguments)
    {
        genericArguments = [];
        
        foreach (var interfaceType in interfaces)
        {
            // Check the current interface
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
            {
                genericArguments = interfaceType.GetGenericArguments();
                return true;
            }
            
            // Recursively check sub-interfaces (depth-first)
            var subInterfaces = interfaceType.GetInterfaces();
            if (TryFindInInterfacesDepthFirst(subInterfaces, genericType, out genericArguments))
            {
                return true;
            }
        }
        
        return false;
    }

}
