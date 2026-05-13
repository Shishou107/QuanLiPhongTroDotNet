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
