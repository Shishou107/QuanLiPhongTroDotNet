/* Run only this script if sp_Rooms_GetById failed near ContractId or IN. */

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
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Contracts
                WHERE RoomId = r.Id AND ISNULL(Status, 0) = 1
            )
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
