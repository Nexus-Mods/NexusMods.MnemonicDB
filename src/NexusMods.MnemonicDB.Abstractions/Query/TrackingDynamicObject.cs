using System;
using System.Collections.Generic;
using System.Dynamic;
using NexusMods.MnemonicDB.Abstractions.Query;

public class TrackingDynamicObject : DynamicObject
{
    // Dictionary to store tracked properties and their associated types
    private readonly Dictionary<string, Type> _propertyTypes = new();

    // Dictionary to store values for each property
    private readonly Dictionary<string, object> _propertyValues = new();

    // Called when attempting to access a property dynamically
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        string propertyName = binder.Name;

        // If the property hasn't been accessed before, create an entry for it
        if (!_propertyValues.TryGetValue(propertyName, out var r))
        {
            result = new TrackedValue(this, propertyName);
            _propertyValues[propertyName] = r!;
        }
        else
        {
            result = r;
        }
        return true;
    }

    // Method to track conversion and ensure no conflicting types are assigned to a property
    public void TrackConversion(string propertyName, Type targetType)
    {
        if (_propertyTypes.TryGetValue(propertyName, out var existingType))
        {
            if (existingType != targetType)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' has already been cast to '{existingType.Name}', cannot cast to '{targetType.Name}'."
                );
            }
        }
        else
        {
            _propertyTypes[propertyName] = targetType;
        }
    }

    // Inner class to represent a property value that is being tracked
    public class TrackedValue : DynamicObject
    {
        private readonly TrackingDynamicObject _parent;
        private readonly string _propertyName;
        private ulong _id = 0;
        private Type? _type = null;

        public TrackedValue(TrackingDynamicObject parent, string propertyName)
        {
            _id = LVar.NextId();
            _parent = parent;
            _propertyName = propertyName;
        }

        // Handle custom conversion logic
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            throw new NotImplementedException("Do we need this?");
            // Track the conversion in the parent object
            _parent.TrackConversion(_propertyName, binder.Type);

            // Here, just return a default value for simplicity
            throw new NotImplementedException();
            result = GetDefaultValue(binder.Type);
            return true;
        }
        private static object GetDefaultValue(Type type)
        {
            //return type.IsValueType ? Activator.CreateInstance(type) : null;
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Convert the tracked value to a logic variable of the given type
        /// </summary>
        public LVar<T> AsLVar<T>()
        {
            _type ??= typeof(T);
            
            if (typeof(T) != _type)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' has been cast to '{_type?.Name}', cannot cast to '{typeof(T).Name}'."
                );
            }
            return LVar.Create<T>(_propertyName);
        }
    }
}
