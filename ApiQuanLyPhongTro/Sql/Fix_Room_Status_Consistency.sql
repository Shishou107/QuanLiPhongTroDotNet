/* Run this script in the Somee database after publishing the API.
   It prevents a room with an active contract from being marked as empty. */

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
    OUTER APPLY
    (
        SELECT TOP 1 c.Id, c.TenantId
        FROM dbo.Contracts AS c
        WHERE c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1
        ORDER BY c.CreatedAt DESC
    ) AS activeContract
    WHERE (@BuildingId IS NULL OR r.BuildingId = @BuildingId)
      AND (@Status IS NULL OR CASE WHEN activeContract.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END = @Status)
      AND (@Keyword IS NULL OR r.RoomNumber LIKE N'%' + @Keyword + N'%');

    SELECT
        r.Id,
        r.RoomNumber,
        r.BuildingId,
        b.Name AS BuildingName,
        r.Area,
        r.BasePrice AS RentPrice,
        CASE WHEN activeContract.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END AS Status,
        ISNULL(t.FullName, N'Chưa có') AS CurrentTenantName,
        activeContract.Id AS CurrentContractId
    FROM dbo.Rooms AS r
    INNER JOIN dbo.Buildings AS b ON b.Id = r.BuildingId
    OUTER APPLY
    (
        SELECT TOP 1 c.Id, c.TenantId
        FROM dbo.Contracts AS c
        WHERE c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1
        ORDER BY c.CreatedAt DESC
    ) AS activeContract
    LEFT JOIN dbo.Tenants AS t ON t.Id = activeContract.TenantId
    WHERE (@BuildingId IS NULL OR r.BuildingId = @BuildingId)
      AND (@Status IS NULL OR CASE WHEN activeContract.Id IS NOT NULL THEN 1 ELSE CAST(ISNULL(r.Status, 0) AS INT) END = @Status)
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
        r.Id,
        r.RoomNumber,
        r.BuildingId,
        b.Name AS BuildingName,
        r.Area,
        r.BasePrice AS RentPrice,
        r.BasePrice AS BasePrice,
        CASE WHEN EXISTS (SELECT 1 FROM dbo.Contracts AS c WHERE c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1)
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
    WHERE c.RoomId = @Id AND ISNULL(c.Status, 0) = 1
    ORDER BY c.CreatedAt DESC;

    SELECT TOP 0
        CAST(NULL AS UNIQUEIDENTIFIER) AS Id,
        CAST(NULL AS VARCHAR(8)) AS InvoiceCode,
        CAST(NULL AS INT) AS BillingMonth,
        CAST(NULL AS INT) AS BillingYear,
        CAST(0 AS DECIMAL(18, 2)) AS TotalAmount,
        CAST(0 AS DECIMAL(18, 2)) AS PaidAmount,
        CAST(0 AS INT) AS Status;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Rooms_GetAvailable
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.RoomNumber,
        r.BuildingId,
        b.Name AS BuildingName,
        r.Area,
        r.BasePrice AS RentPrice,
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
