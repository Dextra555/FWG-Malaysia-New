using Microsoft.SqlServer.ReportingServices2010;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class AccountingRepository : IAccountingRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IConfiguration _configuration;

        public AccountingRepository(OBMSDbContext oBMSDbContext, IConfiguration configuration)
        {
            _oBMSDbContext = oBMSDbContext;
            _configuration = configuration;

        }
        public async Task<List<AccountGLReportDto>> GetDataList(int processYear, string branch)
        {
            var result = new List<AccountGLReportDto>();
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SP_GLListingReport", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Year", processYear);
                cmd.Parameters.AddWithValue("@Branch", branch);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(ReadReport(reader));
                    }
                }
            }

            return result;
        }

        public async Task<List<AccountGLReportDto>> GetDataList(int processYear, string branch, string type)
        {
            var result = new List<AccountGLReportDto>();
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SP_GLListingReportWithType", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Year", processYear);
                cmd.Parameters.AddWithValue("@Branch", branch);
                cmd.Parameters.AddWithValue("@Type", type);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(ReadReport(reader));
                    }
                }
            }

            return result;
        }

        public async Task<bool> GetRecord(int processYear, string branch)
        {
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SP_CheckExistsDataOnGLTable", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessYear", processYear);
                cmd.Parameters.AddWithValue("@Branch", branch);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync(); // returns true if at least one record exists
                }
            }
        }

        public async Task<bool> GetRecordWithType(int processYear, string branch, string type)
        {
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SP_CheckExistsDataOnGLTableWithType", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessYear", processYear);
                cmd.Parameters.AddWithValue("@Branch", branch);
                cmd.Parameters.AddWithValue("@Type", type);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync(); // returns true if at least one record exists
                }
            }
        }

        public async Task<bool> AddGLRecordAsync(string currentUser, List<AccountGLReportDto> accountGLReportDto)
        {
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                foreach (var item in accountGLReportDto)
                {
                    using (var cmd = new SqlCommand("SP_InsertIntoGLTable", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@ProcessYear", SqlDbType.Int).Value = (object)item.ProcessYear ?? DBNull.Value;
                        cmd.Parameters.AddWithValue("@Branch", item.Branch ?? (object)DBNull.Value);
                        cmd.Parameters.Add("@IssueDate", SqlDbType.DateTime).Value = (object)item.IssueDate ?? DBNull.Value;
                        cmd.Parameters.AddWithValue("@RefNo", item.RefNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChequeNo", item.ChequeNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", item.Description ?? (object)DBNull.Value);
                        cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = (object)item.TotalAmount ?? DBNull.Value;
                        cmd.Parameters.AddWithValue("@Type", item.Type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromTable", item.FromTable ?? (object)DBNull.Value);
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = string.IsNullOrEmpty(item.Category) ? "DefaultCategory" : item.Category;
                        cmd.Parameters.AddWithValue("@CreatedBy", currentUser ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return true;
        }

        public async Task<bool> DeleteGLRecordAsync(int processYear, string branch, string currentUser)
        {
            var connectionString = _configuration.GetConnectionString("obms");

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SP_DeleteFromGLTable", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessYear", processYear);
                cmd.Parameters.AddWithValue("@Branch", branch);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
        }

        private AccountGLReportDto ReadReport(SqlDataReader reader)
        {
            return new AccountGLReportDto
            {                
                Branch = reader.GetString(reader.GetOrdinal("Branch")),
                IssueDate = reader.GetDateTime(reader.GetOrdinal("IssueDate")),
                RefNo = reader.GetString(reader.GetOrdinal("RefNo")),
                ChequeNo = reader.GetString(reader.GetOrdinal("ChequeNo")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                FromTable = reader.GetString(reader.GetOrdinal("FromTable"))                
            };
        }
    }
}
