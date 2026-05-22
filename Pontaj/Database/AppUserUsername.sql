SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers
        GROUP BY Username
        HAVING COUNT(*) > 1
    )
    BEGIN
        ;THROW 51000, N'Există valori duplicate în AppUsers.Username. Rezolvați duplicatele înainte de a aplica indexul unic.', 1;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AppUsers') AND name = N'UX_AppUsers_Username'
    )
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX UX_AppUsers_Username ON dbo.AppUsers (Username);');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
