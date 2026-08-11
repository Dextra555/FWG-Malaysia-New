using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
namespace OBMS.WebAPI.BusinessObjects
{
    public class AgreementFactory
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

        public AgreementFactory()
        {

        }
        static AgreementFactory()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }

        public AgreementFactory(decimal ID, string Branch, string Client, string WorkPlace, DateTime AgreementDate, string Note, bool isvalid, DateTime LASTUPDATE)
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

        public static List<AgreementFactory> GetList(string Branch)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (string.IsNullOrEmpty(Branch))
                        //cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate FROM Agreement t WHERE AgreementDate = (SELECT MAX(AgreementDate) FROM Agreement WHERE Client = t.Client and Branch=t.Branch) AND IsDeleted=0 ";
                        //Commented by Kean Hong to remove the isdeleted checking as this is not the latest source
                        cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate FROM Agreement t WHERE AgreementDate = (SELECT MAX(AgreementDate) FROM Agreement WHERE Client = t.Client and Branch=t.Branch) ";
                    else
                    {
                        //cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate FROM Agreement t WHERE AgreementDate = (SELECT MAX(AgreementDate) FROM Agreement WHERE Client = t.Client and Branch=t.Branch) AND Branch=@Branch AND IsDeleted=0 ";
                        //Commented by Kean Hong to remove the isdeleted checking as this is not the latest source
                        cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate FROM Agreement t WHERE AgreementDate = (SELECT MAX(AgreementDate) FROM Agreement WHERE Client = t.Client and Branch=t.Branch) AND Branch=@Branch ";
                        cmd.Parameters.AddWithValue("@Branch", Branch);
                    }
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AgreementFactory> agreementfactoryList = new List<AgreementFactory>();
                            while (dr.Read())
                            {
                                agreementfactoryList.Add(
                                    new AgreementFactory(
                                    dr.GetDecimal(dr.GetOrdinal("ID")),
                                    dr.GetString(dr.GetOrdinal("Branch")),
                                    dr.GetString(dr.GetOrdinal("Client")),
                                    dr.GetString(dr.GetOrdinal("WorkPlace")),
                                    dr.GetDateTime(dr.GetOrdinal("AgreementDate")),
                                    string.Empty, false, DateTime.Now));
                            }
                            return agreementfactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void Get(string Branch, string Client, DateTime AgreementPeriod)
        {
            try
            {

                String strDate = AgreementPeriod.ToString("yyyy-MM-dd");

                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = " SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  Branch=@Branch AND Client=@Client AND AgreementDate <= CAST('20/02/2009 00:00:00' AS DATETIME) ORDER BY AgreementDate DESC ";
                    //cmd.CommandText = " SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE Branch='PF004-SBG' AND Client='C0001' AND AgreementDate <= CAST('07/31/2008 00:00:00' AS DATETIME) ORDER BY AgreementDate DESC";
                    //cmd.CommandText = " SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  Branch=@Branch AND Client=@Client AND AgreementDate <= CAST('" + AgreementPeriod + "' AS DATETIME) ORDER BY AgreementDate DESC ";
                    //cmd.CommandText = " SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  Branch=@Branch AND Client=@Client AND AgreementDate <=@AgreementPeriod ORDER BY AgreementDate DESC ";

                    //Kean Hong taken out the isdeleted checking from previous source, this source doesnt applied to production.
                    cmd.CommandText = " SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  Branch=@Branch AND Client=@Client AND AgreementDate <=@AgreementPeriod ORDER BY AgreementDate DESC,LASTUPDATE DESC ";
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@Client", Client);
                    cmd.Parameters.AddWithValue("@AgreementPeriod", AgreementPeriod);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            AgreementFactory agreementfactory = new AgreementFactory();
                            while (dr.Read())
                            {
                                dID = Convert.ToDecimal(dr["ID"]);
                                sBranch = dr.GetString(dr.GetOrdinal("Branch"));
                                sClient = dr.GetString(dr.GetOrdinal("Client"));
                                sWorkPlace = dr.GetString(dr.GetOrdinal("WorkPlace"));
                                dtAgreementDate = dr.GetDateTime(dr.GetOrdinal("AgreementDate"));
                                sNote = dr.GetString(dr.GetOrdinal("Note"));
                                bIsValid = dr.GetBoolean(dr.GetOrdinal("IsValid"));
                                dtLASTUPDATE = dr.GetDateTime(dr.GetOrdinal("LASTUPDATE"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void Get(decimal ID)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  ID=@ID AND IsDeleted=0";
                    //Kean Hong taken out the isdeleted checking
                    cmd.CommandText = " SELECT ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE FROM Agreement WHERE  ID=@ID";
                    cmd.Parameters.AddWithValue("@ID", ID);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            AgreementFactory agreementfactory = new AgreementFactory();
                            while (dr.Read())
                            {
                                dID = dr.GetDecimal(dr.GetOrdinal("ID"));
                                sBranch = dr.GetString(dr.GetOrdinal("Branch"));
                                sClient = dr.GetString(dr.GetOrdinal("Client"));
                                sWorkPlace = dr.GetString(dr.GetOrdinal("WorkPlace"));
                                dtAgreementDate = dr.GetDateTime(dr.GetOrdinal("AgreementDate"));
                                sNote = dr.GetString(dr.GetOrdinal("Note"));
                                bIsValid = dr.GetBoolean(dr.GetOrdinal("IsValid"));
                                dtLASTUPDATE = dr.GetDateTime(dr.GetOrdinal("LASTUPDATE"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        public decimal Add(SQLDataAccess sdaFactory, SqlTransaction Transaction, string CurrentUser)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " INSERT INTO Agreement ([Branch],[Client],[WorkPlace],[AgreementDate],[Note],[IsValid],[LASTUPDATE],[LastUpdatedBy]) VALUES (@Branch,@Client,@WorkPlace,@AgreementDate,@Note,@IsValid,@LASTUPDATE,@LastUpdatedBy); SELECT SCOPE_IDENTITY()";
                    cmd.Parameters.AddWithValue("@ID", dID);
                    cmd.Parameters.AddWithValue("@Branch", sBranch);
                    cmd.Parameters.AddWithValue("@Client", sClient);
                    cmd.Parameters.AddWithValue("@WorkPlace", sWorkPlace);
                    cmd.Parameters.AddWithValue("@AgreementDate", dtAgreementDate);
                    cmd.Parameters.AddWithValue("@Note", sNote);
                    cmd.Parameters.AddWithValue("@IsValid", bIsValid);
                    cmd.Parameters.AddWithValue("@LASTUPDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                    sdaFactory.StartTransaction(Transaction);
                    SqlDataReader dr = sdaFactory.RetrieveData(cmd);
                    dr.Read();
                    decimal dAgreementID = dr.GetDecimal(0);
                    dr.Close();
                    return dAgreementID;

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Save(SQLDataAccess sdaFactory, SqlTransaction Transaction, string CurrentUser)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " UPDATE Agreement SET [Branch] = @Branch,[Client] = @Client,[WorkPlace] = @WorkPlace,[AgreementDate] = @AgreementDate,[Note] = @Note,[IsValid] = @IsValid, [LASTUPDATE] = @LASTUPDATE, [LastUpdatedBy] = @LastUpdatedBy WHERE [ID] = @ID ";
                    cmd.Parameters.AddWithValue("@ID", dID);
                    cmd.Parameters.AddWithValue("@Branch", sBranch);
                    cmd.Parameters.AddWithValue("@Client", sClient);
                    cmd.Parameters.AddWithValue("@WorkPlace", sWorkPlace);
                    cmd.Parameters.AddWithValue("@AgreementDate", dtAgreementDate);
                    cmd.Parameters.AddWithValue("@Note", sNote);
                    cmd.Parameters.AddWithValue("@IsValid", bIsValid);
                    cmd.Parameters.AddWithValue("@LASTUPDATE", dtLASTUPDATE);
                    cmd.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                    sdaFactory.StartTransaction(Transaction);
                    sdaFactory.ExecuteSQL(cmd);
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Delete(string CurrentUser)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = " UPDATE Agreement SET [IsDeleted] = 1,[LASTUPDATE] = GetDate(), [LastUpdatedBy] = @LastUpdatedBy WHERE   [ID] = @ID";
                    //Kean Hong taken out isdeleted 
                    cmd.CommandText = " UPDATE Agreement SET [LASTUPDATE] = GetDate(), [LastUpdatedBy] = @LastUpdatedBy WHERE   [ID] = @ID";
                    cmd.Parameters.AddWithValue("@ID", dID);
                    cmd.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);

                    SqlCommand cmdInvoice = new SqlCommand();
                    cmdInvoice.CommandText = " UPDATE ClientInvoice SET [IsDeleted] = 'Y',[LASTUPDATE] = GetDate(), [LastUpdatedBy] = @LastUpdatedBy WHERE AgreementID=@ID";
                    cmdInvoice.Parameters.AddWithValue("@ID", dID);
                    cmdInvoice.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.StartTransaction();
                        sdaFactory.ExecuteSQL(cmd);
                        sdaFactory.ExecuteSQL(cmdInvoice);
                        sdaFactory.EndTransaction(true);
                        cmdInvoice.Dispose();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}