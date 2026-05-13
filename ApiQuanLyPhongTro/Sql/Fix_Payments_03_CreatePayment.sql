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
