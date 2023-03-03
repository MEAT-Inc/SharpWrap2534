using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpWrapperTests.TestHelpers
{
    /// <summary>
    /// A wrapper object that helps to expose normally private or protected internal classes for testing.
    /// Normally I'd just change the assembly info file of the testing application, but I don't want to cause problems so I wrote this quickly
    /// </summary>
    internal class InternalInvoker
    {
        #region Custom Events

        // Events used to indicate steps being competed during an extraction routine/operation
        public event EventHandler<InvokerSuccessEventArgs> InvokeSuccess;
        public event EventHandler<InvokerFailureEventArgs> InvokeFailure;

        #endregion //Custom Events

        #region Fields

        // Public backing fields for class information and type information
        public readonly Type InvokedType;                                        // The type to find all needed method information from
        public readonly string InvokedNamespace;                                 // Namespace of the invoked type object
        public readonly BindingFlags SearchFlags;                                // Binding flags to limit how far we search in this object

        // The actual object being invoked on when operations are run

        // Dictionaries holding all loaded methods and their names
        private Dictionary<string, FieldInfo> _typeFieldInfos;                   // A collection of all loaded field info objects
        private Dictionary<string, MethodInfo> _typeMethodInfos;                 // A collection of all loaded method info objects
        private Dictionary<string, PropertyInfo> _typePropertyInfos;             // A collection of all loaded property info objects
        private Dictionary<string, ConstructorInfo> _typeConstructorInfos;       // A collection of all loaded constructor info objects

        #endregion //Fields

        #region Properties

        // Instance of the object being reflected on
        public dynamic InvokedInstance { get; private set; }

        // Exposed properties for the different type definition values built out when this instance was created
        public Dictionary<string, FieldInfo> TypeFieldInfos => this._typeFieldInfos ??= this._buildTypeFields();
        public Dictionary<string, MethodInfo> TypeMethodInfos => this._typeMethodInfos ??= this._buildTypeMethods();
        public Dictionary<string, PropertyInfo> TypePropertyInfos => this._typePropertyInfos ??= this._buildTypeProperties();
        public Dictionary<string, ConstructorInfo> TypeConstructorInfos => this._typeConstructorInfos ??= this._buildTypeConstructors();

        #endregion //Properties

        #region Structs and Classes
        
        /// <summary>
        /// Enumeration that holds all the state values for an invoke operation.
        /// Used in the Event Args class to help determine what's going on
        /// </summary>
        public enum InvokerEventTypes
        {
            // No action type defined/base value 
            NO_ACTION        = 0x00,

            // CTOR and Method Event Type Values
            CTOR_INVOKED     = 0x01,
            METHOD_INVOKED   = 0x02,

            // Getting Value Events 
            GET_VALUE        = 0x10,
            GET_FIELD        = GET_VALUE | 0x01,
            GET_PROPERTY     = GET_VALUE | 0x02,

            // Setting Value Events
            SET_VALUE        = 0x20,
            SET_FIELD        = SET_VALUE | 0x01,
            SET_PROPERTY     = SET_VALUE | 0x02,
        }

        /// <summary>
        /// Class object that holds information about an invoker event routine
        /// </summary>
        public class InvokerEventArgs : EventArgs
        {
            // Readonly information about the event argument objects
            public readonly bool InvokePassed;              // True of false based on our action results 
            public readonly object[] InvokedArgs;           // Argument objects used in this routine 
            public readonly InvokerEventTypes EventType;    // The type of event being fired for this event args class

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new base invoker event argument object. Used for all fired events
            /// </summary>
            /// <param name="Success">True or false based on if the event passed or not</param>
            /// <param name="EventType">The type of event fired for this event</param>
            /// <param name="EventArguments">Args provided to the event being fired out</param>
            public InvokerEventArgs(bool Success, InvokerEventTypes EventType, params object[] EventArguments)
            {
                // Store class values and exit out
                this.EventType = EventType;
                this.InvokePassed = Success;
                this.InvokedArgs = EventArguments;
            }
        }
        /// <summary>
        /// Successful operation event argument objects. Used to indicate an operation passed during an invoke routine
        /// </summary>
        public class InvokerSuccessEventArgs : InvokerEventArgs
        {
            // Readonly information about the member object that is modified
            public readonly string MemberName;              // Name of the member being updated
            public readonly object MemberValue;             // Value of the member we pulled or set
            
            // Type of member being invoked and the member info object used to do it
            public readonly MemberTypes MemberType;         // The type of the member which was updated or pulled
            public readonly MemberInfo MemberInformation;   // Member information being used to invoke this routine

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new instance of a successful event argument object involving field information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="CtorInvoked">The field being invoked or updated or pulled</param>
            /// <param name="ObjectBuilt">The value pulled or set for our field instance</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, ConstructorInfo CtorInvoked, object ObjectBuilt)
                : base(true, EventType, CtorInvoked.Name, CtorInvoked.MemberType, ObjectBuilt)
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = ObjectBuilt;
                this.MemberName = CtorInvoked.Name;
                this.MemberInformation = CtorInvoked;
                this.MemberType = MemberTypes.Constructor;
            }
            /// <summary>
            /// Builds a new instance of a successful event argument object involving field information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="CtorInvoked">The field being invoked or updated or pulled</param>
            /// <param name="ObjectBuilt">The value pulled or set for our field instance</param>
            /// <param name="CtorArguments">Arguments passed into the CTOR of this object</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, ConstructorInfo CtorInvoked, object ObjectBuilt, object[] CtorArguments)
                : base(true, EventType, CtorInvoked.Name, CtorInvoked.MemberType, CtorArguments.Prepend(ObjectBuilt))
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = ObjectBuilt;
                this.MemberName = CtorInvoked.Name;
                this.MemberInformation = CtorInvoked;
                this.MemberType = MemberTypes.Constructor;
            }

            /// <summary>
            /// Builds a new instance of a successful event argument object involving field information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="FieldInvoked">The field being invoked or updated or pulled</param>
            /// <param name="ObjectBuilt">The value pulled or set for our field instance</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, FieldInfo FieldInvoked, object ObjectBuilt)
                : base(true, EventType, FieldInvoked.Name, FieldInvoked.FieldType, ObjectBuilt)
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = ObjectBuilt;
                this.MemberType = MemberTypes.Field;
                this.MemberName = FieldInvoked.Name;
                this.MemberInformation = FieldInvoked;
            }
            /// <summary>
            /// Builds a new instance of a successful event argument object involving property information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="PropertyInvoked">The field being invoked or updated or pulled</param>
            /// <param name="PropertyValue">The value pulled or set for our member instance</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, PropertyInfo PropertyInvoked, object PropertyValue) 
                : base(true, EventType, PropertyInvoked.Name, PropertyInvoked.PropertyType, PropertyValue)
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = PropertyValue;
                this.MemberType = MemberTypes.Property;
                this.MemberName = PropertyInvoked.Name;
                this.MemberInformation = PropertyInvoked;
            }

            /// <summary>
            /// Builds a new instance of a successful event argument object involving method information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="MethodInvoked">The method being invoked</param>
            /// <param name="MethodResult">The value returned from the invoked method</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, MethodInfo MethodInvoked, object MethodResult)
                : base(true, EventType, MethodInvoked, MethodResult)
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = MethodResult;
                this.MemberType = MemberTypes.Method;
                this.MemberName = MethodInvoked.Name;
                this.MemberInformation = MethodInvoked;
            }
            /// <summary>
            /// Builds a new instance of a successful event argument object involving method information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="MethodInvoked">The method being invoked</param>
            /// <param name="MethodArgs">The arguments fired in the method</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, MethodInfo MethodInvoked, object[] MethodArgs)
                : base(true, EventType, MethodArgs.Prepend(MethodInvoked).ToArray())
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = null;
                this.MemberType = MemberTypes.Method;
                this.MemberName = MethodInvoked.Name;
                this.MemberInformation = MethodInvoked;
            }
            /// <summary>
            /// Builds a new instance of a successful event argument object involving method information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="MethodInvoked">The method being invoked</param>
            /// <param name="MethodResult">The value returned from the invoked method</param>
            /// <param name="MethodArgs">The arguments fired in the method</param>
            public InvokerSuccessEventArgs(InvokerEventTypes EventType, MethodInfo MethodInvoked, object[] MethodArgs, object MethodResult)
                : base(true, EventType, MethodArgs.Prepend(MethodInvoked).Prepend(MethodResult).ToArray())
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberValue = MethodResult;
                this.MemberType = MemberTypes.Method;
                this.MemberName = MethodInvoked.Name;
                this.MemberInformation = MethodInvoked;
            }
        }
        /// <summary>
        /// Failed operation event argument objects. Used to indicate an operation was unsuccessful during an invoke routine
        /// </summary>
        public class InvokerFailureEventArgs : InvokerEventArgs
        {
            // Readonly information about the failure that was seen during an invoke routine    
            public readonly MemberTypes MemberType;         // The type of the member which was updated or pulled
            public readonly Exception InvokerException;     // The Exception thrown during this routine
            public readonly MemberInfo MemberInformation;   // Member information being used to invoke this routine

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new instance of a successful event argument object involving field information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="ExceptionThrown">The exception thrown during this routine</param>
            public InvokerFailureEventArgs(InvokerEventTypes EventType, Exception ExceptionThrown, params object[] EventArguments)
                : base(false, EventType, ExceptionThrown, EventArguments)
            {
                // Store class values, run the base CTOR, and exit out
                this.InvokerException = ExceptionThrown;
            }
            /// <summary>
            /// Builds a new instance of a successful event argument object involving field information
            /// </summary>
            /// <param name="EventType">The type of event fired for this routine</param>
            /// <param name="ExceptionThrown">The exception thrown during this routine</param>
            /// <param name="MemberInvoked">Member Info about the class member that threw this exception</param>
            public InvokerFailureEventArgs(InvokerEventTypes EventType, Exception ExceptionThrown, MemberInfo MemberInvoked, params object[] EventArguments) 
                : base(false, EventType, ExceptionThrown, MemberInvoked, EventArguments)
            {
                // Store class values, run the base CTOR, and exit out
                this.MemberInformation = MemberInvoked;
                this.InvokerException = ExceptionThrown;
                this.MemberType = MemberInvoked.MemberType;
            }
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a Type invoker object and loads all supported properties and fields of it
        /// </summary>
        /// <param name="TypeToInvoke">The type to reflect and invoke from</param>
        /// <param name="ReflectionFlags">Flags to control what fields/properties/methods are loaded in</param>
        /// <exception cref="InvalidOperationException">Thrown when an invalid type is provided to be reflected</exception>
        public InternalInvoker(Type TypeToInvoke, BindingFlags ReflectionFlags = BindingFlags.Default)
        {
            // Store the invoker type on this instance and find all the methods/properties we could need to use for it
            this.SearchFlags = ReflectionFlags;
            this.InvokedType = TypeToInvoke ?? throw new InvalidOperationException("Error! Provide a non-null type value!");
            this.InvokedNamespace = this.InvokedType.Namespace;

            // Initialize the types and event handlers for this invoker
            this._initializeInvokerTypes();
        }
        /// <summary>
        /// Builds a new instance of a Type invoker object and loads all supported properties and fields of it
        /// </summary>
        /// <param name="QualifiedTypeName">Fully qualified name of the type to reflect and invoke from</param>
        /// <param name="ReflectionFlags">Flags to control what fields/properties/methods are loaded in</param>
        /// <exception cref="InvalidOperationException">Thrown when an invalid type is provided to be reflected</exception>
        public InternalInvoker(string QualifiedTypeName, BindingFlags ReflectionFlags = BindingFlags.Default)
        {
            // Store the invoker type on this instance and find all the methods/properties we could need to use for it
            this.SearchFlags = ReflectionFlags;
            if (!QualifiedTypeName.Contains(".")) throw new ArgumentException("Error! Please provide a valid type name");
            this.InvokedType = Type.GetType(QualifiedTypeName) ?? throw new InvalidOperationException("Error! Provide a non-null type value!");
            this.InvokedNamespace = this.InvokedType.Namespace;

            // Initialize the types and event handlers for this invoker
            this._initializeInvokerTypes();
        }
        /// <summary>
        /// Builds a new instance of a Type invoker object and loads all supported properties and fields of it
        /// </summary>
        /// <param name="InstanceToConsume">An existing object which we need to store the type of and reflect off of</param>
        /// <param name="ReflectionFlags">Flags to control what fields/properties/methods are loaded in</param>
        /// <exception cref="InvalidOperationException">Thrown when an invalid type is provided to be reflected</exception>
        public InternalInvoker(object InstanceToConsume, BindingFlags ReflectionFlags = BindingFlags.Default)
        {
            // Store the invoker type on this instance and find all the methods/properties we could need to use for it
            this.InvokedInstance = InstanceToConsume ?? throw new ArgumentNullException("Error! Requested object to consume we seen to be null!");
            this.InvokedType = this.InvokedInstance.GetType();
            this.InvokedNamespace = this.InvokedType.Namespace;
            this.SearchFlags = ReflectionFlags;

            // Initialize the types and event handlers for this invoker
            this._initializeInvokerTypes();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        #region Public Invoke Operation Methods

        /// <summary>
        /// Attempts to build a new instance of the invoked type based on constructor arguments
        /// </summary>
        /// <param name="ConstructorArgs">The arguments to use to construct this instance</param>
        /// <returns>True if constructed. False if not</returns>
        public bool ConstructInstance(params object[] ConstructorArgs)
        {
            try
            {
                // Try and find a usable Constructor first
                ConstructorInfo CtorToInvoke = this.TypeConstructorInfos.FirstOrDefault(CtorInfoPair =>
                {
                    // Find the CTOR parameter objects and build a type string for the ars passed in
                    var CtorParams = CtorInfoPair.Value.GetParameters();
                    string ParamsTypeString =
                        $"{this.InvokedType.Name}" +
                        $"({string.Join(",", ConstructorArgs.Select(ParamObj => ParamObj.GetType().Name))})";

                    // Compare the types found and our argument string values here
                    if (CtorInfoPair.Key == ParamsTypeString) return true;
                    if (CtorParams.Length == 0 && ConstructorArgs.Length == 0) return true;
                    if (CtorParams.Length != 1 && ConstructorArgs.Length == CtorParams.Length) return true;
                    if (CtorParams.Length == 1 && ConstructorArgs[0].GetType() == CtorParams[0].ParameterType) return true;

                    // If none of these conditions are met, return false
                    return false;
                }).Value;

                // If the method is null or the invoker instance is not built for non static methods, throw an exception now
                if (CtorToInvoke == null)
                    throw new MissingMemberException($"Error! Could not find a constructor for type {this.InvokedType.Name} matching given signature!");

                // If the CTOR value info object is null, return false. Otherwise construct a new instance and return passed
                this.InvokedInstance = CtorToInvoke.Invoke(ConstructorArgs);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.CTOR_INVOKED, 
                    CtorToInvoke, ConstructorArgs,
                    this.InvokedInstance)
                );
                
                // Return passed once we've built the CTOR correctly and invoked it
                return true;
            }
            catch (Exception InvokeCtorEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeCtorEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.CTOR_INVOKED, 
                    InvokeCtorEx,
                    ConstructorArgs)
                );

                // Return failed to invoke here
                return false;
            }
        }

        /// <summary>
        /// Attempts to set a class property or field value. If both are found nothing is set
        /// </summary>
        /// <param name="ClassMemberName">The name of the Field or Property which we wish to set</param>
        /// <returns>True if either the class property or field value found is set</returns>
        public TValueType GetValue<TValueType>(string ClassMemberName)
        {
            try
            {
                // Find if we've got the field or property info needed to invoke this routine
                bool HasField = this.TypeFieldInfos.ContainsKey(ClassMemberName);
                bool HasProperty = this.TypePropertyInfos.ContainsKey(ClassMemberName);
                if (!HasField && !HasProperty)
                    throw new NullReferenceException($"Error! No fields or properties matching {ClassMemberName} were found!");
                
                // Go Check the values for each information object found and return it
                TValueType FieldValue = default(TValueType);
                TValueType PropertyValue = default(TValueType);

                // Set the values now based on the results of our checks
                if (HasField)
                {
                    // Get the new field info object and make sure we can use it as a static field if needed
                    FieldInfo FieldToGet = this.TypeFieldInfos[ClassMemberName];
                    if (this.InvokedInstance == null && !FieldToGet.IsStatic)
                        throw new NullReferenceException("Error! Invoked instance was null!");

                    // Get the field value and invoke a passed event and move on to check for properties
                    FieldValue = FieldToGet.GetValue(this.InvokedInstance);
                    this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                        InvokerEventTypes.GET_FIELD,
                        FieldToGet, FieldValue)
                    );
                }
                if (HasProperty)
                {
                    // Get the new property info object and make sure we can use it as a static property if needed
                    PropertyInfo PropertyToGet = this.TypePropertyInfos[ClassMemberName];
                    if (this.InvokedInstance == null && !PropertyToGet.GetMethod.IsStatic)
                        throw new NullReferenceException("Error! Invoked instance was null!");

                    // Get the new property value and invoke a passed event
                    PropertyValue = PropertyToGet.GetValue(this.InvokedInstance);
                    this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                        InvokerEventTypes.GET_PROPERTY,
                        PropertyToGet, PropertyValue)
                    );
                }

                // Return based on the values pulled back from the instance object
                return FieldValue ?? PropertyValue ??
                    throw new NullReferenceException($"Error! No fields or properties matching {ClassMemberName} were found!");
            }
            catch (Exception InvokeMemberEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeMemberEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.GET_VALUE,
                    InvokeMemberEx, ClassMemberName)
                );

                // Return failed to invoke here
                return default;
            }
        }
        /// <summary>
        /// Pulls in the value for a field based on the name given and returns it
        /// </summary>
        /// <typeparam name="TValueType">The type of value the field should be returning out</typeparam>
        /// <param name="FieldName">The name of the field being pulled in</param>
        /// <returns>The value of the field pulled or null if nothing could be found</returns>
        public TValueType GetField<TValueType>(string FieldName)
        {
            try
            {
                // First find the field we would like to set and make sure we can
                if (!this.TypeFieldInfos.ContainsKey(FieldName))
                    throw new MissingMemberException($"Error! Could not find field named {FieldName}!");

                // Get the new field info object and make sure we can use it as a static field if needed
                FieldInfo FieldToGet = this.TypeFieldInfos[FieldName];
                if (this.InvokedInstance == null && !FieldToGet.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");

                // Get the field value and invoke a passed event and move on to check for properties
                TValueType FieldValue = FieldToGet.GetValue(this.InvokedInstance);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.GET_FIELD,
                    FieldToGet, FieldValue)
                );

                // Return the built field value
                return FieldValue;
            }
            catch (Exception InvokeFieldEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeFieldEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.GET_FIELD,
                    InvokeFieldEx, FieldName)
                );

                // Return failed to invoke here
                return default;
            }
        }
        /// <summary>
        /// Pulls in the value for a property based on the name given and returns it
        /// </summary>
        /// <typeparam name="TValueType">The type of value the property should be returning out</typeparam>
        /// <param name="PropertyName">The name of the property being pulled in</param>
        /// <returns>The value of the property pulled or null if nothing could be found</returns>
        public TValueType GetProperty<TValueType>(string PropertyName)
        {
            try
            {
                // First find the field we would like to set and make sure we can
                if (!this.TypePropertyInfos.ContainsKey(PropertyName))
                    throw new MissingMemberException($"Error! Could not find property named {PropertyName}!");

                // Get the new property info object and make sure we can use it as a static property if needed
                PropertyInfo PropertyToGet = this.TypePropertyInfos[PropertyName];
                if (this.InvokedInstance == null && !PropertyToGet.GetMethod.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");

                // Get the property value and invoke a passed event
                TValueType PropertyValue = PropertyToGet.GetValue(this.InvokedInstance);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.GET_PROPERTY,
                    PropertyToGet, PropertyValue)
                );

                // Return the built property value
                return PropertyValue;
            }
            catch (Exception InvokePropertyEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokePropertyEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.GET_PROPERTY,
                    InvokePropertyEx, PropertyName)
                );

                // Return default value for failed to invoke states
                return default; 
            }
        }

        /// <summary>
        /// Attempts to set a class property or field value. If both are found nothing is set
        /// </summary>
        /// <param name="ClassMemberName">The name of the Field or Property which we wish to set</param>
        /// <param name="ClassMemberValue">The value being stored into this field or property</param>
        /// <returns>True if either the class property or field value found is set</returns>
        public bool SetValue(string ClassMemberName, object ClassMemberValue)
        {
            try
            {
                // Find if we've got the field or property info needed to invoke this routine
                bool HasField = this.TypeFieldInfos.ContainsKey(ClassMemberName);
                bool HasProperty = this.TypePropertyInfos.ContainsKey(ClassMemberName);
                if (!HasField && !HasProperty)
                    throw new NullReferenceException($"Error! No fields or properties matching {ClassMemberName} were found!");

                // Set the values now based on the results of our checks
                if (HasField)
                {
                    // Get the new field info object and make sure we can use it as a static field if needed
                    FieldInfo FieldToSet = this.TypeFieldInfos[ClassMemberName];
                    if (this.InvokedInstance == null && !FieldToSet.IsStatic)
                        throw new NullReferenceException("Error! Invoked instance was null!");

                    // Invoke a passed event and move on to check for properties
                    FieldToSet.SetValue(this.InvokedInstance, ClassMemberValue);
                    this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                        InvokerEventTypes.SET_FIELD,
                        FieldToSet, ClassMemberValue)
                    );
                }
                if (HasProperty)
                {
                    // Apply the value into the property and exit out
                    PropertyInfo PropertyToSet = this.TypePropertyInfos[ClassMemberName];
                    if (this.InvokedInstance == null && !PropertyToSet.SetMethod.IsStatic)
                        throw new NullReferenceException("Error! Invoked instance was null!");

                    // Invoke a passed event and exit out of this routine
                    PropertyToSet.SetValue(this.InvokedInstance, ClassMemberValue);
                    this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                        InvokerEventTypes.SET_PROPERTY,
                        PropertyToSet, ClassMemberValue)
                    );
                }

                // Return true once our class value has been updated
                return true;
            }
            catch (Exception InvokeMemberEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeMemberEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.SET_VALUE, InvokeMemberEx, 
                    ClassMemberValue, ClassMemberValue)
                );

                // Return failed to invoke here
                return false;
            }
        }
        /// <summary>
        /// Sets a field value based on what's passed into this method
        /// </summary>
        /// <param name="FieldName">Name of the field to set</param>
        /// <param name="FieldValue">Value of the field to store</param>
        /// <returns>True if stored correctly, false if not</returns>
        public bool SetField(string FieldName, object FieldValue)
        {
            try
            {
                // First find the field we would like to set and make sure we can
                if (!this.TypeFieldInfos.ContainsKey(FieldName))
                    throw new MissingMemberException($"Error! Could not find field named {FieldName}!");

                // Get the new field info object and make sure we can use it as a static field if needed
                FieldInfo FieldToSet = this.TypeFieldInfos[FieldName];
                if (this.InvokedInstance == null && !FieldToSet.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");

                // Invoke a passed event and move on to check for properties
                FieldToSet.SetValue(this.InvokedInstance, FieldValue);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.SET_FIELD,
                    FieldToSet, FieldValue)
                );

                // Return true once our class value has been updated
                return true;
            }
            catch (Exception InvokeFieldEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeFieldEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.SET_FIELD, InvokeFieldEx,
                    FieldName, FieldValue)
                );

                // Return failed to invoke here
                return false;
            }
        }
        /// <summary>
        /// Sets a property value based on what's passed into this method
        /// </summary>
        /// <param name="PropertyName">Name of the property to set</param>
        /// <param name="PropertyValue">Value of the property to store</param>
        /// <returns>True if stored correctly, false if not</returns>
        public bool SetProperty(string PropertyName, object PropertyValue)
        {
            try
            {
                // First find the field we would like to set and make sure we can
                if (!this.TypePropertyInfos.ContainsKey(PropertyName))
                    throw new MissingMemberException($"Error! Could not find property named {PropertyName}!");

                // Apply the value into the property and exit out
                PropertyInfo PropertyToSet = this.TypePropertyInfos[PropertyName];
                if (this.InvokedInstance == null && !PropertyToSet.SetMethod.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");

                // Invoke a passed event and exit out of this routine
                PropertyToSet.SetValue(this.InvokedInstance, PropertyValue);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.SET_PROPERTY,
                    PropertyToSet, PropertyValue)
                );

                // Return true once our class value has been updated
                return true;
            }
            catch (Exception InvokePropertyEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokePropertyEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.SET_PROPERTY, InvokePropertyEx, 
                    PropertyName, PropertyValue)
                );

                // Return failed to invoke here
                return false;
            }
        }
        
        /// <summary>
        /// Attempts to invoke a new method from our instance object with the given arguments and signature
        /// </summary>
        /// <param name="MethodName">Friendly signature of the method</param>
        /// <param name="MethodArgs">Arguments to pass into the method</param>
        /// <returns>True if the method is invoked. False if it is not</returns>
        public bool InvokeMethod(string MethodName, params object[] MethodArgs)
        {
            try
            {
                // Find a method matching the signature requested now
                MethodInfo MethodToInvoke = this.TypeMethodInfos.FirstOrDefault(MethodInfoPair =>
                {
                    // Find the CTOR parameter objects and build a type string for the ars passed in
                    var MethodParams = MethodInfoPair.Value.GetParameters();
                    string ParamsTypeString =
                        $"{this.InvokedType.Name}" +
                        $"({string.Join(",", MethodParams.Select(ParamObj => ParamObj.GetType().Name))})";

                    // Find the needed method object here based on our signature string or the arg counts
                    if (MethodInfoPair.Key == ParamsTypeString) return true;
                    if (!MethodInfoPair.Key.Contains(MethodName)) return false;
                    if (MethodParams.Length == MethodArgs.Length) return true;

                    // If none of these conditions are met, return false
                    return false;
                }).Value;

                // If the method is null or the invoker instance is not built for non static methods, throw an exception now
                if (MethodToInvoke == null)
                    throw new MissingMemberException($"Error! Could not find a member with signature {MethodName}!");
                if (this.InvokedInstance == null && !MethodToInvoke.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");
                
                // Invoke the method and fire off a new event for passed output
                MethodToInvoke.Invoke(this.InvokedInstance, MethodArgs);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.METHOD_INVOKED,
                    MethodToInvoke, MethodArgs)
                );

                // Return the invoke routine passed at this point
                return true;
            }
            catch (Exception InvokeMethodEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeMethodEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.METHOD_INVOKED,
                    InvokeMethodEx, MethodArgs)
                );

                // Return failed to invoke here
                return false;
            }
        }
        /// <summary>
        /// Attempts to invoke a new method from our instance object with the given arguments and signature
        /// </summary>
        /// <typeparam name="TValueType">The type of value the property should be returning out</typeparam>
        /// <param name="MethodName">Friendly signature of the method</param>
        /// <param name="MethodArgs">Arguments to pass into the method</param>
        /// <returns>The value of the method invoked if it has a return type</returns>
        public TValueType InvokeMethod<TValueType>(string MethodName, params object[] MethodArgs)
        {
            try
            {
                // Find a method matching the signature requested now
                MethodInfo MethodToInvoke = this.TypeMethodInfos
                    .Where(InfoPair =>
                        InfoPair.Key.Contains(MethodName) &&
                        InfoPair.Value.GetParameters().Length == MethodArgs.Length)
                    .Select(InfoPair => InfoPair.Value)
                    .FirstOrDefault();

                // If the method is null or the invoker instance is not built for non static methods, throw an exception now
                if (MethodToInvoke == null)
                    throw new MissingMemberException($"Error! Could not find a member with signature {MethodName}!");
                if (this.InvokedInstance == null && !MethodToInvoke.IsStatic)
                    throw new NullReferenceException("Error! Invoked instance was null!");

                // Invoke the method and fire off a new event for passed output
                TValueType MethodResult = MethodToInvoke.Invoke(this.InvokedInstance, MethodArgs);
                this.InvokeSuccess?.Invoke(this, new InvokerSuccessEventArgs(
                    InvokerEventTypes.METHOD_INVOKED,
                    MethodToInvoke, MethodArgs, MethodResult)
                );

                // Return the invoke routine passed at this point
                return MethodResult;
            }
            catch (Exception InvokeMethodEx)
            {
                // Build new event args for the failure and raise them if needed
                if (this.InvokeFailure == null) throw InvokeMethodEx;
                this.InvokeFailure.Invoke(this, new InvokerFailureEventArgs(
                    InvokerEventTypes.METHOD_INVOKED,
                    InvokeMethodEx, MethodArgs)
                );

                // Return default value for failed to invoke states
                return default;
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------------------------------------------------------------

        #region Private Type Reflecting Methods

        /// <summary>
        /// Builds all the type information for the invoker object being used and sets up a default failure event handler
        /// </summary>
        private void _initializeInvokerTypes()
        {
            // Store the fields, methods, properties, and constructors for this class now
            this._typeFieldInfos = this._buildTypeFields(this.SearchFlags);
            this._typeMethodInfos = this._buildTypeMethods(this.SearchFlags);
            this._typePropertyInfos = this._buildTypeProperties(this.SearchFlags);
            this._typeConstructorInfos = this._buildTypeConstructors(this.SearchFlags);

            // Hook in a new failure event here for the invoker instance
            this.InvokeFailure += (SendingObject, InvokeFailureArgs) =>
            {
                // On invoker failures, we just want to throw a new exception since something went wrong
                if (InvokeFailureArgs.InvokePassed) return;
                if (SendingObject is not InternalInvoker SendingInvoker) return;

                // Throw the new failure found and invoke a test method failed routine
                string EventTypeString = InvokeFailureArgs.EventType.ToString();
                string FullSendingTypeName = SendingInvoker.InvokedType.FullName;
                string FailureMessage = $"Error! Failed to perform a {EventTypeString} event on type {FullSendingTypeName}!";
                
                // Log out the thrown exception from our invoke routine and exit out
                Exception InvokerEx = InvokeFailureArgs.InvokerException.InnerException ?? InvokeFailureArgs.InvokerException;
                LoggerTestHelpers.AssertException(FailureMessage, InvokerEx, false);
            };
        }

        /// <summary>
        /// Finds and stores all field objects for our invoked type requested
        /// </summary>
        /// <param name="SearchFlags">Flags to control the reflection routine for finding fields</param>
        /// <returns>A dictionary holding all the field names and a field info for each found field</returns>
        private Dictionary<string, FieldInfo> _buildTypeFields(BindingFlags SearchFlags = BindingFlags.Default)
        {
            // Configure flags for finding type field info values now
            if (SearchFlags == BindingFlags.Default && this.SearchFlags != BindingFlags.Default) 
                SearchFlags = this.SearchFlags;

            // Reflect the type provided and store all field objects now
            FieldInfo[] FieldsReflected = this.InvokedType.GetFields(SearchFlags).ToArray();
            Dictionary<string, FieldInfo> FieldInfoDictionary = FieldsReflected
                .Select(FieldObj => new Tuple<string, FieldInfo>(FieldObj.Name, FieldObj))
                .ToDictionary(TupleSet => TupleSet.Item1, TupleSet => TupleSet.Item2);

            // Return out the built dictionary object here
            return FieldInfoDictionary;
        }
        /// <summary>
        /// Finds and stores all method objects for our invoked type requested
        /// </summary>
        /// <param name="SearchFlags">Flags to control the reflection routine for finding methods</param>
        /// <returns>A dictionary holding all the method names and a method info for each found method</returns>
        private Dictionary<string, MethodInfo> _buildTypeMethods(BindingFlags SearchFlags = BindingFlags.Default)
        {
            // Configure flags for finding type method info values now
            if (SearchFlags == BindingFlags.Default && this.SearchFlags != BindingFlags.Default)
                SearchFlags = this.SearchFlags;

            // Reflect the type provided and store all method objects now
            MethodInfo[] MethodsReflected = this.InvokedType.GetMethods(SearchFlags).ToArray();
            Dictionary<string, MethodInfo> MethodInfoDictionary = MethodsReflected
                .Select(MethodObj =>
                {
                    // Get the types of the method args and build the string name for it 
                    string MethodNameBuilt = 
                        $"{MethodObj.Name}" +
                        $"({string.Join(",", MethodObj.GetParameters().Select(ParamObj => ParamObj.ParameterType.Name))})";

                    // Return the new built method name and method info objects
                    return new Tuple<string, MethodInfo>(MethodNameBuilt, MethodObj);
                }).GroupBy(TupleSet => TupleSet.Item1).Select(TupleGroup => TupleGroup.First())
                .ToDictionary(TupleSet => TupleSet.Item1, TupleSet => TupleSet.Item2);

            // Return out the built dictionary object here
            return MethodInfoDictionary;
        }
        /// <summary>
        /// Finds and stores all property objects for our invoked type requested
        /// </summary>
        /// <param name="SearchFlags">Flags to control the reflection routine for finding properties</param>
        /// <returns>A dictionary holding all the field names and a field info for each found property</returns>
        private Dictionary<string, PropertyInfo> _buildTypeProperties(BindingFlags SearchFlags = BindingFlags.Default)
        {
            // Configure flags for finding type property info values now
            if (SearchFlags == BindingFlags.Default && this.SearchFlags != BindingFlags.Default)
                SearchFlags = this.SearchFlags;

            // Reflect the type provided and store all property objects now
            PropertyInfo[] PropertiesReflected = this.InvokedType.GetProperties(SearchFlags).ToArray();
            Dictionary<string, PropertyInfo> PropertyInfoDictionary = PropertiesReflected
                .Select(PropertyObj => new Tuple<string, PropertyInfo>(PropertyObj.Name, PropertyObj))
                .ToDictionary(TupleSet => TupleSet.Item1, TupleSet => TupleSet.Item2);

            // Return out the built dictionary object here
            return PropertyInfoDictionary;
        }
        /// <summary>
        /// Finds and stores all constructor objects for our invoked type requested
        /// </summary>
        /// <param name="SearchFlags">Flags to control the reflection routine for finding constructors</param>
        /// <returns>A dictionary holding all the method names and a method info for each found constructor</returns>
        private Dictionary<string, ConstructorInfo> _buildTypeConstructors(BindingFlags SearchFlags = BindingFlags.Default)
        {
            // Configure flags for finding type constructor info values now
            if (SearchFlags == BindingFlags.Default && this.SearchFlags != BindingFlags.Default)
                SearchFlags = this.SearchFlags;

            // Reflect the type provided and store all constructor objects now
            ConstructorInfo[] ConstructorsReflected = this.InvokedType.GetConstructors(SearchFlags).ToArray();
            Dictionary<string, ConstructorInfo> MethodInfoDictionary = ConstructorsReflected
                .Select(ConstructorObj =>
                {
                    // Get the types of the constructor args and build the string name for it 
                    string ConstructorNameBuilt =
                        $"{ConstructorObj.DeclaringType.Name}" +
                        $"({string.Join(",", ConstructorObj.GetParameters().Select(ParamObj => ParamObj.ParameterType.Name))})";

                    // Return the new built constructor name and constructor info objects
                    return new Tuple<string, ConstructorInfo>(ConstructorNameBuilt, ConstructorObj);
                }).GroupBy(TupleSet => TupleSet.Item1).Select(TupleGroup => TupleGroup.First())
                .ToDictionary(TupleSet => TupleSet.Item1, TupleSet => TupleSet.Item2);

            // Return out the built dictionary object here
            return MethodInfoDictionary;
        }

        #endregion
    }
}
