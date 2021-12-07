// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    public static class CustomAttributeExtensions
    {
        public static IDictionary<string, object> GetAllDefinedProperties(this CustomAttributeData attribute)
        {
            var properties = new Dictionary<string, object>();
            // To avoid needing to instantiate any types, assume that the constructor
            // argument names are equal to property names.
            LoadDefaultProperties(properties, attribute);
            LoadConstructorArguments(properties, attribute);
            LoadDefinedProperties(properties, attribute);

            return properties;
        }

        private static IEnumerable<(string Name, CustomAttributeTypedArgument?)> GetDefaultValues(
            this CustomAttributeData attribute)
        {
            return attribute.AttributeType
                .GetProperties()
                .Select(p => (p.Name, p.CustomAttributes
                    .SingleOrDefault(attr => string.Equals(attr.AttributeType.FullName,
                        Constants.DefaultValueAttributeType, StringComparison.Ordinal))
                    ?.ConstructorArguments.SingleOrDefault()))
                .Where(t => t.Item2 is not null);
        }

        private static void LoadDefaultProperties(IDictionary<string, object> properties, CustomAttributeData attribute)
        {
            var propertyDefaults = attribute.GetDefaultValues();

            foreach (var propertyDefault in propertyDefaults)
            {
                if (propertyDefault.Item2 is not null)
                {
                    properties[propertyDefault.Item1] = propertyDefault.Item2.Value.Value;
                }
            }
        }

        private static void LoadConstructorArguments(IDictionary<string, object> properties,
            CustomAttributeData attribute)
        {
            var constructorParams = attribute.Constructor.GetParameters();
            for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                var arg = attribute.ConstructorArguments[i];
                var param = constructorParams[i];

                string? paramName = param?.Name;
                object? paramValue = arg.Value;

                if (paramName is null || paramValue is null)
                {
                    continue;
                }

                paramValue = GetEnrichedValue(param!.ParameterType, paramValue);
                properties[paramName] = paramValue!;
            }
        }

        private static void LoadDefinedProperties(IDictionary<string, object> properties, CustomAttributeData attribute)
        {
            foreach (System.Reflection.CustomAttributeNamedArgument property in attribute.NamedArguments)
            {
                object? propVal = property.TypedValue.Value;
                string? propName = property.MemberName;

                if (propVal is null || propName is null)
                {
                    continue;
                }

                propVal = GetEnrichedValue(property.TypedValue.ArgumentType, propVal);

                properties[propName] = propVal!;
            }
        }

        private static object? GetEnrichedValue(Type type, object value)
        {
            if (TryGetEnumName(type, value, out string? enumName))
            {
                return enumName;
            }
            // TODO: Fixme
            // else if (type.IsArray)
            // {
            //     var arrayValue = value as IEnumerable<CustomAttributeArgument>;
            //     return arrayValue.Select(p => p.Value).ToArray();
            // }
            else
            {
                return value;
            }
        }

        private static bool TryGetEnumName(Type typeDef, object enumValue, out string? enumName)
        {
            if (typeDef.IsEnum)
            {
                enumName = typeDef.GetEnumName(enumValue);
                return true;
            }

            enumName = null;
            return false;
        }
    }

    // Copy of FunctionMetadata, but using internal type to simplify dependencies.
}