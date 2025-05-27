using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpStringExtractor
{
    /// <summary>
    /// Main program class for the C# string literal extractor
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task<int> Main(string[] args)
        {
            var directoryOption = new Option<DirectoryInfo>(
                new[] { "--directory", "-d" },
                "The directory containing C# source code files to analyze"
            )
            {
                IsRequired = true
            };

            var outputOption = new Option<FileInfo>(
                new[] { "--output", "-o" },
                "The output JSON file path"
            )
            {
                IsRequired = true
            };

            var rootCommand = new RootCommand("Extract string literals from C# source code files")
            {
                directoryOption,
                outputOption
            };

            rootCommand.SetHandler(async (DirectoryInfo directory, FileInfo output) =>
            {
                await ExtractStringsFromDirectory(directory, output);
            }, directoryOption, outputOption);

            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Extracts string literals from all C# files in the specified directory
        /// </summary>
        /// <param name="directory">The directory to scan</param>
        /// <param name="output">The output file path</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private static async Task ExtractStringsFromDirectory(DirectoryInfo directory, FileInfo output)
        {
            if (!directory.Exists)
            {
                Console.Error.WriteLine($"Error: Directory '{directory.FullName}' does not exist.");
                return;
            }

            Console.WriteLine($"Scanning directory: {directory.FullName}");
            
            var result = new Dictionary<string, List<string>>();
            var files = Directory.GetFiles(directory.FullName, "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(directory.FullName, file);
                var stringLiterals = await ExtractStringLiteralsFromFile(file);
                
                if (stringLiterals.Any())
                {
                    result[relativePath] = stringLiterals;
                }
            }

            Console.WriteLine($"Found string literals in {result.Count} files");
            
            // Write the result to the output file
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(result, options);
            await File.WriteAllTextAsync(output.FullName, json);
            
            Console.WriteLine($"Results written to: {output.FullName}");
        }

        /// <summary>
        /// Extracts string literals from a single C# file
        /// </summary>
        /// <param name="filePath">The path to the C# file</param>
        /// <returns>A list of string literals found in the file</returns>
        private static async Task<List<string>> ExtractStringLiteralsFromFile(string filePath)
        {
            var stringLiterals = new List<string>();
            
            try
            {
                var code = await File.ReadAllTextAsync(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = await syntaxTree.GetRootAsync();
                
                // Find all regular string literals in the syntax tree
                var regularStringLiterals = root.DescendantNodes()
                    .OfType<LiteralExpressionSyntax>()
                    .Where(node => node.Kind() == SyntaxKind.StringLiteralExpression);
                
                foreach (var node in regularStringLiterals)
                {
                    // Extract the string value without quotes
                    var stringValue = node.Token.ValueText;
                    stringLiterals.Add(stringValue);
                }
                
                // Find all interpolated string literals
                var interpolatedStrings = root.DescendantNodes()
                    .OfType<InterpolatedStringExpressionSyntax>();
                    
                foreach (var interpolatedString in interpolatedStrings)
                {
                    // Get the raw text of the interpolated string
                    var text = interpolatedString.ToString();
                    // Remove the leading $ and quotes
                    if (text.StartsWith("$\"") && text.EndsWith("\""))
                    {
                        text = text.Substring(2, text.Length - 3);
                    }
                    stringLiterals.Add(text);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
            
            return stringLiterals;
        }
    }
}
