USE [Data_Base_Phontro];
GO

CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetSummary
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #DashboardInvoices
    (
        TotalAmount DECIMAL(18, 2) NOT NULL,
        PaidAmount DECIMAL(18, 2) NOT NULL,
        DueDate DATE NULL,
        Status INT NOT NULL
    );

    INSERT INTO #DashboardInvoices (TotalAmount, PaidAmount, DueDate, Status)
    SELECT ISNULL(TotalAmount, 0), ISNULL(PaidAmount, 0), DueDate, 0
    FROM dbo.Invoices;

    UPDATE #DashboardInvoices SET Status = 1 WHERE PaidAmount > 0;
    UPDATE #DashboardInvoices SET Status = 3 WHERE DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
    UPDATE #DashboardInvoices SET Status = 2 WHERE PaidAmount >= TotalAmount AND TotalAmount > 0;

    SELECT
        (SELECT COUNT(*) FROM dbo.Buildings) AS TotalBuildings,
        (SELECT COUNT(*) FROM dbo.Rooms) AS TotalRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 0) AS EmptyRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 1) AS RentedRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 2) AS MaintenanceRooms,
        (SELECT COUNT(*) FROM dbo.Tenants) AS TotalTenants,
        (SELECT COUNT(*) FROM dbo.Contracts WHERE ISNULL(Status, 0) = 1) AS ActiveContracts,
        (SELECT COUNT(*) FROM #DashboardInvoices WHERE Status IN (0, 1)) AS UnpaidInvoices,
        (SELECT COUNT(*) FROM #DashboardInvoices WHERE Status = 3) AS OverdueInvoices,
        ISNULL((SELECT SUM(ISNULL(PaidAmount, 0)) FROM dbo.Invoices), 0) AS TotalPaidAmount,
        ISNULL((SELECT SUM(TotalAmount - ISNULL(PaidAmount, 0)) FROM dbo.Invoices), 0) AS TotalDebtAmount;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_GetAll
    @Keyword NVARCHAR(100) = NULL,
    @ContractId UNIQUEIDENTIFIER = NULL,
    @TenantId UNIQUEIDENTIFIER = NULL,
    @RoomId UNIQUEIDENTIFIER = NULL,
    @BuildingId UNIQUEIDENTIFIER = NULL,
    @Month INT = NULL,
    @Year INT = NULL,
    @Status INT = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @TotalItems INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #InvoiceRows
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        InvoiceCode VARCHAR(8) NULL,
        BillingMonth INT NOT NULL,
        BillingYear INT NOT NULL,
        TenantName NVARCHAR(100) NOT NULL,
        RoomNumber NVARCHAR(50) NOT NULL,
        BuildingName NVARCHAR(100) NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL,
        PaidAmount DECIMAL(18, 2) NOT NULL,
        DueDate DATE NULL,
        Status INT NOT NULL
    );

    INSERT INTO #InvoiceRows
    (
        Id, InvoiceCode, BillingMonth, BillingYear, TenantName, RoomNumber,
        BuildingName, TotalAmount, PaidAmount, DueDate, Status
    )
    SELECT
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)),
        CAST(i.BillingMonth AS INT),
        CAST(i.BillingYear AS INT),
        t.FullName,
        r.RoomNumber,
        b.Name,
        ISNULL(i.TotalAmount, 0),
        ISNULL(i.PaidAmount, 0),
        i.DueDate,
        0
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE (@ContractId IS NULL OR i.ContractId = @ContractId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@BuildingId IS NULL OR r.BuildingId = @BuildingId)
      AND (@Month IS NULL OR i.BillingMonth = @Month)
      AND (@Year IS NULL OR i.BillingYear = @Year)
      AND (@Keyword IS NULL OR t.FullName LIKE N'%' + @Keyword + N'%' OR r.RoomNumber LIKE N'%' + @Keyword + N'%');

    UPDATE #InvoiceRows SET Status = 1 WHERE PaidAmount > 0;
    UPDATE #InvoiceRows SET Status = 3 WHERE DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
    UPDATE #InvoiceRows SET Status = 2 WHERE PaidAmount >= TotalAmount AND TotalAmount > 0;

    SELECT @TotalItems = COUNT(*)
    FROM #InvoiceRows
    WHERE @Status IS NULL OR Status = @Status;

    SELECT Id, InvoiceCode, BillingMonth, BillingYear, TenantName, RoomNumber, BuildingName, TotalAmount, PaidAmount, DueDate, Status
    FROM #InvoiceRows
    WHERE @Status IS NULL OR Status = @Status
    ORDER BY BillingYear DESC, BillingMonth DESC
    OFFSET ((@Page - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #InvoiceDetailHeader
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        InvoiceCode VARCHAR(8) NULL,
        BillingMonth INT NOT NULL,
        BillingYear INT NOT NULL,
        ContractId UNIQUEIDENTIFIER NOT NULL,
        ContractRentPrice DECIMAL(18, 2) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        TenantName NVARCHAR(100) NOT NULL,
        TenantPhoneNumber VARCHAR(15) NULL,
        RoomId UNIQUEIDENTIFIER NOT NULL,
        RoomNumber NVARCHAR(50) NOT NULL,
        BuildingName NVARCHAR(100) NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL,
        PaidAmount DECIMAL(18, 2) NOT NULL,
        DueDate DATE NULL,
        Status INT NOT NULL
    );

    INSERT INTO #InvoiceDetailHeader
    (
        Id, InvoiceCode, BillingMonth, BillingYear, ContractId, ContractRentPrice,
        TenantId, TenantName, TenantPhoneNumber, RoomId, RoomNumber, BuildingName,
        TotalAmount, PaidAmount, DueDate, Status
    )
    SELECT
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)),
        CAST(i.BillingMonth AS INT),
        CAST(i.BillingYear AS INT),
        i.ContractId,
        c.AgreedPrice,
        t.Id,
        t.FullName,
        t.PhoneNumber,
        r.Id,
        r.RoomNumber,
        b.Name,
        ISNULL(i.TotalAmount, 0),
        ISNULL(i.PaidAmount, 0),
        i.DueDate,
        0
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE i.Id = @Id;

    UPDATE #InvoiceDetailHeader SET Status = 1 WHERE PaidAmount > 0;
    UPDATE #InvoiceDetailHeader SET Status = 3 WHERE DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
    UPDATE #InvoiceDetailHeader SET Status = 2 WHERE PaidAmount >= TotalAmount AND TotalAmount > 0;

    SELECT * FROM #InvoiceDetailHeader;

    SELECT
        d.Id,
        d.ServiceId,
        ISNULL(s.Name, d.Description) AS ServiceName,
        d.Quantity,
        d.UnitPrice,
        ISNULL(d.Amount, d.Quantity * d.UnitPrice) AS Amount,
        CAST(NULL AS NVARCHAR(255)) AS Note
    FROM dbo.InvoiceDetails AS d
    LEFT JOIN dbo.Services AS s ON s.Id = d.ServiceId
    WHERE d.InvoiceId = @Id;

    SELECT Id, Amount, PaymentDate, CONVERT(NVARCHAR(50), PaymentMethod) AS Method, Note
    FROM dbo.Payments
    WHERE InvoiceId = @Id
    ORDER BY PaymentDate DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_Recalculate
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE i
    SET TotalAmount = ISNULL(d.TotalAmount, 0),
        PaidAmount = CASE
            WHEN ISNULL(d.TotalAmount, 0) > 0 AND ISNULL(p.PaidAmount, 0) > ISNULL(d.TotalAmount, 0) THEN ISNULL(d.TotalAmount, 0)
            WHEN ISNULL(d.TotalAmount, 0) <= 0 AND ISNULL(p.PaidAmount, 0) > 0 THEN 0
            ELSE ISNULL(p.PaidAmount, 0)
        END,
        Status = 0,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    OUTER APPLY (SELECT SUM(ISNULL(Amount, Quantity * UnitPrice)) AS TotalAmount FROM dbo.InvoiceDetails WHERE InvoiceId = i.Id) AS d
    OUTER APPLY (SELECT SUM(Amount) AS PaidAmount FROM dbo.Payments WHERE InvoiceId = i.Id) AS p
    WHERE i.Id = @Id;

    UPDATE dbo.Invoices SET Status = 1, UpdatedAt = SYSDATETIME() WHERE Id = @Id AND PaidAmount > 0;
    UPDATE dbo.Invoices SET Status = 3, UpdatedAt = SYSDATETIME() WHERE Id = @Id AND DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
    UPDATE dbo.Invoices SET Status = 2, UpdatedAt = SYSDATETIME() WHERE Id = @Id AND PaidAmount >= TotalAmount AND TotalAmount > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_GetOverdue
AS
BEGIN
    SET NOCOUNT ON;

    SELECT i.Id, UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode, CAST(i.BillingMonth AS INT) AS BillingMonth,
           CAST(i.BillingYear AS INT) AS BillingYear, t.FullName AS TenantName, r.RoomNumber, b.Name AS BuildingName,
           i.TotalAmount, ISNULL(i.PaidAmount, 0) AS PaidAmount, i.DueDate, CAST(3 AS INT) AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE i.DueDate IS NOT NULL
      AND i.DueDate < CAST(GETDATE() AS DATE)
      AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_GetUnpaid
AS
BEGIN
    SET NOCOUNT ON;

    SELECT i.Id, UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode, CAST(i.BillingMonth AS INT) AS BillingMonth,
           CAST(i.BillingYear AS INT) AS BillingYear, t.FullName AS TenantName, r.RoomNumber, b.Name AS BuildingName,
           i.TotalAmount, ISNULL(i.PaidAmount, 0) AS PaidAmount, i.DueDate,
           CAST(0 AS INT) AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE NOT (ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0)
      AND NOT (i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0));
END;
GO

UPDATE dbo.Invoices SET Status = 0, UpdatedAt = SYSDATETIME();
UPDATE dbo.Invoices SET Status = 1, UpdatedAt = SYSDATETIME() WHERE PaidAmount > 0;
UPDATE dbo.Invoices SET Status = 3, UpdatedAt = SYSDATETIME() WHERE DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
UPDATE dbo.Invoices SET Status = 2, UpdatedAt = SYSDATETIME() WHERE PaidAmount >= TotalAmount AND TotalAmount > 0;
GO
