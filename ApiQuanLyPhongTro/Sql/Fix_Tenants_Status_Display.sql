USE [Data_Base_Phontro];
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
