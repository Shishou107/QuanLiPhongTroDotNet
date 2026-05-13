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
