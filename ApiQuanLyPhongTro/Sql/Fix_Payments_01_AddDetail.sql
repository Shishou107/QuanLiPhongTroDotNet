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

    IF ISNULL(@EffectiveUnitPrice, 0) <= 0
    BEGIN
        RAISERROR(N'Đơn giá dịch vụ phải lớn hơn 0', 16, 1);
        RETURN;
    END;

    INSERT INTO dbo.InvoiceDetails (Id, InvoiceId, ServiceId, Description, Quantity, UnitPrice, CreatedAt)
    VALUES (NEWID(), @InvoiceId, @ServiceId, @Description, @Quantity, @EffectiveUnitPrice, SYSDATETIME());
END;
