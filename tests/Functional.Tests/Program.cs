using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChatSupportSystem.FunctionalTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Chat Management System functional tests...");

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });

        var logger = loggerFactory.CreateLogger<TestRunner>();

        var testRunner = new TestRunner(logger);

        try
        {
            await testRunner.RunAllTestScenarios();
            Console.WriteLine("All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}