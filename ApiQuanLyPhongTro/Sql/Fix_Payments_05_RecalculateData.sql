UPDATE d
SET UnitPrice = s.UnitPrice
FROM dbo.InvoiceDetails AS d
INNER JOIN dbo.Services AS s ON s.Id = d.ServiceId
WHERE ISNULL(d.UnitPrice, 0) <= 0
  AND ISNULL(s.UnitPrice, 0) > 0;

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
