using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ReportsClient;

string baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5001";
using var httpClient = new HttpClient();

Console.Clear();
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("==================================================");
Console.WriteLine("        NUCLEAR REACTOR CONSENSUS DASHBOARD       ");
Console.WriteLine("==================================================");
Console.ResetColor();
Console.WriteLine($"Target API: {baseUrl}/api/reports\n");

while (true)
{
    Console.Write("Get report? (y/n): ");
    string? input = Console.ReadLine()?.Trim().ToLower();

    if (input == "n")
    {
        Console.WriteLine("Exiting application. Goodbye!");
        break; 
    }
    else if (input == "y")
    {
        try
        {
            Console.WriteLine("\nConnecting to API and fetching reports...");

            var reports = await httpClient.GetFromJsonAsync<List<ConsensusReportDTO>>($"{baseUrl}/api/reports");

            if (reports != null && reports.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n--- CONSENSUS REPORTS FROM DATABASE ---");
                Console.ResetColor();

                foreach (var report in reports)
                {
                    string sensors = string.Join(", ", report.ParticipatingSensors);
                    Console.WriteLine($"Timestamp: {report.CalculatedAt:yyyy-MM-dd HH:mm:ss} | Value: {report.Value:F2}°C | Sensors: [{sensors}]");
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("---------------------------------------\n");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("No reports found in the database.\n");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error occurred while fetching data: {ex.Message}\n");
            Console.ResetColor();
        }

    }
    else
    {
        Console.WriteLine("Invalid option. Please enter 'y' for Yes or 'n' for exit.\n");
    }
}