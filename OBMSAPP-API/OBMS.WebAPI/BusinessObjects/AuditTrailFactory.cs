using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
namespace OBMS.WebAPI.BusinessObjects
{
    public class AuditTrailFactory
    {
        private static readonly IConfiguration configuration;
        protected decimal dID;
        protected string susername;
        protected string susertype;
        protected string sbranch;
        protected string smodule;
        protected string saction;
        protected string sdetail;
        protected string sdescription;
        protected DateTime dtcreatedate;
        protected List<AuditTrailFactory> oAuditTrailFactoryDetails;

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
        public string UserName
        {
            get
            {
                return susername;
            }
            set
            {
                susername = value;
            }
        }
        public string UserType
        {
            get
            {
                return susertype;
            }
            set
            {
                susertype = value;
            }
        }
        public string Branch
        {
            get
            {
                return sbranch;
            }
            set
            {
                sbranch = value;
            }
        }
        public string Module
        {
            get
            {
                return smodule;
            }
            set
            {
                smodule = value;
            }
        }
        public string Action
        {
            get
            {
                return saction;
            }
            set
            {
                saction = value;
            }
        }
        public string Details
        {
            get
            {
                return sdetail;
            }
            set
            {
                sdetail = value;
            }
        }
        public string Description
        {
            get
            {
                return sdescription;
            }
            set
            {
                sdescription = value;
            }
        }
        public DateTime CreatedDate
        {
            get
            {
                return dtcreatedate;
            }
            set
            {
                dtcreatedate = value;
            }
        }
        public List<AuditTrailFactory> AuditTrailFactoryDetails
        {
            get { return oAuditTrailFactoryDetails; }
            set { oAuditTrailFactoryDetails = value; }
        }
        public AuditTrailFactory()
        {
        }
        static AuditTrailFactory()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }
        public AuditTrailFactory(decimal ID, string UserName, string UserType, string Branch, string Module, string Action, string Details, string Description, DateTime CreatedDate)
        {
            dID = ID;
            susername = UserName;
            susertype = UserType;
            sbranch = Branch;
            smodule = Module;
            saction = Action;
            sdetail = Details;
            sdescription = Description;
            dtcreatedate = CreatedDate;
        }
        public static List<AuditTrailFactory> GetActionByList()
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT DISTINCT [UserName] FROM tblAuditTrail " +
                                    " Order By UserName ";

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AuditTrailFactory> AuditTrailFactoryList = new List<AuditTrailFactory>();
                            while (dr.Read())
                            {
                                string _UserName = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserName")))
                                {
                                    _UserName = dr.GetString(dr.GetOrdinal("UserName"));
                                }
                                AuditTrailFactoryList.Add(
                                    new AuditTrailFactory(
                                    0,
                                    _UserName,
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    DateTime.Now));
                            }
                            return AuditTrailFactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<AuditTrailFactory> GetList(DateTime DateFrom, DateTime DateTo)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT [ID],[UserName],[UserType], " +
                                    " [Branch],[Module],[Action], " +
                                    " [Detail],[Description],[CreatedDate] " +
                                    " FROM tblAuditTrail " +
                                    " WHERE CreatedDate BETWEEN @StartDate AND @EndDate  " +
                                    " Order By UserName ";
                    cmd.Parameters.AddWithValue("@StartDate", DateFrom);
                    cmd.Parameters.AddWithValue("@EndDate", DateTo);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AuditTrailFactory> AuditTrailFactoryList = new List<AuditTrailFactory>();
                            while (dr.Read())
                            {
                                string _UserName = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserName")))
                                {
                                    _UserName = dr.GetString(dr.GetOrdinal("UserName"));
                                }
                                string _UserType = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserType")))
                                {
                                    _UserType = dr.GetString(dr.GetOrdinal("UserType"));
                                }
                                string _Branch = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Branch")))
                                {
                                    _Branch = dr.GetString(dr.GetOrdinal("Branch"));
                                }
                                string _Module = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Module")))
                                {
                                    _Module = dr.GetString(dr.GetOrdinal("Module"));
                                }
                                string _Action = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Action")))
                                {
                                    _Action = dr.GetString(dr.GetOrdinal("Action"));
                                }
                                string _Detail = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Detail")))
                                {
                                    _Detail = dr.GetString(dr.GetOrdinal("Detail"));
                                }
                                string _Description = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Description")))
                                {
                                    _Description = dr.GetString(dr.GetOrdinal("Description"));
                                }


                                AuditTrailFactoryList.Add(
                                    new AuditTrailFactory(
                                    dr.GetDecimal(dr.GetOrdinal("ID")),
                                    _UserName,
                                    _UserType,
                                    _Branch,
                                    _Module,
                                    _Action,
                                    _Detail,
                                    _Description,
                                    dr.GetDateTime(dr.GetOrdinal("CreatedDate"))
                                    ));
                            }
                            return AuditTrailFactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<AuditTrailFactory> GetList(string ActionBy, DateTime DateFrom, DateTime DateTo)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT [ID],[UserName],[UserType], " +
                                    " [Branch],[Module],[Action], " +
                                    " [Detail],[Description],[CreatedDate] " +
                                    " FROM tblAuditTrail " +
                                    " WHERE UserName = @UserName  " +
                                    " AND CreatedDate BETWEEN @StartDate AND @EndDate  " +
                                    " Order By UserName,Module,Action,CreatedDate ";
                    cmd.Parameters.AddWithValue("@UserName", ActionBy);
                    cmd.Parameters.AddWithValue("@StartDate", DateFrom);
                    cmd.Parameters.AddWithValue("@EndDate", DateTo);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AuditTrailFactory> AuditTrailFactoryList = new List<AuditTrailFactory>();
                            while (dr.Read())
                            {
                                string _UserName = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserName")))
                                {
                                    _UserName = dr.GetString(dr.GetOrdinal("UserName"));
                                }
                                string _UserType = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserType")))
                                {
                                    _UserType = dr.GetString(dr.GetOrdinal("UserType"));
                                }
                                string _Branch = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Branch")))
                                {
                                    _Branch = dr.GetString(dr.GetOrdinal("Branch"));
                                }
                                string _Module = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Module")))
                                {
                                    _Module = dr.GetString(dr.GetOrdinal("Module"));
                                }
                                string _Action = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Action")))
                                {
                                    _Action = dr.GetString(dr.GetOrdinal("Action"));
                                }
                                string _Detail = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Detail")))
                                {
                                    _Detail = dr.GetString(dr.GetOrdinal("Detail"));
                                }
                                string _Description = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Description")))
                                {
                                    _Description = dr.GetString(dr.GetOrdinal("Description"));
                                }


                                AuditTrailFactoryList.Add(
                                    new AuditTrailFactory(
                                    dr.GetDecimal(dr.GetOrdinal("ID")),
                                    _UserName,
                                    _UserType,
                                    _Branch,
                                    _Module,
                                    _Action,
                                    _Detail,
                                    _Description,
                                    dr.GetDateTime(dr.GetOrdinal("CreatedDate"))
                                    ));
                            }
                            return AuditTrailFactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<AuditTrailFactory> GetList(string ActionBy, string Module, DateTime DateFrom, DateTime DateTo)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT [ID],[UserName],[UserType], " +
                                    " [Branch],[Module],[Action], " +
                                    " [Detail],[Description],[CreatedDate] " +
                                    " FROM tblAuditTrail " +
                                    " WHERE UserName = @UserName  " +
                                    " AND CreatedDate BETWEEN @StartDate AND @EndDate  " +
                                    " AND Module =@Module  " +
                                    " Order By UserName,Module,Action,CreatedDate ";
                    cmd.Parameters.AddWithValue("@UserName", ActionBy);
                    cmd.Parameters.AddWithValue("@StartDate", DateFrom);
                    cmd.Parameters.AddWithValue("@EndDate", DateTo);
                    cmd.Parameters.AddWithValue("@Module", Module);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AuditTrailFactory> AuditTrailFactoryList = new List<AuditTrailFactory>();
                            while (dr.Read())
                            {
                                string _UserName = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserName")))
                                {
                                    _UserName = dr.GetString(dr.GetOrdinal("UserName"));
                                }
                                string _UserType = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserType")))
                                {
                                    _UserType = dr.GetString(dr.GetOrdinal("UserType"));
                                }
                                string _Branch = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Branch")))
                                {
                                    _Branch = dr.GetString(dr.GetOrdinal("Branch"));
                                }
                                string _Module = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Module")))
                                {
                                    _Module = dr.GetString(dr.GetOrdinal("Module"));
                                }
                                string _Action = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Action")))
                                {
                                    _Action = dr.GetString(dr.GetOrdinal("Action"));
                                }
                                string _Detail = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Detail")))
                                {
                                    _Detail = dr.GetString(dr.GetOrdinal("Detail"));
                                }
                                string _Description = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Description")))
                                {
                                    _Description = dr.GetString(dr.GetOrdinal("Description"));
                                }


                                AuditTrailFactoryList.Add(
                                    new AuditTrailFactory(
                                    dr.GetDecimal(dr.GetOrdinal("ID")),
                                    _UserName,
                                    _UserType,
                                    _Branch,
                                    _Module,
                                    _Action,
                                    _Detail,
                                    _Description,
                                    dr.GetDateTime(dr.GetOrdinal("CreatedDate"))
                                    ));
                            }
                            return AuditTrailFactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<AuditTrailFactory> GetList(string ActionBy, string Module, string Action, DateTime DateFrom, DateTime DateTo)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT [ID],[UserName],[UserType], " +
                                    " [Branch],[Module],[Action], " +
                                    " [Detail],[Description],[CreatedDate] " +
                                    " FROM tblAuditTrail " +
                                    " WHERE UserName = @UserName  " +
                                    " AND CreatedDate BETWEEN @StartDate AND @EndDate  " +
                                    " AND Module =@Module  " +
                                    " AND Action =@Action  " +
                                    " Order By UserName,Module,Action,CreatedDate ";
                    cmd.Parameters.AddWithValue("@UserName", ActionBy);
                    cmd.Parameters.AddWithValue("@StartDate", DateFrom);
                    cmd.Parameters.AddWithValue("@EndDate", DateTo);
                    cmd.Parameters.AddWithValue("@Module", Module);
                    cmd.Parameters.AddWithValue("@Action", Action);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<AuditTrailFactory> AuditTrailFactoryList = new List<AuditTrailFactory>();
                            while (dr.Read())
                            {
                                string _UserName = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserName")))
                                {
                                    _UserName = dr.GetString(dr.GetOrdinal("UserName"));
                                }
                                string _UserType = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("UserType")))
                                {
                                    _UserType = dr.GetString(dr.GetOrdinal("UserType"));
                                }
                                string _Branch = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Branch")))
                                {
                                    _Branch = dr.GetString(dr.GetOrdinal("Branch"));
                                }
                                string _Module = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Module")))
                                {
                                    _Module = dr.GetString(dr.GetOrdinal("Module"));
                                }
                                string _Action = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Action")))
                                {
                                    _Action = dr.GetString(dr.GetOrdinal("Action"));
                                }
                                string _Detail = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Detail")))
                                {
                                    _Detail = dr.GetString(dr.GetOrdinal("Detail"));
                                }
                                string _Description = string.Empty;
                                if (!dr.IsDBNull(dr.GetOrdinal("Description")))
                                {
                                    _Description = dr.GetString(dr.GetOrdinal("Description"));
                                }


                                AuditTrailFactoryList.Add(
                                    new AuditTrailFactory(
                                    dr.GetDecimal(dr.GetOrdinal("ID")),
                                    _UserName,
                                    _UserType,
                                    _Branch,
                                    _Module,
                                    _Action,
                                    _Detail,
                                    _Description,
                                    dr.GetDateTime(dr.GetOrdinal("CreatedDate"))
                                    ));
                            }
                            return AuditTrailFactoryList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool Add(string CurrentUser)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " INSERT INTO tblAuditTrail ([UserName],[UserType],[Branch],[Module],[Action],[Detail],[Description],[CreatedDate]) VALUES (@UserName,@UserType,@Branch,@Module,@Action,@Detail,@Description,GetDate())";
                    cmd.Parameters.AddWithValue("@UserName", CurrentUser);
                    cmd.Parameters.AddWithValue("@UserType", susertype);
                    cmd.Parameters.AddWithValue("@Branch", sbranch);
                    cmd.Parameters.AddWithValue("@Module", smodule);
                    cmd.Parameters.AddWithValue("@Action", saction);
                    cmd.Parameters.AddWithValue("@Detail", sdetail);
                    cmd.Parameters.AddWithValue("@Description", sdescription);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.StartTransaction();
                        sdaFactory.ExecuteSQL(cmd);
                        sdaFactory.EndTransaction(true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool Add(string CurrentUser, string UserType, string Branch, string Module, string Action, string Details, string Description)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " INSERT INTO tblAuditTrail ([UserName],[UserType],[Branch],[Module],[Action],[Detail],[Description],[CreatedDate]) VALUES (@UserName,@UserType,@Branch,@Module,@Action,@Detail,@Description,GetDate())";
                    cmd.Parameters.AddWithValue("@UserName", CurrentUser);
                    cmd.Parameters.AddWithValue("@UserType", UserType);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@Module", Module);
                    cmd.Parameters.AddWithValue("@Action", Action);
                    cmd.Parameters.AddWithValue("@Detail", Details);
                    cmd.Parameters.AddWithValue("@Description", Description);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.StartTransaction();
                        sdaFactory.ExecuteSQL(cmd);
                        sdaFactory.EndTransaction(true);
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