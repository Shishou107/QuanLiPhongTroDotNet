USE [Data_Base_Phontro];

/* Somee-safe script:
   - Khong phu thuoc vao GO.
   - Tao/sua procedure va trigger bang dynamic SQL de tranh loi "Incorrect syntax near BEGIN".
*/

UPDATE d
SET UnitPrice = s.UnitPrice
FROM dbo.InvoiceDetails AS d
INNER JOIN dbo.Services AS s ON s.Id = d.ServiceId
WHERE ISNULL(d.UnitPrice, 0) <= 0
  AND ISNULL(s.UnitPrice, 0) > 0;

EXEC(N'
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
        RAISERROR(N''Dịch vụ không tồn tại'', 16, 1);
        RETURN;
    END;

    IF @Quantity <= 0
    BEGIN
        RAISERROR(N''Số lượng phải lớn hơn 0'', 16, 1);
        RETURN;
    END;

    IF @UnitPrice > 0
        SET @EffectiveUnitPrice = @UnitPrice;

    IF ISNULL(@EffectiveUnitPrice, 0) <= 0
    BEGIN
        RAISERROR(N''Đơn giá dịch vụ phải lớn hơn 0'', 16, 1);
        RETURN;
    END;

    INSERT INTO dbo.InvoiceDetails (Id, InvoiceId, ServiceId, Description, Quantity, UnitPrice, CreatedAt)
    VALUES (NEWID(), @InvoiceId, @ServiceId, @Description, @Quantity, @EffectiveUnitPrice, SYSDATETIME());
END
');

EXEC(N'
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
END
');

EXEC(N'
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
        RAISERROR(N''Hóa đơn không tồn tại'', 16, 1);
        RETURN;
    END;

    IF @Amount <= 0
    BEGIN
        RAISERROR(N''Số tiền thanh toán phải lớn hơn 0'', 16, 1);
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
        RAISERROR(N''Hóa đơn chưa có tổng tiền, vui lòng cập nhật chi tiết hóa đơn trước khi thanh toán'', 16, 1);
        RETURN;
    END;

    IF @DebtAmount <= 0
    BEGIN
        RAISERROR(N''Hóa đơn đã thanh toán đủ'', 16, 1);
        RETURN;
    END;

    IF @Amount > @DebtAmount
    BEGIN
        RAISERROR(N''Số tiền thanh toán vượt quá số tiền còn nợ'', 16, 1);
        RETURN;
    END;

    SET @MethodCode = TRY_CONVERT(TINYINT, @Method);
    IF @MethodCode NOT IN (1, 2, 3)
        SET @MethodCode = 1;

    SET @Id = NEWID();

    INSERT INTO dbo.Payments (Id, InvoiceId, Amount, PaymentDate, PaymentMethod, Note, CreatedAt)
    VALUES (@Id, @InvoiceId, @Amount, ISNULL(@PaymentDate, GETDATE()), @MethodCode, @Note, SYSDATETIME());

    EXEC dbo.sp_Invoices_Recalculate @InvoiceId;
END
');

IF OBJECT_ID(N'dbo.trg_Payments_UpdateInvoiceSummary', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Payments_UpdateInvoiceSummary;

EXEC(N'
CREATE TRIGGER dbo.trg_Payments_UpdateInvoiceSummary
ON dbo.Payments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted WHERE Amount <= 0)
    BEGIN
        RAISERROR(N''Số tiền thanh toán phải lớn hơn 0'', 16, 1);
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
        RAISERROR(N''Hóa đơn chưa có tổng tiền, vui lòng cập nhật chi tiết hóa đơn trước khi thanh toán'', 16, 1);
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
        RAISERROR(N''Số tiền thanh toán vượt quá số tiền còn nợ'', 16, 1);
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
END
');

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
OUTER APPLY (SELECT SUM(Amount) AS PaidAmount FROM dbo.Payments WHERE InvoiceId = i.Id) AS p;

UPDATE dbo.Invoices SET Status = 1, UpdatedAt = SYSDATETIME() WHERE PaidAmount > 0 AND TotalAmount > PaidAmount;
UPDATE dbo.Invoices SET Status = 3, UpdatedAt = SYSDATETIME() WHERE DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) AND TotalAmount > PaidAmount;
UPDATE dbo.Invoices SET Status = 2, UpdatedAt = SYSDATETIME() WHERE PaidAmount >= TotalAmount AND TotalAmount > 0;

SELECT
    i.Id,
    UPPER(LEFT(CONVERT(VARCHAR(36), i.Id), 8)) AS InvoiceCode,
    i.TotalAmount,
    ISNULL(i.PaidAmount, 0) AS PaidAmount,
    ISNULL(p.PaidFromHistory, 0) AS PaidFromHistory
FROM dbo.Invoices AS i
OUTER APPLY (SELECT SUM(Amount) AS PaidFromHistory FROM dbo.Payments WHERE InvoiceId = i.Id) AS p
WHERE ISNULL(i.TotalAmount, 0) <= 0
   OR ISNULL(p.PaidFromHistory, 0) > ISNULL(i.TotalAmount, 0);
