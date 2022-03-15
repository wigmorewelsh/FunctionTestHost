using System;
using System.Runtime.Serialization;

namespace FunctionTestHost.Metadata;

[Serializable]
internal class FunctionsMetadataGenerationException: Exception
{
    public FunctionsMetadataGenerationException() { }

    internal FunctionsMetadataGenerationException(string message): base(message) { }

    internal FunctionsMetadataGenerationException(string message, Exception innerException) : base(message, innerException) { }
        
    protected FunctionsMetadataGenerationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}