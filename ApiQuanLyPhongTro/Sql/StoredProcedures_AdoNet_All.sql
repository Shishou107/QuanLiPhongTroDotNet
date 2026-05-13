/* Run this script in database QuanLyPhongTro before testing the ADO.NET API. */

CREATE OR ALTER PROCEDURE dbo.sp_Account_GetByLoginName
    @LoginName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 MaTK, TenDangNhap, MatKhauHash, HoTen, Email, TrangThai
    FROM dbo.TaiKhoan
    WHERE TenDangNhap = @LoginName OR Email = @LoginName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Account_UpdateLastLogin
    @Id INT
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.TaiKhoan
    SET LanDangNhapCuoi = GETDATE()
    WHERE MaTK = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Services_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Unit, UnitPrice AS DefaultPrice
    FROM dbo.Services
    ORDER BY Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Services_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Unit, UnitPrice AS DefaultPrice
    FROM dbo.Services
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Services_Create
    @Name NVARCHAR(100),
    @Unit NVARCHAR(50),
    @DefaultPrice DECIMAL(18, 2),
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Id = NEWID();

    INSERT INTO dbo.Services (Id, Name, Unit, UnitPrice, CreatedAt)
    VALUES (@Id, @Name, @Unit, @DefaultPrice, SYSDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Services_Update
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @Unit NVARCHAR(50),
    @DefaultPrice DECIMAL(18, 2)
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.Services
    SET Name = @Name,
        Unit = @Unit,
        UnitPrice = @DefaultPrice,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Services_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    IF EXISTS (SELECT 1 FROM dbo.InvoiceDetails WHERE ServiceId = @Id)
    BEGIN
        RAISERROR(N'Không thể xóa dịch vụ đã phát sinh trong hóa đơn', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.Services WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Buildings) AS TotalBuildings,
        (SELECT COUNT(*) FROM dbo.Rooms) AS TotalRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 0) AS EmptyRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 1) AS RentedRooms,
        (SELECT COUNT(*) FROM dbo.Rooms WHERE ISNULL(Status, 0) = 2) AS MaintenanceRooms,
        (SELECT COUNT(*) FROM dbo.Tenants) AS TotalTenants,
        (SELECT COUNT(*) FROM dbo.Contracts WHERE ISNULL(Status, 0) = 1) AS ActiveContracts,
        (SELECT COUNT(*) FROM dbo.Invoices WHERE
            CASE
                WHEN ISNULL(PaidAmount, 0) >= ISNULL(TotalAmount, 0) AND ISNULL(TotalAmount, 0) > 0 THEN 2
                WHEN DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND ISNULL(TotalAmount, 0) > ISNULL(PaidAmount, 0) THEN 3
                WHEN ISNULL(PaidAmount, 0) > 0 THEN 1
                ELSE 0
            END IN (0, 1)) AS UnpaidInvoices,
        (SELECT COUNT(*) FROM dbo.Invoices WHERE
            CASE
                WHEN ISNULL(PaidAmount, 0) >= ISNULL(TotalAmount, 0) AND ISNULL(TotalAmount, 0) > 0 THEN 2
                WHEN DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND ISNULL(TotalAmount, 0) > ISNULL(PaidAmount, 0) THEN 3
                WHEN ISNULL(PaidAmount, 0) > 0 THEN 1
                ELSE 0
            END = 3) AS OverdueInvoices,
        ISNULL((SELECT SUM(ISNULL(PaidAmount, 0)) FROM dbo.Invoices), 0) AS TotalPaidAmount,
        ISNULL((SELECT SUM(TotalAmount - ISNULL(PaidAmount, 0)) FROM dbo.Invoices), 0) AS TotalDebtAmount;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetMonthlyRevenue
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.Month,
        ISNULL(SUM(i.TotalAmount), 0) AS TotalInvoiceAmount,
        ISNULL(SUM(ISNULL(i.PaidAmount, 0)), 0) AS TotalPaidAmount,
        ISNULL(SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)), 0) AS TotalDebtAmount
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) AS v(Month)
    LEFT JOIN dbo.Invoices AS i ON i.BillingYear = @Year AND i.BillingMonth = v.Month
    GROUP BY v.Month
    ORDER BY v.Month;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetRoomStatusStatistics
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.Status,
        CASE v.Status WHEN 0 THEN N'Trống' WHEN 1 THEN N'Đang thuê' WHEN 2 THEN N'Bảo trì' ELSE N'Không xác định' END AS StatusText,
        COUNT(r.Id) AS Count
    FROM (VALUES (0),(1),(2)) AS v(Status)
    LEFT JOIN dbo.Rooms AS r ON ISNULL(r.Status, 0) = v.Status
    GROUP BY v.Status
    ORDER BY v.Status;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_GetAll
    @BuildingId UNIQUEIDENTIFIER = NULL,
    @Status INT = NULL,
    @Keyword NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @TotalItems INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalItems = COUNT(*)
    FROM dbo.Rooms AS r
    LEFT JOIN dbo.Contracts AS c ON c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1
    WHERE (@BuildingId IS NULL OR r.BuildingId = @BuildingId)
      AND (@Status IS NULL OR CASE WHEN c.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END = @Status)
      AND (@Keyword IS NULL OR r.RoomNumber LIKE N'%' + @Keyword + N'%');

    SELECT
        r.Id, r.RoomNumber, r.BuildingId, b.Name AS BuildingName, r.Area, r.BasePrice AS RentPrice,
        CASE WHEN c.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END AS Status,
        ISNULL(t.FullName, N'Chưa có') AS CurrentTenantName,
        c.Id AS CurrentContractId
    FROM dbo.Rooms AS r
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    LEFT JOIN dbo.Contracts AS c ON c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1
    LEFT JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    WHERE (@BuildingId IS NULL OR r.BuildingId = @BuildingId)
      AND (@Status IS NULL OR CASE WHEN c.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END = @Status)
      AND (@Keyword IS NULL OR r.RoomNumber LIKE N'%' + @Keyword + N'%')
    ORDER BY r.RoomNumber
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

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
        CAST(ISNULL(i.Status, 0) AS INT) AS Status
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

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_GetAvailable
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id, r.RoomNumber, r.BuildingId, b.Name AS BuildingName, r.Area, r.BasePrice AS RentPrice,
        CAST(ISNULL(r.Status, 0) AS INT) AS Status,
        CAST(NULL AS NVARCHAR(100)) AS CurrentTenantName,
        CAST(NULL AS UNIQUEIDENTIFIER) AS CurrentContractId
    FROM dbo.Rooms AS r
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE ISNULL(r.Status, 0) = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.Contracts AS c WHERE c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1)
    ORDER BY b.Name, r.RoomNumber;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_Create
    @BuildingId UNIQUEIDENTIFIER,
    @RoomNumber NVARCHAR(50),
    @Capacity INT = NULL,
    @Area DECIMAL(10, 2) = NULL,
    @BasePrice DECIMAL(18, 2),
    @Description NVARCHAR(MAX) = NULL,
    @Status INT = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Id = NEWID();

    INSERT INTO dbo.Rooms (Id, BuildingId, RoomNumber, Capacity, Area, BasePrice, Description, Status, CreatedAt)
    VALUES (@Id, @BuildingId, @RoomNumber, ISNULL(@Capacity, 1), @Area, @BasePrice, @Description, ISNULL(@Status, 0), SYSDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_Update
    @Id UNIQUEIDENTIFIER,
    @BuildingId UNIQUEIDENTIFIER,
    @RoomNumber NVARCHAR(50),
    @Capacity INT = NULL,
    @Area DECIMAL(10, 2) = NULL,
    @BasePrice DECIMAL(18, 2),
    @Description NVARCHAR(MAX) = NULL,
    @Status INT = NULL
AS
BEGIN
    SET NOCOUNT OFF;

    IF ISNULL(@Status, (SELECT Status FROM dbo.Rooms WHERE Id = @Id)) = 0
       AND EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @Id AND ISNULL(Status, 0) = 1)
    BEGIN
        RAISERROR(N'Phòng đang có hợp đồng hiệu lực, không thể chuyển sang Trống. Hãy kết thúc hoặc hủy hợp đồng trước.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Rooms
    SET BuildingId = @BuildingId,
        RoomNumber = @RoomNumber,
        Capacity = ISNULL(@Capacity, Capacity),
        Area = @Area,
        BasePrice = @BasePrice,
        Description = @Description,
        Status = ISNULL(@Status, Status),
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_UpdateStatus
    @Id UNIQUEIDENTIFIER,
    @Status INT
AS
BEGIN
    SET NOCOUNT OFF;

    IF @Status = 0
       AND EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @Id AND ISNULL(Status, 0) = 1)
    BEGIN
        RAISERROR(N'Phòng đang có hợp đồng hiệu lực, không thể chuyển sang Trống. Hãy kết thúc hoặc hủy hợp đồng trước.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Rooms
    SET Status = @Status,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    IF EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @Id)
    BEGIN
        RAISERROR(N'Không thể xóa phòng đã có hợp đồng', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.Rooms WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Tenants_GetAll
    @Keyword NVARCHAR(100) = NULL,
    @Status INT = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @TotalItems INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH TenantRows AS
    (
        SELECT
            t.Id,
            t.FullName,
            t.PhoneNumber,
            t.Email,
            t.IdCardNumber AS IdentityNumber,
            t.PermanentAddress AS Address,
            ISNULL(r.RoomNumber, N'Chưa có') AS CurrentRoom,
            ISNULL(b.Name, N'Chưa có') AS CurrentBuilding,
            CASE WHEN c.Id IS NULL THEN N'Không thuê' ELSE N'Đang thuê' END AS StatusText,
            CASE WHEN c.Id IS NULL THEN 0 ELSE 1 END AS TenantStatus
        FROM dbo.Tenants AS t
        LEFT JOIN dbo.Contracts AS c ON c.TenantId = t.Id AND ISNULL(c.Status, 0) = 1
        LEFT JOIN dbo.Rooms AS r ON r.Id = c.RoomId
        LEFT JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
        WHERE @Keyword IS NULL
           OR t.FullName LIKE N'%' + @Keyword + N'%'
           OR t.PhoneNumber LIKE N'%' + @Keyword + N'%'
           OR ISNULL(t.Email, '') LIKE N'%' + @Keyword + N'%'
           OR t.IdCardNumber LIKE N'%' + @Keyword + N'%'
    )
    SELECT @TotalItems = COUNT(*)
    FROM TenantRows
    WHERE @Status IS NULL OR TenantStatus = @Status;

    ;WITH TenantRows AS
    (
        SELECT
            t.Id,
            t.FullName,
            t.PhoneNumber,
            t.Email,
            t.IdCardNumber AS IdentityNumber,
            t.PermanentAddress AS Address,
            ISNULL(r.RoomNumber, N'Chưa có') AS CurrentRoom,
            ISNULL(b.Name, N'Chưa có') AS CurrentBuilding,
            CASE WHEN c.Id IS NULL THEN N'Không thuê' ELSE N'Đang thuê' END AS StatusText,
            CASE WHEN c.Id IS NULL THEN 0 ELSE 1 END AS TenantStatus
        FROM dbo.Tenants AS t
        LEFT JOIN dbo.Contracts AS c ON c.TenantId = t.Id AND ISNULL(c.Status, 0) = 1
        LEFT JOIN dbo.Rooms AS r ON r.Id = c.RoomId
        LEFT JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
        WHERE @Keyword IS NULL
           OR t.FullName LIKE N'%' + @Keyword + N'%'
           OR t.PhoneNumber LIKE N'%' + @Keyword + N'%'
           OR ISNULL(t.Email, '') LIKE N'%' + @Keyword + N'%'
           OR t.IdCardNumber LIKE N'%' + @Keyword + N'%'
    )
    SELECT Id, FullName, PhoneNumber, Email, IdentityNumber, Address, CurrentRoom, CurrentBuilding, TenantStatus, StatusText
    FROM TenantRows
    WHERE @Status IS NULL OR TenantStatus = @Status
    ORDER BY FullName
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Tenants_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.Id,
        t.FullName,
        t.PhoneNumber,
        t.Email,
        t.IdCardNumber AS IdentityNumber,
        t.Dob AS DateOfBirth,
        t.PermanentAddress AS Address,
        ISNULL(SUM(ISNULL(i.PaidAmount, 0)), 0) AS TotalPaidAmount,
        ISNULL(SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)), 0) AS TotalDebtAmount
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.Contracts AS c ON c.TenantId = t.Id
    LEFT JOIN dbo.Invoices AS i ON i.ContractId = c.Id
    WHERE t.Id = @Id
    GROUP BY t.Id, t.FullName, t.PhoneNumber, t.Email, t.IdCardNumber, t.Dob, t.PermanentAddress;

    SELECT TOP 1
        c.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        b.Name AS BuildingName,
        c.StartDate,
        c.EndDate,
        c.AgreedPrice AS RentPrice,
        ISNULL(c.DepositAmount, 0) AS DepositAmount,
        CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE c.TenantId = @Id AND ISNULL(c.Status, 0) = 1;

    SELECT
        c.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        b.Name AS BuildingName,
        c.StartDate,
        c.EndDate,
        c.AgreedPrice AS RentPrice,
        ISNULL(c.DepositAmount, 0) AS DepositAmount,
        CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE c.TenantId = @Id
    ORDER BY c.CreatedAt DESC;

    SELECT
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        CAST(i.BillingMonth AS INT) AS BillingMonth,
        CAST(i.BillingYear AS INT) AS BillingYear,
        i.TotalAmount,
        ISNULL(i.PaidAmount, 0) AS PaidAmount,
        CAST(ISNULL(i.Status, 0) AS INT) AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    WHERE c.TenantId = @Id
    ORDER BY i.BillingYear DESC, i.BillingMonth DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Tenants_Create
    @FullName NVARCHAR(100),
    @PhoneNumber VARCHAR(15),
    @Email VARCHAR(100) = NULL,
    @IdentityNumber VARCHAR(20),
    @DateOfBirth DATE = NULL,
    @Address NVARCHAR(255) = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Id = NEWID();

    INSERT INTO dbo.Tenants (Id, FullName, PhoneNumber, Email, IdCardNumber, Dob, PermanentAddress, CreatedAt)
    VALUES (@Id, @FullName, @PhoneNumber, @Email, @IdentityNumber, @DateOfBirth, @Address, SYSDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Tenants_Update
    @Id UNIQUEIDENTIFIER,
    @FullName NVARCHAR(100),
    @PhoneNumber VARCHAR(15),
    @Email VARCHAR(100) = NULL,
    @IdentityNumber VARCHAR(20),
    @DateOfBirth DATE = NULL,
    @Address NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.Tenants
    SET FullName = @FullName,
        PhoneNumber = @PhoneNumber,
        Email = @Email,
        IdCardNumber = @IdentityNumber,
        Dob = @DateOfBirth,
        PermanentAddress = @Address,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Tenants_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    IF EXISTS (SELECT 1 FROM dbo.Contracts WHERE TenantId = @Id)
    BEGIN
        RAISERROR(N'Không thể xóa khách thuê đã có hợp đồng', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.Tenants WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_GetAll
    @Keyword NVARCHAR(100) = NULL,
    @RoomId UNIQUEIDENTIFIER = NULL,
    @TenantId UNIQUEIDENTIFIER = NULL,
    @Status INT = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @TotalItems INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalItems = COUNT(*)
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    WHERE (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@Status IS NULL OR ISNULL(c.Status, 0) = @Status)
      AND (@Keyword IS NULL OR t.FullName LIKE N'%' + @Keyword + N'%' OR r.RoomNumber LIKE N'%' + @Keyword + N'%');

    SELECT
        c.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        b.Name AS BuildingName,
        c.StartDate,
        c.EndDate,
        c.AgreedPrice AS RentPrice,
        ISNULL(c.DepositAmount, 0) AS DepositAmount,
        CAST(ISNULL(c.Status, 0) AS INT) AS Status
    FROM dbo.Contracts AS c
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@Status IS NULL OR ISNULL(c.Status, 0) = @Status)
      AND (@Keyword IS NULL OR t.FullName LIKE N'%' + @Keyword + N'%' OR r.RoomNumber LIKE N'%' + @Keyword + N'%')
    ORDER BY c.CreatedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), c.Id), 8)) AS ContractCode,
        c.StartDate,
        c.EndDate,
        c.AgreedPrice AS RentPrice,
        ISNULL(c.DepositAmount, 0) AS DepositAmount,
        CAST(ISNULL(c.Status, 0) AS INT) AS Status,
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
           CAST(ISNULL(i.Status, 0) AS INT) AS Status
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

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_Create
    @TenantId UNIQUEIDENTIFIER,
    @RoomId UNIQUEIDENTIFIER,
    @StartDate DATE,
    @EndDate DATE,
    @RentPrice DECIMAL(18, 2),
    @DepositAmount DECIMAL(18, 2),
    @Note NVARCHAR(MAX) = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE Id = @RoomId)
    BEGIN
        RAISERROR(N'Phòng không tồn tại', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId)
    BEGIN
        RAISERROR(N'Khách thuê không tồn tại', 16, 1);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @RoomId AND ISNULL(Status, 0) = 1)
    BEGIN
        RAISERROR(N'Phòng đã có hợp đồng đang hiệu lực', 16, 1);
        RETURN;
    END;

    SET @Id = NEWID();

    INSERT INTO dbo.Contracts (Id, RoomId, TenantId, StartDate, EndDate, DepositAmount, AgreedPrice, Status, Note, CreatedAt)
    VALUES (@Id, @RoomId, @TenantId, @StartDate, @EndDate, @DepositAmount, @RentPrice, 1, @Note, SYSDATETIME());

    UPDATE dbo.Rooms SET Status = 1, UpdatedAt = SYSDATETIME() WHERE Id = @RoomId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_Update
    @Id UNIQUEIDENTIFIER,
    @StartDate DATE,
    @EndDate DATE,
    @RentPrice DECIMAL(18, 2),
    @DepositAmount DECIMAL(18, 2),
    @Status INT,
    @Note NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.Contracts
    SET StartDate = @StartDate,
        EndDate = @EndDate,
        AgreedPrice = @RentPrice,
        DepositAmount = @DepositAmount,
        Status = @Status,
        Note = @Note,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_Cancel
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @RoomId UNIQUEIDENTIFIER;
    SELECT @RoomId = RoomId FROM dbo.Contracts WHERE Id = @Id;

    UPDATE dbo.Contracts SET Status = 2, UpdatedAt = SYSDATETIME() WHERE Id = @Id;

    IF @RoomId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @RoomId AND Status = 1 AND Id <> @Id)
        UPDATE dbo.Rooms SET Status = 0, UpdatedAt = SYSDATETIME() WHERE Id = @RoomId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Contracts_Finish
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @RoomId UNIQUEIDENTIFIER;
    SELECT @RoomId = RoomId FROM dbo.Contracts WHERE Id = @Id;

    UPDATE dbo.Contracts SET Status = 0, UpdatedAt = SYSDATETIME() WHERE Id = @Id;

    IF @RoomId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = @RoomId AND Status = 1 AND Id <> @Id)
        UPDATE dbo.Rooms SET Status = 0, UpdatedAt = SYSDATETIME() WHERE Id = @RoomId;
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

    SELECT @TotalItems = COUNT(*)
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
      AND (@Status IS NULL OR
            CASE
                WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
                WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
                WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
                ELSE 0
            END = @Status)
      AND (@Keyword IS NULL OR t.FullName LIKE N'%' + @Keyword + N'%' OR r.RoomNumber LIKE N'%' + @Keyword + N'%');

    SELECT
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        CAST(i.BillingMonth AS INT) AS BillingMonth,
        CAST(i.BillingYear AS INT) AS BillingYear,
        t.FullName AS TenantName,
        r.RoomNumber,
        b.Name AS BuildingName,
        i.TotalAmount,
        ISNULL(i.PaidAmount, 0) AS PaidAmount,
        i.DueDate,
        CASE
            WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
            WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
            WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
            ELSE 0
        END AS Status
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
      AND (@Status IS NULL OR
            CASE
                WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
                WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
                WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
                ELSE 0
            END = @Status)
      AND (@Keyword IS NULL OR t.FullName LIKE N'%' + @Keyword + N'%' OR r.RoomNumber LIKE N'%' + @Keyword + N'%')
    ORDER BY i.BillingYear DESC, i.BillingMonth DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.Id,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        CAST(i.BillingMonth AS INT) AS BillingMonth,
        CAST(i.BillingYear AS INT) AS BillingYear,
        i.ContractId,
        c.AgreedPrice AS ContractRentPrice,
        t.Id AS TenantId,
        t.FullName AS TenantName,
        t.PhoneNumber AS TenantPhoneNumber,
        r.Id AS RoomId,
        r.RoomNumber,
        b.Name AS BuildingName,
        i.TotalAmount,
        ISNULL(i.PaidAmount, 0) AS PaidAmount,
        i.DueDate,
        CASE
            WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
            WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
            WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
            ELSE 0
        END AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE i.Id = @Id;

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

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_Create
    @ContractId UNIQUEIDENTIFIER,
    @BillingMonth INT,
    @BillingYear INT,
    @DueDate DATE = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE Id = @ContractId)
    BEGIN
        RAISERROR(N'Hợp đồng không tồn tại', 16, 1);
        RETURN;
    END;

    -- Check if invoice already exists for this month
    SELECT @Id = Id FROM dbo.Invoices 
    WHERE ContractId = @ContractId AND BillingMonth = @BillingMonth AND BillingYear = @BillingYear;

    IF @Id IS NULL
    BEGIN
        SET @Id = NEWID();
        INSERT INTO dbo.Invoices (Id, ContractId, BillingMonth, BillingYear, TotalAmount, PaidAmount, DueDate, Status, CreatedAt)
        VALUES (@Id, @ContractId, @BillingMonth, @BillingYear, 0, 0, @DueDate, 0, SYSDATETIME());
    END
    ELSE
    BEGIN
        -- If exists, optionally update the DueDate if provided
        IF @DueDate IS NOT NULL
        BEGIN
            UPDATE dbo.Invoices SET DueDate = @DueDate, UpdatedAt = SYSDATETIME() WHERE Id = @Id;
        END
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_Update
    @Id UNIQUEIDENTIFIER,
    @BillingMonth INT,
    @BillingYear INT,
    @DueDate DATE = NULL,
    @Status INT
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.Invoices
    SET BillingMonth = @BillingMonth,
        BillingYear = @BillingYear,
        DueDate = @DueDate,
        Status = @Status,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_ClearDetails
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.InvoiceDetails WHERE InvoiceId = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_AddDetail
    @InvoiceId UNIQUEIDENTIFIER,
    @ServiceId UNIQUEIDENTIFIER,
    @Quantity DECIMAL(10, 2),
    @UnitPrice DECIMAL(18, 2),
    @Note NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Description NVARCHAR(255);
    DECLARE @EffectiveUnitPrice DECIMAL(18, 2);

    SELECT
        @Description = Name,
        @EffectiveUnitPrice = UnitPrice
    FROM dbo.Services
    WHERE Id = @ServiceId;

    IF @Description IS NULL
    BEGIN
        RAISERROR(N'Dịch vụ không tồn tại', 16, 1);
        RETURN;
    END;

    IF @Quantity <= 0
    BEGIN
        RAISERROR(N'Số lượng phải lớn hơn 0', 16, 1);
        RETURN;
    END;

    IF @UnitPrice > 0
        SET @EffectiveUnitPrice = @UnitPrice;

    -- Allow 0 price if it's explicitly set or intended
    SET @EffectiveUnitPrice = ISNULL(@EffectiveUnitPrice, 0);

    -- Check if service already exists in this invoice
    IF EXISTS (SELECT 1 FROM dbo.InvoiceDetails WHERE InvoiceId = @InvoiceId AND ServiceId = @ServiceId)
    BEGIN
        UPDATE dbo.InvoiceDetails
        SET Quantity = Quantity + @Quantity,
            UnitPrice = @EffectiveUnitPrice, -- Update with latest price
            Note = CASE WHEN @Note IS NOT NULL THEN ISNULL(Note + '; ', '') + @Note ELSE Note END,
            UpdatedAt = SYSDATETIME()
        WHERE InvoiceId = @InvoiceId AND ServiceId = @ServiceId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.InvoiceDetails (Id, InvoiceId, ServiceId, Description, Quantity, UnitPrice, CreatedAt)
        VALUES (NEWID(), @InvoiceId, @ServiceId, @Description, @Quantity, @EffectiveUnitPrice, SYSDATETIME());
    END
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

    UPDATE dbo.Invoices SET Status = 1, UpdatedAt = SYSDATETIME() WHERE Id = @Id AND PaidAmount > 0 AND TotalAmount > PaidAmount;
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
           i.TotalAmount, ISNULL(i.PaidAmount, 0) AS PaidAmount, i.DueDate,
           CASE
               WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
               WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
               WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
               ELSE 0
           END AS Status
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
           CASE
               WHEN ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0 THEN 2
               WHEN i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0) THEN 3
               WHEN ISNULL(i.PaidAmount, 0) > 0 THEN 1
               ELSE 0
           END AS Status
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    WHERE NOT (ISNULL(i.PaidAmount, 0) >= ISNULL(i.TotalAmount, 0) AND ISNULL(i.TotalAmount, 0) > 0)
      AND NOT (i.DueDate IS NOT NULL AND i.DueDate < CAST(GETDATE() AS DATE) AND ISNULL(i.TotalAmount, 0) > ISNULL(i.PaidAmount, 0));
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Invoices_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    IF EXISTS (SELECT 1 FROM dbo.Payments WHERE InvoiceId = @Id)
    BEGIN
        RAISERROR(N'Không thể xóa hóa đơn đã có thanh toán', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.InvoiceDetails WHERE InvoiceId = @Id;
    DELETE FROM dbo.Invoices WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payments_GetAll
    @InvoiceId UNIQUEIDENTIFIER = NULL,
    @TenantId UNIQUEIDENTIFIER = NULL,
    @RoomId UNIQUEIDENTIFIER = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @Method NVARCHAR(50) = NULL,
    @Keyword NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 10,
    @TotalItems INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalItems = COUNT(*)
    FROM dbo.Payments AS p
    INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    WHERE (@InvoiceId IS NULL OR p.InvoiceId = @InvoiceId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@FromDate IS NULL OR p.PaymentDate >= @FromDate)
      AND (@ToDate IS NULL OR p.PaymentDate <= @ToDate)
      AND (@Method IS NULL OR p.Method = @Method)
      AND (@Keyword IS NULL OR t.FullName LIKE '%' + @Keyword + '%' OR UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) LIKE '%' + @Keyword + '%');

    SELECT
        p.Id,
        p.InvoiceId,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        p.Amount,
        p.PaymentDate,
        p.Method,
        p.Note
    FROM dbo.Payments AS p
    INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    WHERE (@InvoiceId IS NULL OR p.InvoiceId = @InvoiceId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@FromDate IS NULL OR p.PaymentDate >= @FromDate)
      AND (@ToDate IS NULL OR p.PaymentDate <= @ToDate)
      AND (@Method IS NULL OR p.Method = @Method)
      AND (@Keyword IS NULL OR t.FullName LIKE '%' + @Keyword + '%' OR UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) LIKE '%' + @Keyword + '%');

    SELECT
        p.Id,
        p.InvoiceId,
        UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        p.Amount,
        p.PaymentDate,
        p.Method,
        p.Note
    FROM dbo.Payments AS p
    INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    WHERE (@InvoiceId IS NULL OR p.InvoiceId = @InvoiceId)
      AND (@TenantId IS NULL OR c.TenantId = @TenantId)
      AND (@RoomId IS NULL OR c.RoomId = @RoomId)
      AND (@FromDate IS NULL OR p.PaymentDate >= @FromDate)
      AND (@ToDate IS NULL OR p.PaymentDate <= @ToDate)
      AND (@Method IS NULL OR p.Method = @Method)
      AND (@Keyword IS NULL OR t.FullName LIKE '%' + @Keyword + '%' OR UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) LIKE '%' + @Keyword + '%')
    ORDER BY p.PaymentDate DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payments_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.InvoiceId,
        UPPER(LEFT(CONVERT(VARCHAR(36), p.InvoiceId), 8)) AS InvoiceCode,
        t.FullName AS TenantName,
        r.RoomNumber,
        p.Amount,
        CONVERT(NVARCHAR(50), p.PaymentMethod) AS Method,
        p.PaymentDate,
        p.Note
    FROM dbo.Payments AS p
    INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    WHERE p.Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payments_Create
    @InvoiceId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18, 2),
    @Method NVARCHAR(50) = NULL,
    @PaymentDate DATETIME = NULL,
    @Note NVARCHAR(255) = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalAmount DECIMAL(18, 2);
    DECLARE @PaidAmount DECIMAL(18, 2);
    DECLARE @DebtAmount DECIMAL(18, 2);
    DECLARE @MethodCode TINYINT;

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE Id = @InvoiceId)
    BEGIN
        RAISERROR(N'Hóa đơn không tồn tại', 16, 1);
        RETURN;
    END;

    IF @Amount <= 0
    BEGIN
        RAISERROR(N'Số tiền thanh toán phải lớn hơn 0', 16, 1);
        RETURN;
    END;

    EXEC dbo.sp_Invoices_Recalculate @InvoiceId;

    SELECT
        @TotalAmount = ISNULL(TotalAmount, 0),
        @PaidAmount = ISNULL(PaidAmount, 0)
    FROM dbo.Invoices
    WHERE Id = @InvoiceId;

    SET @DebtAmount = ISNULL(@TotalAmount, 0) - ISNULL(@PaidAmount, 0);

    IF ISNULL(@TotalAmount, 0) <= 0
    BEGIN
        RAISERROR(N'Hóa đơn chưa có tổng tiền, vui lòng cập nhật chi tiết hóa đơn trước khi thanh toán', 16, 1);
        RETURN;
    END;

    IF @DebtAmount <= 0
    BEGIN
        RAISERROR(N'Hóa đơn đã thanh toán đủ', 16, 1);
        RETURN;
    END;

    IF @Amount > @DebtAmount
    BEGIN
        RAISERROR(N'Số tiền thanh toán vượt quá số tiền còn nợ', 16, 1);
        RETURN;
    END;

    SET @MethodCode = TRY_CONVERT(TINYINT, @Method);
    IF @MethodCode NOT IN (1, 2, 3)
        SET @MethodCode = 1;

    SET @Id = NEWID();

    INSERT INTO dbo.Payments (Id, InvoiceId, Amount, PaymentDate, PaymentMethod, Note, CreatedAt)
    VALUES (@Id, @InvoiceId, @Amount, ISNULL(@PaymentDate, GETDATE()), @MethodCode, @Note, SYSDATETIME());

    EXEC dbo.sp_Invoices_Recalculate @InvoiceId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payments_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @InvoiceId UNIQUEIDENTIFIER;
    SELECT @InvoiceId = InvoiceId FROM dbo.Payments WHERE Id = @Id;

    DELETE FROM dbo.Payments WHERE Id = @Id;

    IF @InvoiceId IS NOT NULL
        EXEC dbo.sp_Invoices_Recalculate @InvoiceId;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Payments_UpdateInvoiceSummary
ON dbo.Payments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted WHERE Amount <= 0)
    BEGIN
        RAISERROR(N'Số tiền thanh toán phải lớn hơn 0', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    DECLARE @ChangedInvoices TABLE (InvoiceId UNIQUEIDENTIFIER PRIMARY KEY);

    INSERT INTO @ChangedInvoices (InvoiceId)
    SELECT DISTINCT InvoiceId
    FROM inserted
    WHERE InvoiceId IS NOT NULL;

    INSERT INTO @ChangedInvoices (InvoiceId)
    SELECT DISTINCT d.InvoiceId
    FROM deleted AS d
    WHERE d.InvoiceId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM @ChangedInvoices AS c WHERE c.InvoiceId = d.InvoiceId);

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS p
        INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
        WHERE ISNULL(i.TotalAmount, 0) <= 0
    )
    BEGIN
        RAISERROR(N'Hóa đơn chưa có tổng tiền, vui lòng cập nhật chi tiết hóa đơn trước khi thanh toán', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Invoices AS i
        INNER JOIN @ChangedInvoices AS c ON c.InvoiceId = i.Id
        OUTER APPLY (SELECT SUM(Amount) AS PaidAmount FROM dbo.Payments WHERE InvoiceId = i.Id) AS p
        WHERE ISNULL(i.TotalAmount, 0) > 0
          AND ISNULL(p.PaidAmount, 0) > ISNULL(i.TotalAmount, 0)
    )
    BEGIN
        RAISERROR(N'Số tiền thanh toán vượt quá số tiền còn nợ', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    UPDATE i
    SET PaidAmount = ISNULL(p.PaidAmount, 0),
        Status = 0,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    INNER JOIN @ChangedInvoices AS c ON c.InvoiceId = i.Id
    OUTER APPLY (SELECT SUM(Amount) AS PaidAmount FROM dbo.Payments WHERE InvoiceId = i.Id) AS p;

    UPDATE i
    SET Status = 1,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    INNER JOIN @ChangedInvoices AS c ON c.InvoiceId = i.Id
    WHERE i.PaidAmount > 0 AND i.TotalAmount > i.PaidAmount;

    UPDATE i
    SET Status = 3,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    INNER JOIN @ChangedInvoices AS c ON c.InvoiceId = i.Id
    WHERE i.DueDate IS NOT NULL
      AND i.DueDate < CAST(GETDATE() AS DATE)
      AND i.TotalAmount > i.PaidAmount;

    UPDATE i
    SET Status = 2,
        UpdatedAt = SYSDATETIME()
    FROM dbo.Invoices AS i
    INNER JOIN @ChangedInvoices AS c ON c.InvoiceId = i.Id
    WHERE i.PaidAmount >= i.TotalAmount
      AND i.TotalAmount > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reports_RevenueByMonth
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_Dashboard_GetMonthlyRevenue @Year;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reports_DebtByTenant
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.TenantId,
        t.FullName AS TenantName,
        t.PhoneNumber,
        r.RoomNumber,
        b.Name AS BuildingName,
        SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)) AS TotalDebtAmount
    FROM dbo.Invoices AS i
    INNER JOIN dbo.Contracts AS c ON c.Id = i.ContractId
    INNER JOIN dbo.Tenants AS t ON t.Id = c.TenantId
    INNER JOIN dbo.Rooms AS r ON r.Id = c.RoomId
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    GROUP BY c.TenantId, t.FullName, t.PhoneNumber, r.RoomNumber, b.Name
    HAVING SUM(i.TotalAmount - ISNULL(i.PaidAmount, 0)) > 0
    ORDER BY TotalDebtAmount DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reports_RoomOccupancy
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalRooms DECIMAL(18, 2);
    SELECT @TotalRooms = COUNT(*) FROM dbo.Rooms;

    SELECT
        CASE v.Status WHEN 0 THEN N'Trống' WHEN 1 THEN N'Đang thuê' WHEN 2 THEN N'Bảo trì' ELSE N'Không xác định' END AS StatusText,
        COUNT(r.Id) AS Count,
        CASE WHEN @TotalRooms = 0 THEN 0 ELSE ROUND(COUNT(r.Id) / @TotalRooms * 100, 2) END AS Percentage
    FROM (VALUES (0),(1),(2)) AS v(Status)
    LEFT JOIN dbo.Rooms AS r ON ISNULL(r.Status, 0) = v.Status
    GROUP BY v.Status
    ORDER BY v.Status;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reports_ServiceUsage
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.ServiceId,
        d.Description AS ServiceName,
        SUM(d.Quantity) AS TotalQuantity,
        SUM(ISNULL(d.Amount, d.Quantity * d.UnitPrice)) AS TotalAmount
    FROM dbo.InvoiceDetails AS d
    INNER JOIN dbo.Invoices AS i ON i.Id = d.InvoiceId
    WHERE i.BillingMonth = @Month AND i.BillingYear = @Year
    GROUP BY d.ServiceId, d.Description
    ORDER BY ServiceName;
END;
GO
