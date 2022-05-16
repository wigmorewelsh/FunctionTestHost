﻿using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

namespace FunctionTestHost.Tasks;

/// <summary>
/// Generate a JSON file mapping assemblies to content root paths.
/// </summary>
public class GenerateFunctionTestManifestTask : Task
{
    /// <summary>
    /// The path to output the manifest file to.
    /// </summary>
    [Required]
    public string ManifestPath { get; set; }

    /// <summary>
    /// A list of content root paths and assembly names to generate the
    /// manifest from.
    /// </summary>
    [Required]
    public ITaskItem[] Projects { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        using var fileStream = File.Create(ManifestPath);
        var output = new Dictionary<string, string>();

        foreach (var project in Projects)
        {
            var contentRoot = project.GetMetadata("ContentRoot");
            var assemblyName = project.GetMetadata("Identity");
            output[assemblyName] = contentRoot;
        }

        var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true
        });
        using var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, ownsStream: false, indent: true);
        serializer.WriteObject(writer, output);

        return !Log.HasLoggedErrors;
    }
}