using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public abstract class DataAccess
    {
        private readonly IConfiguration _configuration;

        protected DataAccess(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected SqlConnection obms_connection = null;
        protected string ConnectionString
        {
            get
            {
                string obms_conn = _configuration.GetConnectionString("obms");
                if (string.IsNullOrEmpty(obms_conn))
                    throw new NullReferenceException("Connection String is not specified");
                return obms_conn;
            }
        }

        public abstract SqlDataReader RetrieveData(SqlCommand cmd);
        public abstract bool ExecuteSQL(SqlCommand cmd);
        public abstract bool ExecuteSQL(string sqlquery);
        public abstract SqlTransaction StartTransaction();
        public abstract bool StartTransaction(SqlTransaction transaction);
        public abstract bool EndTransaction(bool saveTransaction);
    }
}
