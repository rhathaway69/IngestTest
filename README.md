# IngestionTest - CSV to SQL Server Bulk Importer

A robust C# solution for ingesting CSV files, validating data, and bulk inserting into SQL Server with comprehensive testing.

## Features

✅ **CSV Parsing** - Uses CsvHelper for reliable CSV reading
✅ **Data Validation** - Comprehensive validation with detailed error reporting
✅ **Bulk Insert** - High-performance SQL Server bulk copy with batch processing
✅ **Automatic Timestamps** - InsertDate field populated with UTC time
✅ **Complete Test Suite** - 5 xUnit test cases covering success and error scenarios
✅ **Error Handling** - Production-ready exception handling throughout

## Project Structure

```
IngestionTest/
├── IngestionTest.csproj                    # Main project file
├── Models/
│   └── TransactionRecord.cs                # Data models and validation errors
├── Services/
│   └── CsvTransactionImporter.cs           # Core import logic
├── Program.cs                              # Example usage
├── IngestionTest.Tests/                    # Test project
│   ├── IngestionTest.Tests.csproj
│   └── CsvTransactionImporterTests.cs      # 5 comprehensive test cases
├── Database/
│   └── CreateTransactionsTable.sql         # SQL Server schema
├── sample-transactions.csv                 # Sample data
└── README.md                               # This file
```

## Requirements

- .NET 8.0 or later
- SQL Server 2019 or later (optional, only needed for actual database operations)
- Visual Studio 2022, VS Code, or another .NET IDE (optional)

## NuGet Dependencies

- **CsvHelper** (v30.0.0) - CSV parsing
- **System.Data.SqlClient** (v4.8.6) - SQL Server connectivity
- **xUnit** (v2.6.2) - Unit testing framework (Tests project only)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/rhathaway69/IngestionTest.git
cd IngestionTest
```

### 2. Build the Solution

```bash
dotnet build
```

### 3. Run the Tests

```bash
dotnet test
```

Expected output:
```
✓ ParseCsvFile_WithValidData_ReturnsCorrectRecords
✓ ValidateRecords_WithInvalidData_ReturnsErrorsForEachInvalidField
✓ ParseCsvFile_WithEmptyFile_ReturnsEmptyList
✓ ParseCsvFile_WithNonExistentFile_ThrowsFileNotFoundException
✓ ValidationError_ToString_FormatsMessageCorrectly

5 passed
```

## Usage

### Basic Example

```csharp
using IngestionTest.Services;

string connectionString = "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;";
string csvFilePath = "transactions.csv";

var importer = new CsvTransactionImporter(connectionString);
var (success, errors) = await importer.ImportCsvFile(csvFilePath);

if (success)
{
    Console.WriteLine("Import completed successfully!");
}
else
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### CSV File Format

Your CSV file must contain these columns:

```csv
TransactionId,MemberId,TransactionDate,Amount
TXN001,MEM001,2026-05-01,100.50
TXN002,MEM002,2026-05-02,250.75
```

**Field Requirements:**
- **TransactionId** - Required, non-empty string
- **MemberId** - Required, non-empty string
- **TransactionDate** - Required, valid date (YYYY-MM-DD format)
- **Amount** - Required, must be greater than 0
- **InsertDate** - Auto-generated with current UTC timestamp

## Database Setup

### 1. Create the Transactions Table

Run `Database/CreateTransactionsTable.sql` on your SQL Server:

```sql
CREATE TABLE [dbo].[Transactions]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TransactionId] NVARCHAR(50) NOT NULL,
    [MemberId] NVARCHAR(50) NOT NULL,
    [TransactionDate] DATETIME NOT NULL,
    [Amount] DECIMAL(10, 2) NOT NULL,
    [InsertDate] DATETIME NOT NULL,
    CONSTRAINT [UQ_TransactionId] UNIQUE ([TransactionId])
);
```

### 2. Update Connection String

Modify `Program.cs` with your SQL Server credentials:

```csharp
const string connectionString = "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;";
```

## Test Cases

### Test 1: Valid CSV Parsing
**Purpose:** Verify that valid CSV data is parsed correctly
- Creates a valid 3-record CSV file
- Parses the file
- Asserts all records are loaded with correct values

### Test 2: Invalid Data Validation
**Purpose:** Verify validation catches all data errors
- Creates records with multiple validation errors (empty fields, negative amounts, invalid dates)
- Runs validation
- Asserts all errors are detected and reported with correct line/field info

### Test 3: Empty File Handling
**Purpose:** Verify graceful handling of empty files
- Creates a CSV with only headers
- Parses the file
- Asserts empty list is returned

### Test 4: Missing File Exception
**Purpose:** Verify appropriate exception for non-existent files
- Attempts to parse non-existent file
- Asserts FileNotFoundException is thrown

### Test 5: Validation Error Formatting
**Purpose:** Verify ValidationError message formatting
- Creates a ValidationError object
- Asserts ToString() outputs correct format

## API Reference

### CsvTransactionImporter Class

#### ParseCsvFile(string filePath)
```csharp
public List<TransactionRecord> ParseCsvFile(string filePath)
```
Reads and parses a CSV file into TransactionRecord objects.

**Parameters:**
- `filePath` - Path to the CSV file

**Returns:** List of TransactionRecord objects

**Throws:** 
- `FileNotFoundException` - If file doesn't exist
- `InvalidOperationException` - If CSV parsing fails

#### ValidateRecords(List<TransactionRecord> records)
```csharp
public (bool IsValid, List<ValidationError> Errors) ValidateRecords(List<TransactionRecord> records)
```
Validates a list of transaction records.

**Parameters:**
- `records` - List of records to validate

**Returns:** Tuple of (IsValid, ErrorList)

#### BulkInsertRecords(List<TransactionRecord> records)
```csharp
public async Task BulkInsertRecords(List<TransactionRecord> records)
```
Performs bulk insert into SQL Server.

**Parameters:**
- `records` - List of validated records to insert

**Throws:** `InvalidOperationException` - If bulk insert fails

#### ImportCsvFile(string filePath)
```csharp
public async Task<(bool Success, List<ValidationError> Errors)> ImportCsvFile(string filePath)
```
Orchestrates the complete import process (parse → validate → insert).

**Parameters:**
- `filePath` - Path to the CSV file

**Returns:** Tuple of (Success, ErrorList)

## Performance Notes

- **Batch Size:** 1000 records per batch for optimal performance
- **Bulk Copy Timeout:** 300 seconds (configurable)
- **Column Mapping:** Explicitly mapped for reliability

## Error Handling

The solution provides detailed error reporting:

```
Line 5, Field 'Amount': Amount must be greater than 0
Line 7, Field 'TransactionDate': TransactionDate is required and must be a valid date
```

## Troubleshooting

### Connection String Issues
- Verify SQL Server is running
- Check credentials are correct
- Ensure database exists
- Test connection with SQL Server Management Studio

### CSV Parsing Issues
- Ensure CSV headers match expected column names exactly
- Check date format is YYYY-MM-DD
- Verify no extra spaces in CSV headers

### Validation Failures
- Review validation error messages for specific field issues
- Check CSV data types and formats
- Ensure TransactionDate values are valid dates
- Verify Amount values are positive numbers

## Contributing

Feel free to fork and submit pull requests with improvements!

## License

MIT License - See LICENSE file for details
