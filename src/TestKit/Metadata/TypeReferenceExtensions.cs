
using System;

namespace TestKit.Metadata;

internal static class TypeReferenceExtensions
{
    public static string GetReflectionFullName(this Type typeRef)
    {
        return typeRef.FullName.Replace('/', '+');
    }
}