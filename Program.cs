using IngestionTest.Services;
using System;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        const string connectionString = "Server=ROBERT-PC2021;Database=StudentDB;User Id=RHTest;Password=Test2026!;";
        const string csvFilePath = "C:\\Users\\robha\\Documents\\GitHub\\IngestionTest\\sample-transactions.csv";

        try
        {
            var importer = new CsvTransactionImporter(connectionString);

            Console.WriteLine("Starting CSV import process...");
            Console.WriteLine($"Reading file: {csvFilePath}");

            var result = await importer.ImportCsvFile(csvFilePath);

            if (result.Success)
            {
                Console.WriteLine("✓ CSV import completed successfully!");
                Console.WriteLine($"  Records inserted: {result.RecordsInserted}");
                if (result.DuplicatesFound > 0)
                {
                    Console.WriteLine($"  Duplicates found and logged: {result.DuplicatesFound}");
                }
            }
            else
            {
                Console.WriteLine("✗ CSV import failed:");
                foreach (var error in result.ValidationErrors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Details: {ex.InnerException.Message}");
            }
        }
    }
}