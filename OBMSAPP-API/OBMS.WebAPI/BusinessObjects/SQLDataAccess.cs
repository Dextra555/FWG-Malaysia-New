using System.Data;
using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public class SQLDataAccess : DataAccess, IDisposable
    {
        // Track whether Dispose has been called.
        private bool disposed = false;
        private SqlTransaction obms_transaction;
         public SQLDataAccess(IConfiguration configuration) : base(configuration)
        {
            try
            {
                obms_connection = new SqlConnection();
                obms_connection.ConnectionString = ConnectionString;
                obms_connection.Open();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to connect to database. Reason :" + e.Message);
            }
        }
        public bool checkConnectionIsConnected()
        {
            bool CheckConnection = false;
            if (obms_connection.State == ConnectionState.Open)
            {
                try
                {
                    CheckConnection = true;
                }
                catch
                {
                    CheckConnection = false;
                }
            }
            return CheckConnection;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (obms_connection != null)
                    {
                        if (obms_connection.State != ConnectionState.Closed)
                        {
                            obms_connection.Close();
                        }
                    }
                }
            }
            this.disposed = true;
        }
        ~SQLDataAccess()
        {
            Dispose(false);
        }
        public override SqlDataReader RetrieveData(SqlCommand cmd)
        {
            try
            {
                cmd.CommandTimeout = 300;
                cmd.Connection = obms_connection;
                if (obms_transaction != null)
                    cmd.Transaction = obms_transaction;
                SqlDataReader dr = cmd.ExecuteReader();
                cmd.Dispose();
                return dr;
            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.RetrieveData: Following Error occurred :" + e.Message);
            }
        }
        public override bool ExecuteSQL(SqlCommand cmd)
        {
            try
            {
                cmd.Connection = obms_connection;
                if (obms_transaction != null)
                    cmd.Transaction = obms_transaction;
                int i = cmd.ExecuteNonQuery();
                cmd.Dispose();
                return (i > 0);
            }
            catch (SqlException sx)
            {
                switch (sx.Number)
                {
                    case 547:
                        throw new Exception("Record can not be deleted because it has related records.");
                    default:
                        throw new Exception("Database Error occurred." + sx.Message);
                }


            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.ExecuteSQL: Following Error occurred :" + e.Message);
            }
        }
        public override bool ExecuteSQL(string sqlquery)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(sqlquery);
                cmd.Connection = obms_connection;
                if (obms_transaction != null)
                    cmd.Transaction = obms_transaction;
                int i = cmd.ExecuteNonQuery();
                cmd.Dispose();
                return (i > 0);
            }
            catch (SqlException sx)
            {
                switch (sx.Number)
                {
                    case 547:
                        throw new Exception("Record can not be deleted because it has related records.");
                    default:
                        throw new Exception("Database Error occurred." + sx.Message);
                }


            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.ExecuteSQL: Following Error occurred :" + e.Message);
            }
        }
        public override SqlTransaction StartTransaction()
        {
            try
            {
                obms_transaction = obms_connection.BeginTransaction();
                return obms_transaction;
            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.StartTransaction: Following Error occurred :" + e.Message);
            }
        }
        public override bool StartTransaction(SqlTransaction transaction)
        {
            try
            {
                obms_transaction = transaction;
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.StartTransaction: Following Error occurred :" + e.Message);
            }
        }
        public override bool EndTransaction(bool SaveTransaction)
        {
            try
            {
                if (SaveTransaction)
                    obms_transaction.Commit();
                else
                    obms_transaction.Rollback();
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("SQLDataAccess.EndTransaction: Following Error occurred :" + e.Message);
            }
            finally
            {
                obms_connection.Close();
            }
        }
    }
}
