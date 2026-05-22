SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Employees') AND name = N'Username'
    )
    BEGIN
        ALTER TABLE dbo.Employees ADD Username NVARCHAR(200) NULL;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Employees') AND name = N'UX_Employees_Username'
    )
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX UX_Employees_Username
               ON dbo.Employees (Username)
               WHERE Username IS NOT NULL;');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
