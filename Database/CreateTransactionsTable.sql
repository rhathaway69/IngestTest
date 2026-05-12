-- Create Transactions table for CSV import
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

-- Create indexes for common queries
CREATE INDEX [IX_MemberId] ON [dbo].[Transactions] ([MemberId]);
CREATE INDEX [IX_TransactionDate] ON [dbo].[Transactions] ([TransactionDate]);
CREATE INDEX [IX_InsertDate] ON [dbo].[Transactions] ([InsertDate]);

CREATE TABLE [dbo].[DuplicateTransactions]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TransactionId] NVARCHAR(50) NOT NULL,
    [MemberId] NVARCHAR(50) NOT NULL,
    [TransactionDate] DATETIME NOT NULL,
    [Amount] DECIMAL(10, 2) NOT NULL,
    [DetectedDate] DATETIME NOT NULL,
    [Reason] NVARCHAR(500) NOT NULL
);