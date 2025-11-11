using System;
using System.Data.SqlClient;

namespace Dental_Final
{
    public static class ActivityLogger
    {
        private static readonly string connectionString = "Server=DESKTOP-PB8NME4\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

        // Ensures activity_log table exists then inserts a new record
        public static void Log(string message, string username = "Admin")
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    // create table if not exists
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.activity_log','U') IS NULL
                        BEGIN
                            CREATE TABLE dbo.activity_log
                            (
                                id INT IDENTITY(1,1) PRIMARY KEY,
                                message NVARCHAR(1000) NOT NULL,
                                username NVARCHAR(200) NULL,
                                created_at DATETIME NOT NULL DEFAULT(GETDATE())
                            );
                        END
                        ";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "INSERT INTO dbo.activity_log (message, username, created_at) VALUES (@m, @u, GETDATE())";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@m", message);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // swallow logging errors to avoid crashing calling flows
            }
        }
    }
}