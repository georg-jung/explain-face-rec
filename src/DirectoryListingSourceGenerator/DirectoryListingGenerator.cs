// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DirectoryListingSourceGenerator;

// inspired by https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
[Generator]
public class DirectoryListingGenerator : IIncrementalGenerator
{
    private static readonly MemoryCache _cache = new(nameof(DirectoryListingGenerator));

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DirectoryListingGenerator.DirectoryListingAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

        IncrementalValuesProvider<MethodDeclarationSyntax> methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<MethodDeclarationSyntax>)> compilationAndMethods
            = context.CompilationProvider.Combine(methodDeclarations.Collect());

        context.RegisterSourceOutput(
            compilationAndMethods,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is MethodDeclarationSyntax methodDeclarationSyntax &&
           methodDeclarationSyntax.AttributeLists.Count > 0 &&
           methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) &&
           (methodDeclarationSyntax.ReturnType.ToString() == "string[]" ||
            methodDeclarationSyntax.ReturnType.ToString() == "IReadOnlyDictionary<string, string[]>");

    private static MethodDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var declSyntax = (MethodDeclarationSyntax)context.Node;

        foreach (AttributeSyntax attributeSyntax in declSyntax.AttributeLists.SelectMany(x => x.Attributes))
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                // weird, we couldn't get the symbol, ignore it
                continue;
            }

            INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
            string fullName = attributeContainingTypeSymbol.ToDisplayString();

            if (fullName == SourceGenerationHelper.AttributeFullName && attributeSyntax.ArgumentList is AttributeArgumentListSyntax lst && lst.Arguments.Count == 1)
            {
                // return the enum
                return declSyntax;
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
    {
        if (methods.IsDefaultOrEmpty)
        {
            return;
        }

        IEnumerable<MethodDeclarationSyntax> distinctMethods = methods.Distinct();
        var methodImpls = new List<string>(methods.Length);

        foreach (var methodDeclaration in distinctMethods)
        {
            var model = compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
            var methodSymbol = model?.GetDeclaredSymbol(methodDeclaration);

            var attributeData = methodSymbol?.GetAttributes()
                .FirstOrDefault(attr => SourceGenerationHelper.AttributeClass.Equals(attr.AttributeClass?.Name) && attr.ConstructorArguments.Length == 1);

            if (attributeData is null)
            {
                continue;
            }

            var path = attributeData.ConstructorArguments[0].Value as string;
            if (path is null)
            {
                continue;
            }

            var returnType = methodDeclaration.ReturnType.ToString();
            string methodSource;

            if (returnType == "string[]")
            {
                methodSource = GenerateStringArrayMethodImplementation(methodSymbol!, methodDeclaration.SyntaxTree.FilePath, path);
            }
            else if (returnType == "IReadOnlyDictionary<string, string[]>")
            {
                methodSource = GenerateDictionaryMethodImplementation(methodSymbol!, methodDeclaration.SyntaxTree.FilePath, path);
            }
            else
            {
                continue;
            }

            methodImpls.Add(methodSource);
        }

        var source = $@"// <auto-generated/>
using System;
using System.Collections.Generic;

{string.Join("\n", methodImpls)}
";

        context.AddSource($"DirectoryListingGenerator.DirectoryListings.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateMethodImplementation(IMethodSymbol methodSymbol, string collectionInitializerContent, string? returnInstanceType = null)
    {
        var containingType = methodSymbol.ContainingType;
        var namespaceName = containingType.ContainingNamespace.ToDisplayString();
        var className = containingType.Name;
        var methodName = methodSymbol.Name;
        var methodAccessibility = methodSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
        var staticSymbol = methodSymbol.IsStatic ? "static" : string.Empty;
        var retType = methodSymbol.ReturnType.ToDisplayString();

        return $@"
namespace {namespaceName}
{{
    partial class {className}
    {{
        {methodAccessibility} {staticSymbol} partial {retType} {methodName}()
        {{
            return new {returnInstanceType ?? retType}
            {{
{collectionInitializerContent}
            }};
        }}
    }}
}}";
    }

    private static string GenerateStringArrayMethodImplementation(IMethodSymbol methodSymbol, string classFilePath, string pathArgument)
    {
        var classDir = Path.GetDirectoryName(classFilePath);
        var fullPath = Path.GetFullPath(Path.Combine(classDir, pathArgument));
        var files = GetFilesAtCompileTime(fullPath);
        return GenerateMethodImplementation(methodSymbol, files);
    }

    private static string GenerateDictionaryMethodImplementation(IMethodSymbol methodSymbol, string classFilePath, string pathArgument)
    {
        var classDir = Path.GetDirectoryName(classFilePath);
        var fullPath = Path.GetFullPath(Path.Combine(classDir, pathArgument));
        var directoryEntries = GetDirectoryEntriesAtCompileTime(fullPath);
        return GenerateMethodImplementation(methodSymbol, directoryEntries, "Dictionary<string, string[]>");
    }

    private static string GetFilesAtCompileTime(string fullPath)
        => GetCached($"Files_{fullPath}", () => GetFilesAtCompileTimeImpl(fullPath));

    private static string GetFilesAtCompileTimeImpl(string directory)
    {
        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory).Select(Path.GetFileName).OrderBy(x => x).Select(file => $@"@""{file}""");
            return string.Join(",\n", files);
        }

        return string.Empty;
    }

    private static string GetDirectoryEntriesAtCompileTime(string fullPath)
        => GetCached($"Directory_{fullPath}", () => GetDirectoryEntriesAtCompileTimeImpl(fullPath));

    private static string GetDirectoryEntriesAtCompileTimeImpl(string directory)
    {
        var directoryData = GetDirectoriesAndFilesAtCompileTime(directory);
        return string.Join(",\n", directoryData.Select(kv => $@"{{ ""{kv.Key}"", new string[] {{ {string.Join(", ", kv.Value.Select(file => $@"@""{file}"""))} }} }}"));
    }

    private static Dictionary<string, List<string>> GetDirectoriesAndFilesAtCompileTime(string directory)
    {
        var result = new Dictionary<string, List<string>>();

        if (Directory.Exists(directory))
        {
            var directories = Directory.GetDirectories(directory).OrderBy(x => x);

            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                var files = Directory.GetFiles(dir).Select(Path.GetFileName).ToList();
                result[dirName] = files;
            }
        }

        return result;
    }

    private static string GetCached(string key, Func<string> factory)
    {
        var cached = _cache.Get(key);
        if (cached is string val)
        {
            return val;
        }

        var result = factory();
        _cache.Add(key, result, DateTimeOffset.Now.AddMinutes(1));
        return result;
    }
}
