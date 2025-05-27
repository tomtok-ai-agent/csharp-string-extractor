using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CSharpStringExtractor.Tests
{
    public class StringLiteralExtractorTests
    {
        [Fact]
        public async Task ExtractStringLiterals_FromSimpleFile_ShouldFindAllLiterals()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "CSharpStringExtractorTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);
            
            var testFilePath = Path.Combine(testDir, "TestFile.cs");
            var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var str1 = ""Hello, World!"";
            var str2 = ""Another string"";
            Console.WriteLine(""Third string"");
            
            // String in a comment ""Not extracted""
            /* String in a block comment ""Not extracted"" */
            
            var multiLine = @""This is a 
multi-line string"";
            
            var interpolated = $""Value: {42}"";
        }
    }
}";
            File.WriteAllText(testFilePath, testCode);
            
            var outputPath = Path.Combine(testDir, "output.json");
            
            try
            {
                // Act
                // Simulate command line execution
                await Program.Main(new[] { "--directory", testDir, "--output", outputPath });
                
                // Assert
                Assert.True(File.Exists(outputPath), "Output file should be created");
                
                var jsonContent = await File.ReadAllTextAsync(outputPath);
                var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent);
                
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains("TestFile.cs", result.Keys);
                
                var literals = result["TestFile.cs"];
                Assert.Equal(5, literals.Count);
                Assert.Contains("Hello, World!", literals);
                Assert.Contains("Another string", literals);
                Assert.Contains("Third string", literals);
                Assert.Contains("This is a \nmulti-line string", literals);
                Assert.Contains("Value: {42}", literals);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }
        
        [Fact]
        public async Task ExtractStringLiterals_FromEmptyDirectory_ShouldReturnEmptyResult()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "CSharpStringExtractorTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);
            
            var outputPath = Path.Combine(testDir, "output.json");
            
            try
            {
                // Act
                await Program.Main(new[] { "--directory", testDir, "--output", outputPath });
                
                // Assert
                Assert.True(File.Exists(outputPath), "Output file should be created");
                
                var jsonContent = await File.ReadAllTextAsync(outputPath);
                var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent);
                
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }
        
        [Fact]
        public async Task ExtractStringLiterals_FromNestedDirectories_ShouldFindAllFiles()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "CSharpStringExtractorTests", Guid.NewGuid().ToString());
            var nestedDir = Path.Combine(testDir, "Nested");
            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(nestedDir);
            
            var testFile1Path = Path.Combine(testDir, "TestFile1.cs");
            var testFile2Path = Path.Combine(nestedDir, "TestFile2.cs");
            
            File.WriteAllText(testFile1Path, @"
namespace Test
{
    class Program
    {
        static void Main()
        {
            var str = ""Root file string"";
        }
    }
}");
            
            File.WriteAllText(testFile2Path, @"
namespace Test.Nested
{
    class Helper
    {
        public string GetValue()
        {
            return ""Nested file string"";
        }
    }
}");
            
            var outputPath = Path.Combine(testDir, "output.json");
            
            try
            {
                // Act
                await Program.Main(new[] { "--directory", testDir, "--output", outputPath });
                
                // Assert
                Assert.True(File.Exists(outputPath), "Output file should be created");
                
                var jsonContent = await File.ReadAllTextAsync(outputPath);
                var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent);
                
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Contains("TestFile1.cs", result.Keys);
                Assert.Contains(Path.Combine("Nested", "TestFile2.cs"), result.Keys);
                
                Assert.Single(result["TestFile1.cs"]);
                Assert.Contains("Root file string", result["TestFile1.cs"]);
                
                Assert.Single(result[Path.Combine("Nested", "TestFile2.cs")]);
                Assert.Contains("Nested file string", result[Path.Combine("Nested", "TestFile2.cs")]);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }
    }
}
