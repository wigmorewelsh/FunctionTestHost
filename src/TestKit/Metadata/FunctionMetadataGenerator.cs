using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AzureFunctionsRpcMessages;

namespace TestKit.Metadata;

internal class FunctionMetadataGenerator
{
    // TODO: Verify that we don't need to allow
    // same extensions of different versions. Picking the last version for now.
    // We can also just add all the versions of extensions and then let the
    // build pick the one it likes.
    private readonly IDictionary<string, string> _extensions;

    public FunctionMetadataGenerator()
    {
        _extensions = new Dictionary<string, string>();
    }

    public IDictionary<string, string> Extensions
    {
        get { return _extensions; }
    }

    public IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadataWithReferences(Assembly assemblyPath)
    {
        var functions = new List<SdkFunctionMetadata>();

        HashSet<Assembly> assemblies = new();

        string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

        var paths = new List<string>(runtimeAssemblies);
        string[] applicationAssemblies = Directory.GetFiles(Path.GetDirectoryName(assemblyPath.Location), "*.dll");
        paths.AddRange(applicationAssemblies);

        var resolver = new PathAssemblyResolver(paths);
        
        using var mlc = new MetadataLoadContext(resolver);
        
        
        void ScanAssembly(Assembly assemblyPath)
        {
            functions.AddRange(GenerateFunctionMetadata(assemblyPath));

            foreach (var path in assemblyPath.GetReferencedAssemblies())
            {
                if(path.Name == null) continue;
                if(path.Name.StartsWith("System")) continue;
                if(path.Name.StartsWith("Microsoft")) continue;

                var assembly = mlc.LoadFromAssemblyName(path.Name);
                if (assemblies.Contains(assembly)) continue;

                assemblies.Add(assembly);
                functions.AddRange(GenerateFunctionMetadata(assembly));

                ScanAssembly(assembly);
            }
        }

        ScanAssembly(assemblyPath);

        return functions;
    }



    public IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(Assembly module)
    {
        var functions = new List<SdkFunctionMetadata>();

        foreach (Type type in module.GetTypes())
        {
            var functionsResult = GenerateFunctionMetadata(type).ToArray();

            functions.AddRange(functionsResult);
        }

        return functions;
    }

    internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(Type type)
    {
        var functions = new List<SdkFunctionMetadata>();

        foreach (MethodInfo method in type.GetMethods())
        {
            AddFunctionMetadataIfFunction(functions, method);
        }

        return functions;
    }

    private void AddFunctionMetadataIfFunction(IList<SdkFunctionMetadata> functions, MethodInfo method)
    {
        if (TryCreateFunctionMetadata(method, out SdkFunctionMetadata? metadata)
            && metadata != null)
        {
            try
            {
                var allBindings = CreateBindingMetadataAndAddExtensions(method);


                foreach (var binding in allBindings)
                {
                    metadata.Bindings.Add(binding);
                }

                functions.Add(metadata);
            }
            catch (FunctionsMetadataGenerationException ex)
            {
                throw new FunctionsMetadataGenerationException(
                    $"Failed to generate medata for function '{metadata.Name}' (method '{method.Name}'): {ex.Message}");
            }
        }
    }

    private bool TryCreateFunctionMetadata(MethodInfo method, out SdkFunctionMetadata? function)
    {
        function = null;

        foreach (CustomAttributeData attribute in method.GetCustomAttributesData())
        {
            if (string.Equals(attribute.AttributeType.FullName, Constants.FunctionNameType,
                    StringComparison.Ordinal))
            {
                string functionName = attribute.ConstructorArguments.SingleOrDefault().Value.ToString();

                if (string.IsNullOrEmpty(functionName))
                {
                    continue;
                }

                Type declaringType = method.DeclaringType;

                string actualMethodName = method.Name;
                string declaringTypeName = declaringType.FullName;
                string assemblyName = declaringType.Module.Assembly.GetName().Name;

                function = CreateSdkFunctionMetadata(functionName, actualMethodName, declaringTypeName, assemblyName);

                return true;
            }
        }

        return false;
    }

    private static SdkFunctionMetadata CreateSdkFunctionMetadata(string functionName, string actualMethodName,
        string declaringTypeName, string assemblyName)
    {
        var function = new SdkFunctionMetadata
        {
            Name = functionName,
            ScriptFile = $"{assemblyName}.dll",
            EntryPoint = $"{declaringTypeName}.{actualMethodName}",
            Language = "dotnet-isolated",
            Properties =
            {
                ["IsCodeless"] = false
            }
        };


        return function;
    }

    private List<IDictionary<string, object>> CreateBindingMetadataAndAddExtensions(MethodInfo method)
    {
        var bindingMetadata = new List<IDictionary<string, object>>();

        AddInputTriggerBindingsAndExtensions(bindingMetadata, method);
        AddOutputBindingsAndExtensions(bindingMetadata, method);

        return bindingMetadata;
    }

    private void AddOutputBindingsAndExtensions(List<IDictionary<string, object>> bindingMetadata, MethodInfo method)
    {
        if (!TryAddOutputBindingFromMethod(bindingMetadata, method))
        {
            AddOutputBindingsFromReturnType(bindingMetadata, method);
        }
    }

    private void AddOutputBindingsFromReturnType(List<IDictionary<string, object>> bindingMetadata, MethodInfo method)
    {
        Type? returnType = GetTaskElementType(method.ReturnType);

        if (returnType is not null &&
            !string.Equals(returnType.FullName, Constants.VoidType, StringComparison.Ordinal))
        {
            if (string.Equals(returnType.FullName, Constants.HttpResponseType, StringComparison.Ordinal))
            {
                AddHttpOutputBinding(bindingMetadata, Constants.ReturnBindingName);
            }
            else
            {
                // TypeDefinition returnDefinition = returnType.Resolve()
                //                                   ?? throw new FunctionsMetadataGenerationException(
                //                                       $"Couldn't find the type definition '{returnType}' for method '{method.Name}'");

                bool hasOutputModel = TryAddOutputBindingsFromProperties(bindingMetadata, returnType);

                // Special handling for HTTP results using POCOs/Types other
                // than HttpResponseData. We should improve this to expand this
                // support to other triggers without special handling
                if (!hasOutputModel && bindingMetadata.Any(d => IsHttpTrigger(d)))
                {
                    AddHttpOutputBinding(bindingMetadata, Constants.ReturnBindingName);
                }
            }
        }
    }

    private static bool IsHttpTrigger(IDictionary<string, object> bindingMetadata)
    {
        return bindingMetadata.Any(kvp => string.Equals(kvp.Key, "Type", StringComparison.Ordinal)
                                          && string.Equals(kvp.Value?.ToString(), Constants.HttpTriggerBindingType,
                                              StringComparison.Ordinal));
    }

    private bool TryAddOutputBindingsFromProperties(List<IDictionary<string, object>> bindingMetadata,
        Type typeDefinition)
    {
        bool foundHttpOutput = false;
        int beforeCount = bindingMetadata.Count;

        foreach (PropertyInfo property in typeDefinition.GetProperties())
        {
            if (string.Equals(property.PropertyType.FullName, Constants.HttpResponseType, StringComparison.Ordinal))
            {
                if (foundHttpOutput)
                {
                    throw new FunctionsMetadataGenerationException(
                        $"Found multiple public properties with type '{Constants.HttpResponseType}' defined in output type '{typeDefinition.FullName}'. " +
                        $"Only one HTTP response binding type is supported in your return type definition.");
                }

                foundHttpOutput = true;
                AddHttpOutputBinding(bindingMetadata, property.Name);
                continue;
            }

            AddOutputBindingFromProperty(bindingMetadata, property, typeDefinition.FullName);
        }

        return bindingMetadata.Count > beforeCount;
    }

    private void AddOutputBindingFromProperty(List<IDictionary<string, object>> bindingMetadata, PropertyInfo property,
        string typeName)
    {
        bool foundOutputAttribute = false;

        foreach (CustomAttributeData propertyAttribute in property.GetCustomAttributesData())
        {
            if (IsOutputBindingType(propertyAttribute))
            {
                if (foundOutputAttribute)
                {
                    throw new FunctionsMetadataGenerationException(
                        $"Found multiple output attributes on property '{property.Name}' defined in the function return type '{typeName}'. " +
                        $"Only one output binding attribute is is supported on a property.");
                }

                foundOutputAttribute = true;

                // tofix
                AddOutputBindingMetadata(bindingMetadata, propertyAttribute, property.PropertyType, property.Name);
                AddExtensionInfo(_extensions, propertyAttribute);
            }
        }
    }

    private bool TryAddOutputBindingFromMethod(List<IDictionary<string, object>> bindingMetadata, MethodInfo method)
    {
        bool foundBinding = false;

        foreach (CustomAttributeData methodAttribute in method.GetCustomAttributesData())
        {
            if (IsOutputBindingType(methodAttribute))
            {
                if (foundBinding)
                {
                    throw new FunctionsMetadataGenerationException(
                        $"Found multiple Output bindings on method '{method.Name}'. " +
                        "Please use an encapsulation to define the bindings in properties. For more information: https://aka.ms/dotnet-worker-poco-binding.");
                }

                AddOutputBindingMetadata(bindingMetadata, methodAttribute, methodAttribute.AttributeType,
                    Constants.ReturnBindingName);
                AddExtensionInfo(_extensions, methodAttribute);

                foundBinding = true;
            }
        }

        return foundBinding;
    }

    private void AddInputTriggerBindingsAndExtensions(List<IDictionary<string, object>> bindingMetadata, MethodInfo method)
    {
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            foreach (CustomAttributeData parameterAttribute in parameter.GetCustomAttributesData())
            {
                if (IsFunctionBindingType(parameterAttribute))
                {
                    AddBindingMetadata(bindingMetadata, parameterAttribute, parameter.ParameterType,
                        parameter.Name);
                    AddExtensionInfo(_extensions, parameterAttribute);
                }
            }
        }
    }

    private static Type? GetTaskElementType(Type typeReference)
    {
        if (typeReference is null ||
            string.Equals(typeReference.FullName, Constants.TaskType, StringComparison.Ordinal))
        {
            return null;
        }

        if (typeReference.IsGenericType
            && string.Equals(typeReference.GetGenericTypeDefinition().FullName, Constants.TaskGenericType,
                StringComparison.Ordinal))
        {
            // T from Task<T>
            return typeReference.GetGenericArguments()[0];
        }
        else
        {
            return typeReference;
        }
    }

    private static void AddOutputBindingMetadata(List<IDictionary<string, object>> bindingMetadata, CustomAttributeData attribute,
        Type parameterType, string? name = null)
    {
        AddBindingMetadata(bindingMetadata, attribute, parameterType, parameterName: name);
    }

    private static void AddBindingMetadata(List<IDictionary<string, object>> bindingMetadata, CustomAttributeData attribute,
        Type parameterType, string? parameterName)
    {
        string bindingType = GetBindingType(attribute);

        var binding =
            BuildBindingMetadataFromAttribute(attribute, bindingType, parameterType, parameterName);
        bindingMetadata.Add(binding);
    }

    private static Dictionary<string, object> BuildBindingMetadataFromAttribute(CustomAttributeData attribute, string bindingType,
        Type parameterType, string? parameterName)
    {
        var binding = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(parameterName))
        {
            binding["Name"] = parameterName!;
        }

        binding["Type"] = bindingType;
        binding["Direction"] = GetBindingDirection(attribute);

        // Is string parameter type
        if (IsStringType(parameterType.FullName))
        {
            binding["DataType"] = "String";
        }
        // Is binary parameter type
        else if (IsBinaryType(parameterType.FullName))
        {
            binding["DataType"] = "Binary";
        }

        foreach (var property in attribute.GetAllDefinedProperties())
        {
            binding.Add(property.Key, property.Value);
        }

        // Determine if we should set the "Cardinality" property based on
        // the presence of "IsBatched." This is a property that is from the
        // attributes that implement the ISupportCardinality interface.
        //
        // Note that we are directly looking for "IsBatched" today while we
        // are not actually instantiating the Attribute type and instead relying
        // on type inspection via Mono.Cecil.
        // TODO: Do not hard-code "IsBatched" as the property to set cardinality.
        // We should rely on the interface
        //
        // Conversion rule
        //     "IsBatched": true => "Cardinality": "Many"
        //     "IsBatched": false => "Cardinality": "One"
        if (binding.TryGetValue(Constants.IsBatchedKey, out object isBatchedValue)
            && isBatchedValue is bool isBatched)
        {
            // Batching set to true
            if (isBatched)
            {
                binding["Cardinality"] = "Many";
                // Throw if parameter type is *definitely* not a collection type.
                // Note that this logic doesn't dictate what we can/can't do, and
                // we can be more restrictive in the future because today some
                // scenarios result in runtime failures.
                if (IsIterableCollection(parameterType, out BindingInfo.Types.DataType dataType))
                {
                    if (dataType.Equals(BindingInfo.Types.DataType.String))
                    {
                        binding["DataType"] = "String";
                    }
                    else if (dataType.Equals(BindingInfo.Types.DataType.Binary))
                    {
                        binding["DataType"] = "Binary";
                    }
                }
                else
                {
                    throw new FunctionsMetadataGenerationException(
                        "Function is configured to process events in batches but parameter type is not iterable. " +
                        $"Change parameter named '{parameterName}' to be an IEnumerable type or set 'IsBatched' to false on your '{attribute.AttributeType.Name.Replace("Attribute", "")}' attribute.");
                }
            }
            // Batching set to false
            else
            {
                binding["Cardinality"] = "One";
            }

            binding.Remove(Constants.IsBatchedKey);
        }

        return binding;
    }

    private static bool IsIterableCollection(Type type, out BindingInfo.Types.DataType dataType)
    {
        // Array and not byte array
        bool isArray = type.IsArray &&
                       !string.Equals(type.FullName, Constants.ByteArrayType, StringComparison.Ordinal);
        if (isArray)
        {
            if (type is Type typeSpecification)
            {
                dataType = GetDataTypeFromType(typeSpecification.GetElementType().FullName);
                return true;
            }
        }

        bool isMappingEnumerable = IsOrDerivedFrom(type, Constants.IEnumerableOfKeyValuePair)
                                   || IsOrDerivedFrom(type, Constants.LookupGenericType)
                                   || IsOrDerivedFrom(type, Constants.DictionaryGenericType);
        if (isMappingEnumerable)
        {
            dataType = BindingInfo.Types.DataType.Undefined;
            return false;
        }

        // IEnumerable and not string or dictionary
        bool isEnumerableOfT = IsOrDerivedFrom(type, Constants.IEnumerableOfT);
        bool isEnumerableCollection =
            !IsStringType(type.FullName)
            && (IsOrDerivedFrom(type, Constants.IEnumerableType)
                || IsOrDerivedFrom(type, Constants.IEnumerableGenericType)
                || isEnumerableOfT);
        if (isEnumerableCollection)
        {
            dataType = BindingInfo.Types.DataType.Undefined;
            if (IsOrDerivedFrom(type, Constants.IEnumerableOfStringType))
            {
                dataType = BindingInfo.Types.DataType.String;
            }
            else if (IsOrDerivedFrom(type, Constants.IEnumerableOfBinaryType))
            {
                dataType = BindingInfo.Types.DataType.Binary;
            }
            else if (isEnumerableOfT)
            {
                // Find real type that "T" in IEnumerable<T> resolves to
                string typeName = ResolveIEnumerableOfTType(type, new Dictionary<string, string>()) ?? string.Empty;
                dataType = GetDataTypeFromType(typeName);
            }

            return true;
        }

        dataType = BindingInfo.Types.DataType.Undefined;
        return false;
    }

    private static bool IsOrDerivedFrom(Type type, string interfaceFullName)
    {
        bool isType = string.Equals(type.FullName, interfaceFullName, StringComparison.Ordinal);
        Type definition = type;
        return isType || IsDerivedFrom(definition, interfaceFullName);
    }

    private static bool IsDerivedFrom(Type definition, string interfaceFullName)
    {
        var isType = string.Equals(definition.FullName, interfaceFullName, StringComparison.Ordinal);
        return isType || HasInterface(definition, interfaceFullName) || IsSubclassOf(definition, interfaceFullName);
    }

    private static bool HasInterface(Type definition, string interfaceFullName)
    {
        return definition.GetInterfaces().Any(i =>
            string.Equals(i.FullName, interfaceFullName, StringComparison.Ordinal));
    }

    private static bool IsSubclassOf(Type definition, string interfaceFullName)
    {
        if (definition.BaseType is null)
        {
            return false;
        }

        Type baseType = definition.BaseType;
        return IsDerivedFrom(baseType, interfaceFullName);
    }

    private static string? ResolveIEnumerableOfTType(Type type, Dictionary<string, string> foundMapping)
    {
        // Base case:
        // We are at IEnumerable<T> and want to return the most recent resolution of T
        // (Most recent is relative to IEnumerable<T>)
        if (string.Equals(type.FullName, Constants.IEnumerableOfT, StringComparison.Ordinal))
        {
            if (foundMapping.TryGetValue(Constants.GenericIEnumerableArgumentName, out string typeName))
            {
                return typeName;
            }

            return null;
        }

        Type definition = type;
        if (definition.IsGenericType)
        {
            for (int i = 0; i < definition.GetGenericArguments().Count(); i++)
            {
                // Check this is correct
                string name = definition.GetGenericArguments().ElementAt(i).Name;
                string resolvedName = definition.GetGenericArguments().ElementAt(i).FullName;

                if (foundMapping.TryGetValue(name, out string firstType))
                {
                    foundMapping.Remove(name);
                    foundMapping.Add(resolvedName, firstType);
                }
                else
                {
                    foundMapping.Add(resolvedName, name);
                }
            }
        }

        return definition.GetInterfaces()
                   .Select(i => ResolveIEnumerableOfTType(i, foundMapping))
                   .FirstOrDefault(name => name is not null)
               ?? ResolveIEnumerableOfTType(definition.BaseType, foundMapping);
    }

    private static BindingInfo.Types.DataType GetDataTypeFromType(string fullName)
    {
        if (IsStringType(fullName))
        {
            return BindingInfo.Types.DataType.String;
        }
        else if (IsBinaryType(fullName))
        {
            return BindingInfo.Types.DataType.Binary;
        }

        return BindingInfo.Types.DataType.Undefined;
    }

    private static bool IsStringType(string fullName)
    {
        return string.Equals(fullName, Constants.StringType, StringComparison.Ordinal);
    }

    private static bool IsBinaryType(string fullName)
    {
        return string.Equals(fullName, Constants.ByteArrayType, StringComparison.Ordinal)
               || string.Equals(fullName, Constants.ReadOnlyMemoryOfBytes, StringComparison.Ordinal);
    }

    private static string GetBindingType(CustomAttributeData attribute)
    {
        var attributeType = attribute.AttributeType.Name;

        // TODO: Should "webjob type" be a property of the "worker types" and come from there?
        return attributeType
            .Replace("TriggerAttribute", "Trigger")
            .Replace("InputAttribute", string.Empty)
            .Replace("OutputAttribute", string.Empty);
    }

    private static void AddHttpOutputBinding(List<IDictionary<string, object>> bindingMetadata, string name)
    {
        var returnBinding = new Dictionary<string, object>();
        returnBinding["Name"] = name;
        returnBinding["Type"] = "http";
        returnBinding["Direction"] = "Out";

        bindingMetadata.Add(returnBinding);
    }

    private static void AddExtensionInfo(IDictionary<string, string> extensions, CustomAttributeData attribute)
    {
        Assembly extensionAssemblyDefinition = attribute.AttributeType.Module.Assembly;

        foreach (var assemblyAttribute in extensionAssemblyDefinition.CustomAttributes)
        {
            if (string.Equals(assemblyAttribute.AttributeType.FullName, Constants.ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                string extensionName = assemblyAttribute.ConstructorArguments[0].Value.ToString();
                string extensionVersion = assemblyAttribute.ConstructorArguments[1].Value.ToString();

                extensions[extensionName] = extensionVersion;

                // Only 1 extension per library
                return;
            }
        }
    }

    private static string GetBindingDirection(CustomAttributeData attribute)
    {
        if (IsOutputBindingType(attribute))
        {
            return "Out";
        }

        return "In";
    }

    private static bool IsOutputBindingType(CustomAttributeData attribute)
    {
        return TryGetBaseAttributeType(attribute, Constants.OutputBindingType, out _);
    }

    private static bool IsFunctionBindingType(CustomAttributeData attribute)
    {
        return TryGetBaseAttributeType(attribute, Constants.BindingType, out _);
    }

    private static bool TryGetBaseAttributeType(CustomAttributeData attribute, string baseType,
        out Type? baseTypeRef)
    {
        baseTypeRef = attribute.AttributeType?.BaseType;

        while (baseTypeRef != null)
        {
            if (string.Equals(baseTypeRef.FullName, baseType, StringComparison.Ordinal))
            {
                return true;
            }

            baseTypeRef = baseTypeRef.BaseType;
        }

        return false;
    }
}