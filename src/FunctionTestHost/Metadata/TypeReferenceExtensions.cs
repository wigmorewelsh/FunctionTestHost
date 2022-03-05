
using System;

namespace Microsoft.Azure.Functions.Worker.Sdk;

internal static class TypeReferenceExtensions
{
    public static string GetReflectionFullName(this Type typeRef)
    {
        return typeRef.FullName.Replace('/', '+');
    }
}