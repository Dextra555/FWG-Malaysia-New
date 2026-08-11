export class EmployeeVisaPassportListView {
  EMP_ID: number;
  EMP_CODE: string;
  EMP_NAME: string;
  EMP_BRANCH_CODE: string;
  EMP_PASSPORT_NO: string;
  EMP_IC_NEW: string;
  VisaExpiryDate: string | null;
  PassportExpiryDate: string | null;
  EMPPAY_DATE_JOINED: string | null;
  EMPPAY_CATEGORY: string;
  DaysToVisaExpiry: number | null;
  DaysToPassportExpiry: number | null;
  VisaStatus: string;
  PassportStatus: string;

  constructor(data: Partial<EmployeeVisaPassportListView> = {}) {
    this.EMP_ID             = data.EMP_ID             ?? 0;
    this.EMP_CODE           = data.EMP_CODE           ?? '';
    this.EMP_NAME           = data.EMP_NAME           ?? '';
    this.EMP_BRANCH_CODE    = data.EMP_BRANCH_CODE    ?? '';
    this.EMP_PASSPORT_NO    = data.EMP_PASSPORT_NO    ?? '';
    this.EMP_IC_NEW         = data.EMP_IC_NEW         ?? '';
    this.VisaExpiryDate     = data.VisaExpiryDate     ?? null;
    this.PassportExpiryDate = data.PassportExpiryDate ?? null;
    this.EMPPAY_DATE_JOINED = data.EMPPAY_DATE_JOINED ?? null;
    this.EMPPAY_CATEGORY    = data.EMPPAY_CATEGORY    ?? '';
    this.DaysToVisaExpiry   = data.DaysToVisaExpiry   ?? null;
    this.DaysToPassportExpiry = data.DaysToPassportExpiry ?? null;
    this.VisaStatus         = data.VisaStatus         ?? '';
    this.PassportStatus     = data.PassportStatus     ?? '';
  }
}
