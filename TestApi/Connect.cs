using Npgsql;

namespace TestApi
{
    public class Connect
    {
        public NpgsqlConnection GetConnection()
        {
            try
            {
                string host = "localhost";
                string port = "5432";
                string user = "postgres";
                string pass = "1234";
                string db = "db_cs_api";
                NpgsqlConnection conn = new NpgsqlConnection();
                conn.ConnectionString = $"Server = {host}; Username = {user}; Database = {db}; Port = {port}; Password = {pass};";
                conn.Open();
                return conn;
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
