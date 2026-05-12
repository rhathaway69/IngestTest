using System;
using System.Collections.Generic;

namespace IngestionTest.Models
{
    public class TransactionRecord
    {
        public string TransactionId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public DateTime InsertDate { get; set; } = DateTime.UtcNow;
    }

    public class DuplicateTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public DateTime DetectedDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
    }

    public class ValidationError
    {
        public int LineNumber { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Line {LineNumber}, Field '{Field}': {Error}";
        }
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public int RecordsInserted { get; set; }
        public int DuplicatesFound { get; set; }
        public List<ValidationError> ValidationErrors { get; set; } = new();
    }
}