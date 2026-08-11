
using OBMS.WebAPI.Utility;
using System.Globalization;

namespace OBMS.WebAPI.BusinessObjects
{
    public class ClientInvoiceCalculation
    {

        private decimal dServiceCharges = 0;
        private decimal dDiscount = 0;
        private decimal dTaxAmount = 0;
        private decimal dNoOfDays = 0;
        private decimal dNoOfHours = 0;
        public decimal ServiceCharges
        {
            get { return dServiceCharges; }
        }
        public decimal Discount
        {
            get { return dDiscount; }
        }
        public decimal TaxAmount
        {
            get { return dTaxAmount; }
        }
        public decimal NoOfDays
        {
            get { return dNoOfDays; }
        }
        public decimal NoOfHours
        {
            get { return dNoOfHours; }
        }
        public decimal Total
        {
            get { return (dServiceCharges - dDiscount + dTaxAmount); }
        }
        public ClientInvoiceCalculation(string Branch, string Client, DateTime AgreementPeriod)
        {
            string GSTStart6 = Constants.GSTStart6;
            string GSTEnd6 = Constants.GSTEnd6;
            string GSTStart0 = Constants.GSTStart0;
            string GSTEnd0 = Constants.GSTEnd0;
            string SSTStart6 = Constants.SSTStart6;
            string SSTEnd6 = Constants.SSTEnd6;
            string SSTStart8 = Constants.SSTStart8;
            string SSTEnd8 = Constants.SSTEnd8;

            Agreement oAgreement = new Agreement();
            oAgreement.Get(Branch, Client, AgreementPeriod);
            if (oAgreement.IsValid && (oAgreement.AgreementDate.ToString("yyyyMM") != AgreementPeriod.ToString("yyyyMM")))
                return;
            if (oAgreement.ID != 0)
            {
                List<AgreementDetail> oAgreementDetails = oAgreement.AgreementDetails;
                for (int i = 0; i < oAgreementDetails.Count; i++)
                {
                    AgreementDetail oAgreementDetail = oAgreementDetails[i];
                    if (oAgreementDetail.FollowCalendar)
                    {
                        oAgreementDetail.NoOfDays = DateTime.DaysInMonth(AgreementPeriod.Year, AgreementPeriod.Month);
                    }
                    else
                    {
                        if ((oAgreement.AgreementDate.Month == AgreementPeriod.Month) && (oAgreement.AgreementDate.Year == AgreementPeriod.Year))
                        {
                            if (!((AgreementPeriod.Day == DateTime.DaysInMonth(oAgreement.AgreementDate.Year, oAgreement.AgreementDate.Month)) && (oAgreement.AgreementDate.Day == 1)))
                            {
                                if ((oAgreementDetail.NoOfDays > (AgreementPeriod.Day - oAgreement.AgreementDate.Day + 1)))
                                {
                                    oAgreementDetail.NoOfDays = oAgreementDetail.NoOfDays;
                                }
                                else
                                {
                                    oAgreementDetail.NoOfDays = (AgreementPeriod.Day - oAgreement.AgreementDate.Day + 1);
                                }
                            }
                        }
                    }

                    //if (oAgreementDetail.NoOfGuards == 0 && oAgreementDetail.Rate == 0 && oAgreementDetail.NoOfHours == 0 && oAgreementDetail.NoOfDays == 0)
                    //{
                    //    dServiceCharges += oAgreementDetail.MonthTotal;
                    //}
                    //else
                    //{
                    //    if (oAgreementDetail.NoOfGuards == 0 && oAgreementDetail.Rate != 0 && oAgreementDetail.NoOfHours == 0 && oAgreementDetail.NoOfDays != 0)
                    //        dServiceCharges += oAgreementDetail.Rate * oAgreementDetail.NoOfDays;
                    //    else
                    //        dServiceCharges += oAgreementDetail.NoOfGuards * oAgreementDetail.Rate * oAgreementDetail.NoOfHours * oAgreementDetail.NoOfDays;
                    //}

                    if (oAgreementDetail.NoOfGuards != 0 && oAgreementDetail.Rate != 0 && oAgreementDetail.NoOfHours != 0 && oAgreementDetail.NoOfDays != 0)
                    {
                        dServiceCharges += oAgreementDetail.NoOfDays * oAgreementDetail.NoOfGuards * oAgreementDetail.NoOfHours * oAgreementDetail.Rate;//oAgreementDetail.MonthTotal;
                    }
                    else
                    {
                        dServiceCharges += oAgreementDetail.MonthTotal;
                    }

                    // For HOUR-type entries, NoOfGuards and NoOfDays are 0 — use NoOfHours directly from MonthTotal/Rate
                    if (oAgreementDetail.NoOfDays != 0)
                        dNoOfHours += oAgreementDetail.NoOfHours * oAgreementDetail.NoOfGuards * oAgreementDetail.NoOfDays;
                    else if (oAgreementDetail.NoOfHours != 0)
                        dNoOfHours += oAgreementDetail.NoOfHours; // HOUR-type: working hours stored directly
                    if (oAgreementDetail.HasDiscount)
                    {
                        dDiscount += oAgreementDetail.DiscountAmount;
                    }

                    if (oAgreementDetail.IsTaxable)
                    {
                        //if (oAgreementDetail.NoOfGuards == 0 && oAgreementDetail.Rate == 0 && oAgreementDetail.NoOfHours == 0 && oAgreementDetail.NoOfDays == 0)
                        //{
                        //    dTaxAmount += (oAgreementDetail.MonthTotal - oAgreementDetail.DiscountAmount) * (decimal)0.05;
                        //}
                        //else
                        //{
                        //    if (oAgreementDetail.NoOfGuards == 0 && oAgreementDetail.Rate != 0 && oAgreementDetail.NoOfHours == 0 && oAgreementDetail.NoOfDays != 0)
                        //    {
                        //        dTaxAmount += ((oAgreementDetail.Rate * oAgreementDetail.NoOfDays) - oAgreementDetail.DiscountAmount) * (decimal)0.05;
                        //    }
                        //    else
                        //        dTaxAmount += ((oAgreementDetail.NoOfGuards * oAgreementDetail.Rate * oAgreementDetail.NoOfHours * oAgreementDetail.NoOfDays) - oAgreementDetail.DiscountAmount) * (decimal)0.05;
                        //}

                        // Correct formats
                        string dateFormat = "MM/dd/yyyy"; // Expected date format for constants
                        //string agreementDateFormat = "dd/M/yyyy hh:mm:ss tt";// Format of AgreementPeriod input
                        CultureInfo culture = CultureInfo.InvariantCulture; // Use invariant culture

                        // Parse AgreementPeriod with its actual format
                        //DateTime agreementDate = DateTime.ParseExact(AgreementPeriod.ToString(), agreementDateFormat, culture).Date;                       
                        DateTime agreementDate = AgreementPeriod.Date;

                        // Parse constants with the correct format
                        DateTime gstStart = DateTime.ParseExact(GSTStart6, dateFormat, culture).Date;
                        DateTime gstEnd = DateTime.ParseExact(GSTEnd6, dateFormat, culture).Date;
                        DateTime sstStart = DateTime.ParseExact(SSTStart6, dateFormat, culture).Date;
                        DateTime sstEnd = DateTime.ParseExact(SSTEnd6, dateFormat, culture).Date;
                        DateTime gstStart0 = DateTime.ParseExact(GSTStart0, dateFormat, culture).Date;
                        DateTime gstEnd0 = DateTime.ParseExact(GSTEnd0, dateFormat, culture).Date;
                        DateTime sstStart8 = DateTime.ParseExact(SSTStart8, dateFormat, culture).Date;
                        DateTime sstEnd8 = DateTime.ParseExact("12/31/9999", dateFormat, culture).Date; // Corrected SSTEnd8

                        decimal pa;

                        // Logic to calculate `pa`
                        if (agreementDate.Year <= 2010)
                        {
                            pa = 0.05M;
                        }
                        else if ((agreementDate >= gstStart && agreementDate <= gstEnd) ||
                                 (agreementDate >= sstStart && agreementDate <= sstEnd))
                        {
                            pa = 0.08M; // Change for New Service Tax
                        }
                        else if (agreementDate >= gstStart0 && agreementDate <= gstEnd0) // GST 0%
                        {
                            pa = 0.00M;
                        }
                        else if (agreementDate >= sstStart8 && agreementDate <= sstEnd8) // SST 8%
                        {
                            pa = 0.08M;
                        }
                        else
                        {
                            pa = 0.06M; // Default service tax
                        }


                        //if (oAgreementDetail.NoOfGuards != 0 && oAgreementDetail.Rate != 0 && oAgreementDetail.NoOfHours != 0 && oAgreementDetail.NoOfDays != 0)
                        //{
                        //    dTaxAmount = (dServiceCharges - dDiscount) * pa;
                        //}
                        //else
                        //{
                        //    dTaxAmount = (dServiceCharges - dDiscount) * pa;
                        //}
                        dTaxAmount += (oAgreementDetail.MonthTotal - oAgreementDetail.DiscountAmount) * pa;
                    }

                }

            }
        }

    }
}