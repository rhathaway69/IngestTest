using CsvHelper;
using IngestionTest.Models;
using System.Globalization;
using System.Data.SqlClient;
using System.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IngestionTest.Services
{
    public class CsvTransactionImporter
    {
        private readonly string _connectionString;
        private const int BatchSize = 1000;

        public CsvTransactionImporter(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Parses a CSV file and returns TransactionRecord objects
        /// </summary>
        public List<TransactionRecord> ParseCsvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            var records = new List<TransactionRecord>();

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<TransactionRecordMap>();
                    records = csv.GetRecords<TransactionRecord>().ToList();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing CSV file: {ex.Message}", ex);
            }

            return records;
        }

        /// <summary>
        /// Validates transaction records
        /// </summary>
        public (bool IsValid, List<ValidationError> Errors) ValidateRecords(List<TransactionRecord> records)
        {
            var errors = new List<ValidationError>();
            int lineNumber = 2; // CSV header is line 1

            foreach (var record in records)
            {
                // Validate TransactionId
                if (string.IsNullOrWhiteSpace(record.TransactionId))
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = lineNumber,
                        Field = "TransactionId",
                        Error = "TransactionId is required and cannot be empty"
                    });
                }

                // Validate MemberId
                if (string.IsNullOrWhiteSpace(record.MemberId))
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = lineNumber,
                        Field = "MemberId",
                        Error = "MemberId is required and cannot be empty"
                    });
                }

                // Validate TransactionDate
                if (record.TransactionDate == default)
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = lineNumber,
                        Field = "TransactionDate",
                        Error = "TransactionDate is required and must be a valid date"
                    });
                }

                // Validate Amount
                if (record.Amount <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = lineNumber,
                        Field = "Amount",
                        Error = "Amount must be greater than 0"
                    });
                }

                lineNumber++;
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Checks for duplicates in the database and returns existing and new records
        /// </summary>
        public async Task<(List<TransactionRecord> NewRecords, List<DuplicateTransaction> DuplicateRecords)> CheckForDuplicates(List<TransactionRecord> records)
        {
            var existingIds = new HashSet<string>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var transactionIds = string.Join(",", records.Select(r => $"'{r.TransactionId.Replace("'", "''")}'"));

                    using (var command = new SqlCommand(
                        $"SELECT TransactionId FROM [dbo].[Transactions] WHERE TransactionId IN ({transactionIds})",
                        connection))
                    {
                        command.CommandTimeout = 300;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                existingIds.Add(reader["TransactionId"].ToString() ?? "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error checking for duplicates: {ex.Message}", ex);
            }

            var newRecords = new List<TransactionRecord>();
            var duplicateRecords = new List<DuplicateTransaction>();

            foreach (var record in records)
            {
                if (existingIds.Contains(record.TransactionId))
                {
                    duplicateRecords.Add(new DuplicateTransaction
                    {
                        TransactionId = record.TransactionId,
                        MemberId = record.MemberId,
                        TransactionDate = record.TransactionDate,
                        Amount = record.Amount,
                        DetectedDate = DateTime.UtcNow,
                        Reason = "Duplicate TransactionId already exists in database"
                    });
                }
                else
                {
                    newRecords.Add(record);
                }
            }

            return (newRecords, duplicateRecords);
        }

        /// <summary>
        /// Logs duplicate transactions to the DuplicateTransactions table
        /// </summary>
        public async Task LogDuplicateTransactions(List<DuplicateTransaction> duplicates)
        {
            if (duplicates.Count == 0)
            {
                return;
            }

            var dataTable = ConvertDuplicatesToDataTable(duplicates);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var bulkCopy = new SqlBulkCopy(connection)
                    {
                        DestinationTableName = "DuplicateTransactions",
                        BatchSize = BatchSize,
                        BulkCopyTimeout = 300
                    })
                    {
                        bulkCopy.ColumnMappings.Add("TransactionId", "TransactionId");
                        bulkCopy.ColumnMappings.Add("MemberId", "MemberId");
                        bulkCopy.ColumnMappings.Add("TransactionDate", "TransactionDate");
                        bulkCopy.ColumnMappings.Add("Amount", "Amount");
                        bulkCopy.ColumnMappings.Add("DetectedDate", "DetectedDate");
                        bulkCopy.ColumnMappings.Add("Reason", "Reason");

                        await bulkCopy.WriteToServerAsync(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error logging duplicate transactions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Performs bulk insert of validated records into SQL Server
        /// </summary>
        public async Task<int> BulkInsertRecords(List<TransactionRecord> records)
        {
            if (records.Count == 0)
            {
                return 0;
            }

            var dataTable = ConvertToDataTable(records);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var bulkCopy = new SqlBulkCopy(connection)
                    {
                        DestinationTableName = "Transactions",
                        BatchSize = BatchSize,
                        BulkCopyTimeout = 300
                    })
                    {
                        bulkCopy.ColumnMappings.Add("TransactionId", "TransactionId");
                        bulkCopy.ColumnMappings.Add("MemberId", "MemberId");
                        bulkCopy.ColumnMappings.Add("TransactionDate", "TransactionDate");
                        bulkCopy.ColumnMappings.Add("Amount", "Amount");
                        bulkCopy.ColumnMappings.Add("InsertDate", "InsertDate");

                        await bulkCopy.WriteToServerAsync(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during bulk insert: {ex.Message}", ex);
            }

            return records.Count;
        }

        /// <summary>
        /// Orchestrates the complete CSV import process with duplicate handling
        /// </summary>
        public async Task<ImportResult> ImportCsvFile(string filePath)
        {
            var result = new ImportResult();

            try
            {
                // Parse CSV
                var records = ParseCsvFile(filePath);

                if (records.Count == 0)
                {
                    throw new InvalidOperationException("CSV file is empty");
                }

                // Set InsertDate to current UTC time
                var utcNow = DateTime.UtcNow;
                foreach (var record in records)
                {
                    record.InsertDate = utcNow;
                }

                // Validate records
                var (isValid, errors) = ValidateRecords(records);

                if (!isValid)
                {
                    result.Success = false;
                    result.ValidationErrors = errors;
                    return result;
                }

                // Check for duplicates
                var (newRecords, duplicateRecords) = await CheckForDuplicates(records);

                // Log duplicates
                if (duplicateRecords.Count > 0)
                {
                    await LogDuplicateTransactions(duplicateRecords);
                    result.DuplicatesFound = duplicateRecords.Count;
                }

                // Bulk insert new records
                if (newRecords.Count > 0)
                {
                    result.RecordsInserted = await BulkInsertRecords(newRecords);
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ValidationErrors.Add(new ValidationError
                {
                    LineNumber = 0,
                    Field = "Import",
                    Error = ex.Message
                });
                return result;
            }
        }

        private DataTable ConvertToDataTable(List<TransactionRecord> records)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("TransactionId", typeof(string));
            dataTable.Columns.Add("MemberId", typeof(string));
            dataTable.Columns.Add("TransactionDate", typeof(DateTime));
            dataTable.Columns.Add("Amount", typeof(decimal));
            dataTable.Columns.Add("InsertDate", typeof(DateTime));

            foreach (var record in records)
            {
                dataTable.Rows.Add(
                    record.TransactionId,
                    record.MemberId,
                    record.TransactionDate,
                    record.Amount,
                    record.InsertDate
                );
            }

            return dataTable;
        }

        private DataTable ConvertDuplicatesToDataTable(List<DuplicateTransaction> duplicates)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("TransactionId", typeof(string));
            dataTable.Columns.Add("MemberId", typeof(string));
            dataTable.Columns.Add("TransactionDate", typeof(DateTime));
            dataTable.Columns.Add("Amount", typeof(decimal));
            dataTable.Columns.Add("DetectedDate", typeof(DateTime));
            dataTable.Columns.Add("Reason", typeof(string));

            foreach (var duplicate in duplicates)
            {
                dataTable.Rows.Add(
                    duplicate.TransactionId,
                    duplicate.MemberId,
                    duplicate.TransactionDate,
                    duplicate.Amount,
                    duplicate.DetectedDate,
                    duplicate.Reason
                );
            }

            return dataTable;
        }
    }

    public sealed class TransactionRecordMap : CsvHelper.Configuration.ClassMap<TransactionRecord>
    {
        public TransactionRecordMap()
        {
            Map(m => m.TransactionId).Name("TransactionId");
            Map(m => m.MemberId).Name("MemberId");
            Map(m => m.TransactionDate).Name("TransactionDate");
            Map(m => m.Amount).Name("Amount");
        }
    }
}