using OBMS.WebAPI.Utility;
using System.Globalization;

namespace OBMS.WebAPI.BusinessObjects
{
    /// <summary>
    /// Calculates client invoice amounts for a specific AgreementID.
    /// Used when a client has multiple agreements in the same month (multiple sites/WorkPlace).
    /// </summary>
    public class ClientInvoiceCalculationByAgreementId
    {
        private static readonly IConfiguration configuration;

        private decimal dServiceCharges = 0;
        private decimal dDiscount = 0;
        private decimal dTaxAmount = 0;
        private decimal dNoOfDays = 0;
        private decimal dNoOfHours = 0;

        public decimal ServiceCharges { get { return dServiceCharges; } }
        public decimal Discount { get { return dDiscount; } }
        public decimal TaxAmount { get { return dTaxAmount; } }
        public decimal NoOfDays { get { return dNoOfDays; } }
        public decimal NoOfHours { get { return dNoOfHours; } }
        public decimal Total { get { return (dServiceCharges - dDiscount + dTaxAmount); } }

        static ClientInvoiceCalculationByAgreementId()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public ClientInvoiceCalculationByAgreementId(int agreementId, DateTime agreementPeriod)
        {
            string GSTStart6 = Constants.GSTStart6;
            string GSTEnd6   = Constants.GSTEnd6;
            string GSTStart0 = Constants.GSTStart0;
            string GSTEnd0   = Constants.GSTEnd0;
            string SSTStart6 = Constants.SSTStart6;
            string SSTEnd6   = Constants.SSTEnd6;
            string SSTStart8 = Constants.SSTStart8;

            // Fetch the agreement directly by ID
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.Get((decimal)agreementId);

            if (oAgreementFactory.ID == 0)
                return; // Agreement not found

            // Validate: if IsValid=true, date must match month
            if (oAgreementFactory.IsValid &&
                oAgreementFactory.AgreementDate.ToString("yyyyMM") != agreementPeriod.ToString("yyyyMM"))
                return;

            List<AgreementDetail> oAgreementDetails = AgreementDetail.GetList(oAgreementFactory.ID);

            string dateFormat = "MM/dd/yyyy";
            CultureInfo culture = CultureInfo.InvariantCulture;

            DateTime gstStart  = DateTime.ParseExact(GSTStart6, dateFormat, culture).Date;
            DateTime gstEnd    = DateTime.ParseExact(GSTEnd6,   dateFormat, culture).Date;
            DateTime sstStart  = DateTime.ParseExact(SSTStart6, dateFormat, culture).Date;
            DateTime sstEnd    = DateTime.ParseExact(SSTEnd6,   dateFormat, culture).Date;
            DateTime gstStart0 = DateTime.ParseExact(GSTStart0, dateFormat, culture).Date;
            DateTime gstEnd0   = DateTime.ParseExact(GSTEnd0,   dateFormat, culture).Date;
            DateTime sstStart8 = DateTime.ParseExact(SSTStart8, dateFormat, culture).Date;
            DateTime agreementDate = agreementPeriod.Date;

            for (int i = 0; i < oAgreementDetails.Count; i++)
            {
                AgreementDetail oAgreementDetail = oAgreementDetails[i];

                if (oAgreementDetail.FollowCalendar)
                {
                    oAgreementDetail.NoOfDays = DateTime.DaysInMonth(agreementPeriod.Year, agreementPeriod.Month);
                }
                else
                {
                    if (oAgreementFactory.AgreementDate.Month == agreementPeriod.Month &&
                        oAgreementFactory.AgreementDate.Year  == agreementPeriod.Year)
                    {
                        if (!((agreementPeriod.Day == DateTime.DaysInMonth(oAgreementFactory.AgreementDate.Year, oAgreementFactory.AgreementDate.Month)) &&
                              oAgreementFactory.AgreementDate.Day == 1))
                        {
                            if (oAgreementDetail.NoOfDays > (agreementPeriod.Day - oAgreementFactory.AgreementDate.Day + 1))
                                oAgreementDetail.NoOfDays = oAgreementDetail.NoOfDays;
                            else
                                oAgreementDetail.NoOfDays = (agreementPeriod.Day - oAgreementFactory.AgreementDate.Day + 1);
                        }
                    }
                }

                if (oAgreementDetail.NoOfGuards != 0 && oAgreementDetail.Rate != 0 &&
                    oAgreementDetail.NoOfHours  != 0 && oAgreementDetail.NoOfDays != 0)
                {
                    dServiceCharges += oAgreementDetail.NoOfDays * oAgreementDetail.NoOfGuards *
                                       oAgreementDetail.NoOfHours * oAgreementDetail.Rate;
                }
                else
                {
                    dServiceCharges += oAgreementDetail.MonthTotal;
                }

                    // For HOUR-type entries, NoOfGuards and NoOfDays are 0 — use NoOfHours directly
                    if (oAgreementDetail.NoOfDays != 0)
                        dNoOfHours += oAgreementDetail.NoOfHours * oAgreementDetail.NoOfGuards * oAgreementDetail.NoOfDays;
                    else if (oAgreementDetail.NoOfHours != 0)
                        dNoOfHours += oAgreementDetail.NoOfHours; // HOUR-type: working hours stored directly

                if (oAgreementDetail.HasDiscount)
                    dDiscount += oAgreementDetail.DiscountAmount;

                if (oAgreementDetail.IsTaxable)
                {
                    decimal pa;
                    if (agreementDate.Year <= 2010)
                        pa = 0.05M;
                    else if ((agreementDate >= gstStart && agreementDate <= gstEnd) ||
                             (agreementDate >= sstStart && agreementDate <= sstEnd))
                        pa = 0.08M;
                    else if (agreementDate >= gstStart0 && agreementDate <= gstEnd0)
                        pa = 0.00M;
                    else
                        pa = 0.08M;

                    dTaxAmount += (oAgreementDetail.MonthTotal - oAgreementDetail.DiscountAmount) * pa;
                }
            }
        }
    }
}
