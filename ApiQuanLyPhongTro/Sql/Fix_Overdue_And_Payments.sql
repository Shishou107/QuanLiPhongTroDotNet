USE [Data_Base_Phontro];
GO

-- 0. Drop problematic constraint if exists
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Invoices_PaidAmount' AND parent_object_id = OBJECT_ID('dbo.Invoices'))
BEGIN
    ALTER TABLE dbo.Invoices DROP CONSTRAINT CK_Invoices_PaidAmount;
END
GO

-- 1. Fix Status column in Invoices table
UPDATE dbo.Invoices
SET 
    TotalAmount = ISNULL((SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) FROM dbo.InvoiceDetails WHERE InvoiceId = Invoices.Id), 0),
    PaidAmount = ISNULL((SELECT SUM(Amount) FROM dbo.Payments WHERE InvoiceId = Invoices.Id), 0),
    Status = CASE
        WHEN ISNULL((SELECT SUM(Amount) FROM dbo.Payments WHERE InvoiceId = Invoices.Id), 0) >= ISNULL((SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) FROM dbo.InvoiceDetails WHERE InvoiceId = Invoices.Id), 0) AND ISNULL((SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) FROM dbo.InvoiceDetails WHERE InvoiceId = Invoices.Id), 0) > 0 THEN 2
        WHEN DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND ISNULL((SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) FROM dbo.InvoiceDetails WHERE InvoiceId = Invoices.Id), 0) > ISNULL((SELECT SUM(Amount) FROM dbo.Payments WHERE InvoiceId = Invoices.Id), 0) THEN 3
        WHEN ISNULL((SELECT SUM(Amount) FROM dbo.Payments WHERE InvoiceId = Invoices.Id), 0) > 0 THEN 1
        ELSE 0
    END,
    UpdatedAt = SYSDATETIME();
GO

-- 1.1. Updated Recalculate procedure
CREATE OR ALTER PROCEDURE dbo.sp_Invoices_Recalculate
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE i
    SET 
        TotalAmount = ISNULL(d.TotalAmount, 0),
        PaidAmount = ISNULL(p.PaidAmount, 0),
        Status = CASE
            WHEN ISNULL(p.PaidAmount, 0) >= ISNULL(d.TotalAmount, 0) AND ISNULL(d.TotalAmount, 0) > 0 THEN 2
            WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(d.TotalAmount, 0) > ISNULL(p.PaidAmount, 0) THEN 3
            WHEN ISNULL(p.PaidAmount, 0) > 0 THEN 1
            ELSE 0
        END,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    OUTER APPLY (SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) AS TotalAmount FROM dbo.InvoiceDetails WHERE InvoiceId = i.Id) AS d
    OUTER APPLY (SELECT SUM(Amount) AS PaidAmount FROM dbo.Payments WHERE InvoiceId = i.Id) AS p
    WHERE i.Id = @Id;
END;
GO

-- 2. Update sp_Rooms_GetById to use calculated status
CREATE OR ALTER PROCEDURE dbo.sp_Rooms_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id, r.RoomNumber, r.BuildingId, b.Name AS BuildingName, r.Area, r.BasePrice AS RentPrice,
        r.BasePrice AS BasePrice,
        CASE WHEN EXISTS (SELECT 1 FROM dbo.Contracts AS ac WHERE ac.RoomId = r.Id AND ISNULL(ac.Status, 0) = 1)
             THEN 1
             ELSE CAST(ISNULL(r.Status, 0) AS INT)
        END AS Status,
        r.Description
    FROM dbo.Rooms AS r
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE r.Id = @Id;

    SELECT TOP 1
        c.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode,
        t.FullName AS TenantName,
        c.StartDate,
        c.EndDate,
        CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    WHERE c.RoomId = @Id AND ISNULL(c.Status, 0) = 1;

    SELECT TOP 5
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        CAST(i.BillingMonth AS INT) AS BillingMonth,
        CAST(i.BillingYear AS INT) AS BillingYear,
        i.TotalAmount,
        ISNULL(i.PaidAmount, 0) AS PaidAmount,
        CASE
            WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
            WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
            WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
            ELSE 0
        END AS Status
    FROM dbo.Invoices AS i
    WHERE i.ContractId IN
    (
        SELECT c.Id
        FROM dbo.Contracts AS c
        WHERE c.RoomId = @Id
    )
    ORDER BY i.BillingYear DESC, i.BillingMonth DESC;
END;
GO

-- 3. Update sp_Tenants_GetById to use calculated status
CREATE OR ALTER PROCEDURE dbo.sp_Tenants_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.Id, t.FullName, t.PhoneNumber, t.Email, t.IdCardNumber AS IdentityNumber, t.Dob AS DateOfBirth, t.PermanentAddress AS Address,
        ISNULL(SUM(ISNULL(i.PaidAmount, 0)), 0) AS TotalPaidAmount,
        ISNULL(SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)), 0) AS TotalDebtAmount
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.Contracts AS c ON c.TenantId = t.Id
    LEFT JOIN dbo.Invoices AS i ON i.ContractId = c.Id
    WHERE t.Id = @Id
    GROUP BY t.Id, t.FullName, t.PhoneNumber, t.Email, t.IdCardNumber, t.Dob, t.PermanentAddress;

    -- Current Contract
    SELECT TOP 1
        c.Id, UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode, t.FullName AS TenantName, r.RoomNumber, b.Name AS BuildingName,
        c.StartDate, c.EndDate, c.AgreedPrice AS RentPrice, ISNULL(c.DepositAmount, 0) AS DepositAmount, CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE c.TenantId = @Id AND ISNULL(c.Status, 0) = 1;

    -- All Contracts
    SELECT
        c.Id, UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode, t.FullName AS TenantName, r.RoomNumber, b.Name AS BuildingName,
        c.StartDate, c.EndDate, c.AgreedPrice AS RentPrice, ISNULL(c.DepositAmount, 0) AS DepositAmount, CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE c.TenantId = @Id
    ORDER BY c.CreatedAt DESC;

    -- Invoices
    SELECT
        i.Id, UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode, CAST(i.BillingMonth AS INT) AS BillingMonth, CAST(i.BillingYear AS INT) AS BillingYear,
        i.TotalAmount, ISNULL(i.PaidAmount, 0) AS PaidAmount,
        CASE
            WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
            WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
            WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
            ELSE 0
        END AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    WHERE c.TenantId = @Id
    ORDER BY i.BillingYear DESC, i.BillingMonth DESC;
END;
GO

-- 4. Update sp_Contracts_GetById to use calculated status
CREATE OR ALTER PROCEDURE dbo.sp_Contracts_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id, c.StartDate, c.EndDate, c.AgreedPrice AS RentPrice, ISNULL(c.DepositAmount, 0) AS DepositAmount, CAST(ISNULL(c.Status, 0) AS INT) AS Status,
        ISNULL(SUM(i.TotalAmount), 0) AS TotalInvoiceAmount,
        ISNULL(SUM(ISNULL(i.PaidAmount, 0)), 0) AS TotalPaidAmount,
        ISNULL(SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)), 0) AS TotalDebtAmount
    FROM dbo.Contracts AS c
    LEFT JOIN dbo.Invoices AS i ON i.ContractId = c.Id
    WHERE c.Id = @Id
    GROUP BY c.Id, c.StartDate, c.EndDate, c.AgreedPrice, c.DepositAmount, c.Status;

    SELECT t.Id, t.FullName, t.PhoneNumber, t.Email, t.IdCardNumber AS IdentityNumber, t.PermanentAddress AS Address
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    WHERE c.Id = @Id;

    SELECT r.Id, r.RoomNumber, r.BuildingId, b.Name AS BuildingName, r.Area, r.BasePrice AS RentPrice,
           CASE WHEN EXISTS (SELECT 1 FROM dbo.Contracts AS ac WHERE ac.RoomId = r.Id AND ISNULL(ac.Status, 0) = 1)
                THEN 1
                ELSE CAST(ISNULL(r.Status, 0) AS INT)
           END AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE c.Id = @Id;

    SELECT i.Id, UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode, CAST(i.BillingMonth AS INT) AS BillingMonth,
           CAST(i.BillingYear AS INT) AS BillingYear, i.TotalAmount, ISNULL(i.PaidAmount, 0) AS PaidAmount,
           CASE
                WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
                WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
                WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
                ELSE 0
           END AS Status
    FROM dbo.Invoices AS i
    WHERE i.ContractId = @Id
    ORDER BY i.BillingYear DESC, i.BillingMonth DESC;

    SELECT p.Id, p.InvoiceId, UPPER(LEFT(CONVERT(VARCHAR(36), p.InvoiceId), 8)) AS InvoiceCode, p.Amount,
           p.PaymentDate, CONVERT(NVARCHAR(50), p.PaymentMethod) AS Method, p.Note
    FROM dbo.Payments AS p
    INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
    WHERE i.ContractId = @Id
    ORDER BY p.PaymentDate DESC;
END;
GO

-- 5. Fix sp_Payments_Create NULL PaymentMethod issue
CREATE OR ALTER PROCEDURE dbo.sp_Payments_Create
    @InvoiceId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18, 2),
    @Method NVARCHAR(50) = NULL,
    @PaymentDate DATETIME = NULL,
    @Note NVARCHAR(255) = NULL,
    @ReferenceCode NVARCHAR(100) = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE Id = @InvoiceId)
    BEGIN
        RAISERROR(N'Hóa đơn không tồn tại', 16, 1);
        RETURN;
    END;

    IF @Amount <= 0
    BEGIN
        RAISERROR(N'So tien thanh toan phai lon hon 0', 16, 1);
        RETURN;
    END;

    SET @Id = NEWID();

    -- Use a safer conversion for PaymentMethod. If conversion fails, default to 1 (Cash)
    DECLARE @MethodCode TINYINT;
    SET @MethodCode = ISNULL(TRY_CONVERT(TINYINT, @Method), 1);

    -- Generate a unique reference code if not provided to avoid UQ constraint violation with multiple NULLs
    IF @ReferenceCode IS NULL
    BEGIN
        SET @ReferenceCode = 'PAY-' + UPPER(LEFT(REPLACE(CAST(@Id AS VARCHAR(36)), '-', ''), 10));
    END

    INSERT INTO dbo.Payments (Id, InvoiceId, Amount, PaymentDate, PaymentMethod, Note, ReferenceCode, CreatedAt)
    VALUES (@Id, @InvoiceId, @Amount, ISNULL(@PaymentDate, GETDATE()), @MethodCode, @Note, @ReferenceCode, SYSDATETIME());

    -- Ensure recalculation is performed
    EXEC dbo.sp_Invoices_Recalculate @InvoiceId;
END;
GO

-- 6. Recalculate all invoices one more time to be safe
DECLARE @InvoiceId UNIQUEIDENTIFIER;
DECLARE cur CURSOR FOR SELECT Id FROM dbo.Invoices;
OPEN cur;
FETCH NEXT FROM cur INTO @InvoiceId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.sp_Invoices_Recalculate @InvoiceId;
    FETCH NEXT FROM cur INTO @InvoiceId;
END;
CLOSE cur;
DEALLOCATE cur;
GO
