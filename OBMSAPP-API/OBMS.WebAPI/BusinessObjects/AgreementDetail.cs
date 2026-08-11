using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public class AgreementDetail
    {
        protected decimal dID;
        protected decimal dAgreementID;
        protected DateTime dtAgreementDate;
        protected string sClient;
        protected string sBranch;
        protected string sDescription;
        protected int iNoOfGuards;
        protected decimal dRate;
        protected decimal dNoOfHours;
        protected decimal dNoOfDays;
        protected bool sFollowCalendar;
        protected decimal dMonthTotal;
        protected bool sHasDiscount;
        protected decimal dDiscountAmount;
        protected int iDiscountHour;
        protected bool sIsTaxable;
        protected decimal dTaxAmount;
        protected DateTime dtLASTUPDATE;
        protected string sCategory;
        protected string sReason;

        public decimal ID
        {
            get
            {
                return dID;
            }
            set
            {
                dID = value;
            }
        }

        public decimal AgreementID
        {
            get
            {
                return dAgreementID;
            }
            set
            {
                dAgreementID = value;
            }
        }

        public DateTime AgreementDate
        {
            get
            {
                return dtAgreementDate;
            }
            set
            {
                dtAgreementDate = value;
            }
        }

        public string Client
        {
            get
            {
                return sClient;
            }
            set
            {
                sClient = value;
            }
        }

        public string Branch
        {
            get
            {
                return sBranch;
            }
            set
            {
                sBranch = value;
            }
        }

        public string Description
        {
            get
            {
                return sDescription;
            }
            set
            {
                sDescription = value;
            }
        }

        public int NoOfGuards
        {
            get
            {
                return iNoOfGuards;
            }
            set
            {
                iNoOfGuards = value;
            }
        }

        public decimal Rate
        {
            get
            {
                return dRate;
            }
            set
            {
                dRate = value;
            }
        }

        public decimal NoOfHours
        {
            get
            {
                return dNoOfHours;
            }
            set
            {
                dNoOfHours = value;
            }
        }

        public decimal NoOfDays
        {
            get
            {
                return dNoOfDays;
            }
            set
            {
                dNoOfDays = value;
            }
        }

        public bool FollowCalendar
        {
            get
            {
                return sFollowCalendar;
            }
            set
            {
                sFollowCalendar = value;
            }
        }

        public decimal MonthTotal
        {
            get
            {
                return dMonthTotal;
            }
            set
            {
                dMonthTotal = value;
            }
        }

        public bool HasDiscount
        {
            get
            {
                return sHasDiscount;
            }
            set
            {
                sHasDiscount = value;
            }
        }

        public decimal DiscountAmount
        {
            get
            {
                return dDiscountAmount;
            }
            set
            {
                dDiscountAmount = value;
            }
        }

        public int DiscountHour
        {
            get
            {
                return iDiscountHour;
            }
            set
            {
                iDiscountHour = value;
            }
        }

        public bool IsTaxable
        {
            get
            {
                return sIsTaxable;
            }
            set
            {
                sIsTaxable = value;
            }
        }

        public decimal TaxAmount
        {
            get
            {
                return dTaxAmount;
            }
            set
            {
                dTaxAmount = value;
            }
        }

        public DateTime LASTUPDATE
        {
            get
            {
                return dtLASTUPDATE;
            }
            set
            {
                dtLASTUPDATE = value;
            }
        }

        public string Category
        {
            get
            {
                return sCategory;
            }
            set
            {
                sCategory = value;
            }
        }

        public string Reason
        {
            get
            {
                return sReason;
            }
            set
            {
                sReason = value;
            }
        }

        public AgreementDetail()
        {
        }

        public AgreementDetail(decimal ID, decimal AgreementID, DateTime AgreementDate, string Client, string Branch, string Description, int NoOfGuards, decimal Rate, decimal NoOfHours, decimal NoOfDays, bool FollowCalender, decimal MonthTotal, bool HasDiscount, decimal DiscountAmount, int DiscountHour, bool IsTaxable, decimal TaxAmount, DateTime LASTUPDATE, string Category, string Reason)
        {
            dID = ID;
            dAgreementID = AgreementID;
            dtAgreementDate = AgreementDate;
            sClient = Client;
            sBranch = Branch;
            sDescription = Description;
            iNoOfGuards = NoOfGuards;
            dRate = Rate;
            dNoOfHours = NoOfHours;
            dNoOfDays = NoOfDays;
            sFollowCalendar = FollowCalender;
            dMonthTotal = MonthTotal;
            sHasDiscount = HasDiscount;
            dDiscountAmount = DiscountAmount;
            iDiscountHour = DiscountHour;
            sIsTaxable = IsTaxable;
            dTaxAmount = TaxAmount;
            dtLASTUPDATE = LASTUPDATE;
            sCategory = Category;
            sReason = Reason;

        }
        public static List<AgreementDetail> GetList(decimal AgreementID)
        {
            List<AgreementDetailFactory> oAgreementDetailFactoryList = AgreementDetailFactory.GetList(AgreementID);
            List<AgreementDetail> oAgreementDetailList = new List<AgreementDetail>();
            for (int i = 0; i < oAgreementDetailFactoryList.Count; i++)
            {
                AgreementDetailFactory oAgreementDetailFactory = oAgreementDetailFactoryList[i];
                oAgreementDetailList.Add(new AgreementDetail(
                                    oAgreementDetailFactory.ID,
                                    oAgreementDetailFactory.AgreementID,
                                    oAgreementDetailFactory.AgreementDate,
                                    oAgreementDetailFactory.Client,
                                    oAgreementDetailFactory.Branch,
                                    oAgreementDetailFactory.Description,
                                    oAgreementDetailFactory.NoOfGuards,
                                    oAgreementDetailFactory.Rate,
                                    oAgreementDetailFactory.NoOfHours,
                                    oAgreementDetailFactory.NoOfDays,
                                    oAgreementDetailFactory.FollowCalender,
                                    oAgreementDetailFactory.MonthTotal,
                                    oAgreementDetailFactory.HasDiscount,
                                    oAgreementDetailFactory.DiscountAmount,
                                    oAgreementDetailFactory.DiscountHour,
                                    oAgreementDetailFactory.IsTaxable,
                                    oAgreementDetailFactory.TaxAmount,
                                    oAgreementDetailFactory.LASTUPDATE,
                                    oAgreementDetailFactory.Category,
                                    oAgreementDetailFactory.Reason));
            }
            return oAgreementDetailList;
        }
        public void Get(decimal ID)
        {
            AgreementDetailFactory oAgreementDetailFactory = new AgreementDetailFactory();
            oAgreementDetailFactory.Get(ID);
            dID = oAgreementDetailFactory.ID;
            dAgreementID = oAgreementDetailFactory.AgreementID;
            dtAgreementDate = oAgreementDetailFactory.AgreementDate;
            sClient = oAgreementDetailFactory.Client;
            sBranch = oAgreementDetailFactory.Branch;
            sDescription = oAgreementDetailFactory.Description;
            iNoOfGuards = oAgreementDetailFactory.NoOfGuards;
            dRate = oAgreementDetailFactory.Rate;
            dNoOfHours = oAgreementDetailFactory.NoOfHours;
            dNoOfDays = oAgreementDetailFactory.NoOfDays;
            sFollowCalendar = oAgreementDetailFactory.FollowCalender;
            sHasDiscount = oAgreementDetailFactory.HasDiscount;
            dDiscountAmount = oAgreementDetailFactory.DiscountAmount;
            iDiscountHour = oAgreementDetailFactory.DiscountHour;
            sIsTaxable = oAgreementDetailFactory.IsTaxable;
            dTaxAmount = oAgreementDetailFactory.TaxAmount;
            dtLASTUPDATE = oAgreementDetailFactory.LASTUPDATE;
            sCategory = oAgreementDetailFactory.Category;
            sReason = oAgreementDetailFactory.Reason;


        }

        public bool Add(SQLDataAccess sdaFactory, SqlTransaction Transaction, string CurrentUser)
        {
            AgreementDetailFactory oAgreementDetailFactory = new AgreementDetailFactory();
            oAgreementDetailFactory.ID = dID;
            oAgreementDetailFactory.AgreementID = dAgreementID;
            oAgreementDetailFactory.AgreementDate = dtAgreementDate;
            oAgreementDetailFactory.Client = sClient;
            oAgreementDetailFactory.Branch = sBranch;
            oAgreementDetailFactory.Description = sDescription;
            oAgreementDetailFactory.NoOfGuards = iNoOfGuards;
            oAgreementDetailFactory.Rate = dRate;
            oAgreementDetailFactory.NoOfHours = dNoOfHours;
            oAgreementDetailFactory.NoOfDays = dNoOfDays;
            oAgreementDetailFactory.FollowCalender = sFollowCalendar;
            oAgreementDetailFactory.MonthTotal = dMonthTotal;
            oAgreementDetailFactory.HasDiscount = sHasDiscount;
            oAgreementDetailFactory.DiscountAmount = dDiscountAmount;
            oAgreementDetailFactory.DiscountHour = iDiscountHour;
            oAgreementDetailFactory.IsTaxable = sIsTaxable;
            oAgreementDetailFactory.TaxAmount = dTaxAmount;
            oAgreementDetailFactory.LASTUPDATE = dtLASTUPDATE;
            oAgreementDetailFactory.Category = sCategory;
            oAgreementDetailFactory.Reason = sReason;
            bool bSuccess = oAgreementDetailFactory.Add(sdaFactory, Transaction, CurrentUser);
            return bSuccess;
        }

        public bool Update(SQLDataAccess sdaFactory, SqlTransaction Transaction, string CurrentUser)
        {
            AgreementDetailFactory oAgreementDetailFactory = new AgreementDetailFactory();
            oAgreementDetailFactory.Get(dID);
            oAgreementDetailFactory.ID = dID;
            oAgreementDetailFactory.AgreementID = dAgreementID;
            oAgreementDetailFactory.AgreementDate = dtAgreementDate;
            oAgreementDetailFactory.Client = sClient;
            oAgreementDetailFactory.Branch = sBranch;
            oAgreementDetailFactory.Description = sDescription;
            oAgreementDetailFactory.NoOfGuards = iNoOfGuards;
            oAgreementDetailFactory.Rate = dRate;
            oAgreementDetailFactory.NoOfHours = dNoOfHours;
            oAgreementDetailFactory.NoOfDays = dNoOfDays;
            oAgreementDetailFactory.FollowCalender = sFollowCalendar;
            oAgreementDetailFactory.MonthTotal = dMonthTotal;
            oAgreementDetailFactory.HasDiscount = sHasDiscount;
            oAgreementDetailFactory.DiscountAmount = dDiscountAmount;
            oAgreementDetailFactory.DiscountHour = iDiscountHour;
            oAgreementDetailFactory.IsTaxable = sIsTaxable;
            oAgreementDetailFactory.TaxAmount = dTaxAmount;
            oAgreementDetailFactory.LASTUPDATE = dtLASTUPDATE;
            oAgreementDetailFactory.Category = sCategory;
            oAgreementDetailFactory.Reason = sReason;
            bool bSuccess = oAgreementDetailFactory.Save(sdaFactory, Transaction, CurrentUser);
            return bSuccess;
        }

        public bool Delete(SQLDataAccess sdaFactory, SqlTransaction Transaction)
        {
            AgreementDetailFactory oAgreementDetailFactory = new AgreementDetailFactory();
            oAgreementDetailFactory.Get(dID);
            bool bSuccess = oAgreementDetailFactory.Delete(sdaFactory, Transaction);
            return bSuccess;
        }
    }
}