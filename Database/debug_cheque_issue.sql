-- Debug script to find why cheque numbers aren't showing for CIMB

-- 1. Check ChequeMaster table structure and data
SELECT 'ChequeMaster Data' AS Section, 
       CM.ID, CM.BankID, CM.ChequeStart, CM.ChequeEnd, CM.IsActive,
       BM.Accname AS BankAccountName, BM.BankCode
FROM ChequeMaster CM
LEFT JOIN BankMaster BM ON CM.BankID = BM.BankId
ORDER BY CM.BankID;

-- 2. Check what BankID is used for CIMB
SELECT 'CIMB Bank Details' AS Section,
       BankId, BankCode, Accname, Accno, AccShortName
FROM BankMaster
WHERE Accname LIKE '%CIMB%' OR BankCode LIKE '%CIMB%';

-- 3. Simulate the GetNextChequeNumber query for BankID = 1
DECLARE @Bank INT = 1;

SELECT 'Simulated Query Result for BankID 1' AS Section,
CASE 
    WHEN ISNULL(MaxCheque.ChequeNo,0) >= ChequeBook.ChequeStart AND MaxCheque.ChequeNo<ChequeBook.ChequeEnd THEN MaxCheque.ChequeNo+1 
    WHEN ISNULL(MaxCheque.ChequeNo,0) < ChequeBook.ChequeStart THEN ChequeBook.ChequeStart 
    WHEN ISNULL(MaxCheque.ChequeNo,0) = ChequeBook.ChequeEnd THEN NULL 
    ELSE NULL 
END AS NextChequeNumber,
MaxCheque.ChequeNo AS LastUsedCheque,
ChequeBook.ChequeStart,
ChequeBook.ChequeEnd,
ChequeBook.BankID
FROM 
(SELECT BranchPayments.BankID,Max(Convert(numeric(18,0),BranchPayments.ChequeNo))as ChequeNo 
 FROM BranchPayments 
 INNER JOIN ChequeMaster ON ChequeMaster.BankID=BranchPayments.BankID AND IsActive=1 and isnumeric(Chequeno)=1 
 WHERE BranchPayments.BankID=@Bank AND ChequeNo BETWEEN ChequeMaster.ChequeStart AND ChequeMaster.ChequeEnd 
 GROUP BY BranchPayments.BankID) MaxCheque 
RIGHT OUTER JOIN 
(SELECT BankID,ChequeStart,ChequeEnd FROM ChequeMaster WHERE BankID=@Bank AND IsActive=1) ChequeBook 
ON MaxCheque.BankID = ChequeBook.BankID;

-- 4. Check recent payments for this bank
SELECT TOP 5 'Recent Payments for BankID 1' AS Section,
       ID, PaymentDate, ChequeNo, BankID, PaymentTo, Amount
FROM BranchPayments
WHERE BankID = 1 AND IsDeleted = 0
ORDER BY PaymentDate DESC, ID DESC;
