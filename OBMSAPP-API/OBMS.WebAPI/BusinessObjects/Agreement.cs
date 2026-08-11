using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public class Agreement
    {
        private static readonly IConfiguration configuration;
        protected decimal dID;
        protected string sBranch;
        protected string sClient;
        protected string sWorkPlace;
        protected DateTime dtAgreementDate;
        protected string sNote;
        protected bool bIsValid;
        protected DateTime dtLASTUPDATE;
        private List<AgreementDetail> oAgreementDetails;
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

        public string WorkPlace
        {
            get
            {
                return sWorkPlace;
            }
            set
            {
                sWorkPlace = value;
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
        public string Note
        {
            get
            {
                return sNote;
            }
            set
            {
                sNote = value;
            }
        }
        public bool IsValid
        {
            get
            {
                return bIsValid;
            }
            set
            {
                bIsValid = value;
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
        public List<AgreementDetail> AgreementDetails
        {
            get { return oAgreementDetails; }
            set { oAgreementDetails = value; }
        }
        public Agreement()
        {

        }
        static Agreement()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }

        public Agreement(decimal ID, string Branch, string Client, string WorkPlace, DateTime AgreementDate, string Note, bool isvalid, DateTime LASTUPDATE)
        {
            dID = ID;
            sBranch = Branch;
            sClient = Client;
            sWorkPlace = WorkPlace;
            dtAgreementDate = AgreementDate;
            sNote = Note;
            bIsValid = isvalid;
            dtLASTUPDATE = LASTUPDATE;

        }
        public static List<Agreement> GetList(string Branch)
        {
            List<AgreementFactory> oAgreementFactoryList = AgreementFactory.GetList(Branch);
            List<Agreement> oAgreementList = new List<Agreement>();
            for (int i = 0; i < oAgreementList.Count; i++)
            {
                AgreementFactory oAgreementFactory = oAgreementFactoryList[i];
                oAgreementList.Add(new Agreement(
                    oAgreementFactory.ID,
                    oAgreementFactory.Branch,
                    oAgreementFactory.Client,
                    oAgreementFactory.WorkPlace,
                    oAgreementFactory.AgreementDate,
                    oAgreementFactory.Note,
                    oAgreementFactory.IsValid,
                    oAgreementFactory.LASTUPDATE));
            }
            return oAgreementList;
        }
        public void Get(string Branch, string Client, DateTime AgreementPeriod)
        {
            DateTime AgreementPrd = DateTime.Parse(AgreementPeriod.ToShortDateString());
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.Get(Branch, Client, AgreementPrd);
            dID = oAgreementFactory.ID;
            sBranch = oAgreementFactory.Branch;
            sClient = oAgreementFactory.Client;
            sWorkPlace = oAgreementFactory.WorkPlace;
            dtAgreementDate = oAgreementFactory.AgreementDate;
            sNote = oAgreementFactory.Note;
            bIsValid = oAgreementFactory.IsValid;
            dtLASTUPDATE = oAgreementFactory.LASTUPDATE;
            oAgreementDetails = AgreementDetail.GetList(dID);
        }
        public void Get(decimal ID)
        {
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.Get(ID);
            dID = oAgreementFactory.ID;
            sBranch = oAgreementFactory.Branch;
            sClient = oAgreementFactory.Client;
            sWorkPlace = oAgreementFactory.WorkPlace;
            dtAgreementDate = oAgreementFactory.AgreementDate;
            sNote = oAgreementFactory.Note;
            bIsValid = oAgreementFactory.IsValid;
            dtLASTUPDATE = oAgreementFactory.LASTUPDATE;
            oAgreementDetails = AgreementDetail.GetList(dID);
        }

        public bool Add(string CurrentUser)
        {
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.ID = dID;
            oAgreementFactory.Branch = sBranch;
            oAgreementFactory.Client = sClient;
            oAgreementFactory.WorkPlace = sWorkPlace;
            oAgreementFactory.AgreementDate = dtAgreementDate;
            oAgreementFactory.Note = sNote;
            oAgreementFactory.IsValid = bIsValid;
            oAgreementFactory.LASTUPDATE = dtLASTUPDATE;
            SqlTransaction Transaction = null;
            bool bSuccess = false;
            using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
            {
                Transaction = sdaFactory.StartTransaction();
                decimal AgreementID = oAgreementFactory.Add(sdaFactory, Transaction, CurrentUser);
                if (AgreementID == 0)
                    Transaction.Rollback();
                for (int i = 0; i < oAgreementDetails.Count; i++)
                {
                    oAgreementDetails[i].AgreementID = AgreementID;
                    bSuccess = oAgreementDetails[i].Add(sdaFactory, Transaction, CurrentUser);
                    if (!bSuccess)
                    {
                        Transaction.Rollback();
                        break;
                    }
                }
                if (!bSuccess)
                    Transaction.Rollback();
                else
                    Transaction.Commit();
            }
            return bSuccess;
        }

        public bool Update(string CurrentUser)
        {
            AuditTrailFactory oAuditTrail = new AuditTrailFactory();

            oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "1-start Get ID");
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.Get(dID);
            oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "2-Get ID Completed");
            oAgreementFactory.ID = dID;
            oAgreementFactory.Branch = sBranch;
            oAgreementFactory.Client = sClient;
            oAgreementFactory.WorkPlace = sWorkPlace;
            oAgreementFactory.AgreementDate = dtAgreementDate;
            oAgreementFactory.Note = sNote;
            oAgreementFactory.IsValid = bIsValid;
            oAgreementFactory.LASTUPDATE = dtLASTUPDATE;
            SqlTransaction Transaction = null;
            bool bSuccess = false;
            using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
            {
                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "3-start Save");
                Transaction = sdaFactory.StartTransaction();
                bSuccess = oAgreementFactory.Save(sdaFactory, Transaction, CurrentUser);
                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "4-Save Complete");
                if (!bSuccess)
                    Transaction.Rollback();
                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "5-Start Get List");
                List<AgreementDetail> oAgreementDetailList = AgreementDetail.GetList(dID);
                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "6-Get List Completed - " + oAgreementDetailList.Count.ToString());
                for (int i = 0; i < oAgreementDetailList.Count; i++)
                {
                    oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", oAgreementDetails.Count.ToString());
                    bool bFound = false;
                    for (int j = 0; j < oAgreementDetails.Count; j++)
                    {
                        oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "7A in loop ");
                        if (oAgreementDetailList[i].ID == oAgreementDetails[j].ID)
                        {
                            bFound = true;
                            break;
                        }
                    }
                    oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "7-Need Delete");
                    if (!bFound)
                    {
                        oAgreementDetailList[i].Delete(sdaFactory, Transaction);
                    }
                    oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "8-Deleted Complete");
                }

                int agreementdetailscnt = oAgreementDetails.Count;

                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", agreementdetailscnt.ToString());

                for (int i = 0; i < oAgreementDetails.Count; i++)
                {
                    oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "9-Final  add or update");
                    if (oAgreementDetails[i].ID == 0)
                        bSuccess = oAgreementDetails[i].Add(sdaFactory, Transaction, CurrentUser);
                    //oAuditTrail.Add(sUserName, string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "10-Finished Add");
                    else
                        bSuccess = oAgreementDetails[i].Update(sdaFactory, Transaction, CurrentUser);
                    //oAuditTrail.Add(sUserName, string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "11-Finished Update");
                    if (!bSuccess)
                    {
                        Transaction.Rollback();
                        break;
                    }
                }
                oAuditTrail.Add("KH", string.Empty, string.Empty, "AGREEMENT_Agreements", "ADD", "ADD Agreements", "10-Before commit Complete");
                if (!bSuccess)
                    Transaction.Rollback();
                else
                    Transaction.Commit();
            }
            return bSuccess;
        }

        public bool Delete(string CurrentUser)
        {
            AgreementFactory oAgreementFactory = new AgreementFactory();
            oAgreementFactory.Get(ID);
            bool bSuccess = oAgreementFactory.Delete(CurrentUser);
            return bSuccess;
        }
    }
}