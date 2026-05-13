CREATE OR ALTER PROCEDURE dbo.sp_Rooms_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.RoomNumber,
        r.BuildingId,
        (SELECT b.Name FROM dbo.Buildings AS b WHERE b.Id = r.BuildingId) AS BuildingName,
        r.Area,
        r.BasePrice AS RentPrice,
        r.BasePrice AS BasePrice,
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Contracts AS c
                WHERE c.RoomId = r.Id AND ISNULL(c.Status, 0) = 1
            )
            THEN 1
            ELSE CONVERT(integer, ISNULL(r.Status, 0))
        END AS Status,
        r.Description
    FROM dbo.Rooms AS r
    WHERE r.Id = @Id;

    SELECT TOP (1)
        c.Id,
        UPPER(LEFT(CONVERT(varchar(36), c.Id), 8)) AS ContractCode,
        (SELECT t.FullName FROM dbo.Tenants AS t WHERE t.Id = c.TenantId) AS TenantName,
        c.StartDate,
        c.EndDate,
        CONVERT(integer, ISNULL(c.Status, 0)) AS Status
    FROM dbo.Contracts AS c
    WHERE c.RoomId = @Id AND ISNULL(c.Status, 0) = 1
    ORDER BY c.CreatedAt DESC;
END
GO
