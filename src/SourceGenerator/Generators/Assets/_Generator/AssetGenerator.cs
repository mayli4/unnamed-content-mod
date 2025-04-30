﻿using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using SourceGenerator.Utilities;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace SourceGenerator.Generators.Assets;

// ReSharper disable VariableHidesOuterVariable

[Generator(LanguageNames.CSharp)]
internal sealed class AssetGenerator : IIncrementalGenerator {
    private const string tool_version = "1.0";
    private const string image_extension = ".png";
    private const string effect_extension = ".fxc";

    private static readonly string[] supported_extensions = new[] {
        image_extension,
        effect_extension,
    };

    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context) {
        var modName = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName);

        var assetRootFolder = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith("AssetRoot.txt"))
            .Collect()
            .Select(static (files, _) =>
            {
                var directory = Path.GetDirectoryName(files.FirstOrDefault()?.Path)?.Replace('\\', '/');
                if(string.IsNullOrWhiteSpace(directory)) return null;
                return directory;
            });

        var generatorInput = assetRootFolder
            .Combine(modName)
            .Select(
                static (tuple, _) =>
                    new GeneratorInput(tuple.Left, tuple.Right)
            );

        /*
            asset files are grouped by directory
            one generated file per asset is technically the most efficient, but itd generate way too many files
            one generated file with all assets would generate a very large file, which the compiler might not like
            and changing one fill would necessitate an entire file rebuild
            
            grouping by directory only triggers a rebuild for the directory a file belongs to
         */
        var contents = context.AdditionalTextsProvider
            .Where(static file => supported_extensions.Any(ext =>
                file.Path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
            ))
            .Select((file, _) => file.Path.Replace('\\', '/'))
            .Combine(generatorInput)
            .Where(tuple =>
                tuple.Right.AssetRootFolder != null && tuple.Left.StartsWith(
                    tuple.Right.AssetRootFolder.Value.ToString(),
                    StringComparison.Ordinal
                )
            )
            .Select(static (tuple, _) =>
            {
                var fileInfo = new FileInformation(
                    FullPath: tuple.Left,
                    RootFolder: tuple.Right.AssetRootFolder!.Value,
                    AssemblyName: tuple.Right.AssemblyName
                );

                var fullPath = fileInfo.FullPath.AsSegment(fileInfo.RootFolder.Length + 1);

                var path = PathUtils.RemoveExtension(fullPath);
                var folder = PathUtils.GetFolder(fullPath);
                var name = PathUtils.GetFileNameWithoutExtension(fullPath);
                var extension = PathUtils.GetExtension(fullPath);

                if(folder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    folder = folder.Substring("Assets/".Length);

                //determine asset type based on file extension
                //exception should never be thrown in any case, but defensive anyways
                var assetType = extension.Equals(image_extension, StringComparison.OrdinalIgnoreCase)
                        ? AssetType.Texture2D :
                    extension.Equals(effect_extension, StringComparison.OrdinalIgnoreCase)
                        ? AssetType.Effect :
                    throw new InvalidOperationException("how");

                return new
                {
                    AssetFile = new AssetFile(
                        Path: path,
                        Folder: folder,
                        Name: name,
                        Extension: extension,
                        AssetType: assetType
                    ),
                    fileInfo.AssemblyName
                };
            })
            .Collect()
            .SelectMany(
                (files, _) =>
                    files
                        .GroupBy(
                            f => f.AssetFile.Folder,
                            f => f,
                            (key, group) =>
                                (key, group.ToImmutableArray())
                        )
                        .ToImmutableArray()
            );

        context.RegisterSourceOutput(
            assetRootFolder.Combine(modName),
            (context, tuple) =>
            {
                var (path, modName) = tuple;
                string warn = "";
                if(path == null)
                    warn = "#warning missing AssetRoot.txt file";
                context.AddSource(
                    "Assets.default.g.cs",
                    $@"// <auto-generated/>
namespace {modName}.Assets;
[System.CodeDom.Compiler.GeneratedCodeAttribute(""{typeof(AssetGenerator).FullName}"", ""{tool_version}"")]
partial class Assets;
{warn}
"
                );
            }
        );

        context.RegisterSourceOutput(
            contents.Combine(modName),
            (sourceContext, tuple) =>
            {

                var (contentTuple, tupleModName) = tuple;
                var (folder, assetFiles) = contentTuple;

                sourceContext.CancellationToken.ThrowIfCancellationRequested();
                using var writer = new IndentedStringWriter(1024);

                writer.WriteLine("#region disclaimer");
                writer.WriteLine("/*");
                writer.WriteLine("<auto-generated/>");

                writer.Indent++;
                writer.WriteLine("this code was auto-generated by a tool.");
                writer.WriteLine("for support, bug reporting, suggestions, etc. contact math2. 'blowyourselfup' on discord");
                writer.Indent--;

                writer.WriteLine("*/");
                writer.WriteLine("#endregion disclaimer");

                writer.WriteLine($@"
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using System;

using ImageAsset = ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D>;
using EffectAsset = ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Effect>;

namespace {tupleModName}.Assets;

partial class Assets {{"
                );
                writer.Indent++;
                foreach(var part in folder.SplitEx('/', StringSplitOptions.RemoveEmptyEntries)) {
                    writer.WriteLine($"public partial class {part} {{   ");
                    writer.Indent++;
                }

                foreach(var fileData in assetFiles) {
                    var file = fileData.AssetFile;
                    var assetPath = $"{tupleModName}/{file.Path}";

                    writer.WriteLine($"public const string KEY_{file.Name} = \"{assetPath}\";");

                    var typeLazy = file.AssetType switch
                    {
                        AssetType.Texture2D => $"public readonly static Lazy<ImageAsset> {file.Name}_lazy = new(() => ModContent.Request<Texture2D>(\"{assetPath}\"));",
                        AssetType.Effect => $"public readonly static Lazy<EffectAsset> {file.Name}_lazy = new(() => ModContent.Request<Effect>(\"{assetPath}\", AssetRequestMode.ImmediateLoad));",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var type = file.AssetType switch
                    {
                        AssetType.Texture2D => $"public static ImageAsset {file.Name} {{ get; }} = {file.Name}_lazy.Value;",
                        AssetType.Effect => $"public static EffectAsset {file.Name} {{ get; }} = ModContent.Request<Effect>(\"{assetPath}\", AssetRequestMode.ImmediateLoad);",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    writer.WriteLine(typeLazy);
                    writer.WriteLine(type);
                }

                foreach(var _ in folder.SplitEx('/', StringSplitOptions.RemoveEmptyEntries)) {
                    writer.Indent--;
                    writer.WriteLine("}");
                }

                writer.Indent--;
                writer.WriteLine("}"); // Assets class

                var sourceText = writer.ToStringAndClear();

                writer.Write($"Assets.{folder}.cs");
                writer.Builder.Replace('/', '.');
                var fileName = writer.ToString();
                if(fileName.Equals("Assets..cs", StringComparison.Ordinal)) // file was on root
                    fileName = "Assets.g.cs";

                sourceContext.AddSource(fileName, sourceText);
            }
        );
    }

    /// <summary> Represents the input data required for this generator, including the root assets folder and assembly name. </summary>
    /// <param name="AssemblyName">The name of the mod used for generation, derived from the compilation's assembly name.</param>
    private readonly record struct GeneratorInput(StringSegment? AssetRootFolder, StringSegment AssemblyName);

    /// <summary> Represents information about a file. </summary>
    private readonly record struct FileInformation(string FullPath, StringSegment RootFolder, StringSegment AssemblyName);
}