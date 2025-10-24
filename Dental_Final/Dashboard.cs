using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Dashboard : Form
    {
        private readonly string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

        public Dashboard()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            // update counts and revenue for today
            UpdateDashboardStats();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patients Patients = new Patients();
            Patients.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Services services = new Services();
            services.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.Show();
            this.Hide();
        }

        // Checks whether a column exists on a table (dbo schema)
        private bool ColumnExists(string tableName, string columnName)
        {
            const string sql = "SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name = @col";
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@table", "dbo." + tableName);
                cmd.Parameters.AddWithValue("@col", columnName);
                conn.Open();
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        // Check table existence (used to detect appointments_services linking table)
        private bool TableExists(string tableName)
        {
            const string sql = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table";
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        // Update the four dashboard labels using today's date and the same classification rules as Appointments
        private void UpdateDashboardStats()
        {
            DateTime today = DateTime.Today;

            try
            {
                int totalToday = 0;
                int completed = 0;
                int cancelled = 0;
                decimal revenue = 0m;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // total appointments today
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CAST(appointment_date AS DATE) = @date", conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                        totalToday = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // completed count (notes contains 'completed' - case-insensitive via LOWER)
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CAST(appointment_date AS DATE) = @date AND LOWER(ISNULL(notes,'')) LIKE '%completed%'", conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                        completed = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // cancelled count
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM appointments WHERE CAST(appointment_date AS DATE) = @date AND LOWER(ISNULL(notes,'')) LIKE '%cancel%'", conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                        cancelled = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // revenue: prefer appointments.price if present; otherwise aggregate services
                    if (ColumnExists("appointments", "price"))
                    {
                        using (var cmd = new SqlCommand("SELECT SUM(price) FROM appointments WHERE CAST(appointment_date AS DATE) = @date AND LOWER(ISNULL(notes,'')) LIKE '%completed%'", conn))
                        {
                            cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                            var o = cmd.ExecuteScalar();
                            if (o != DBNull.Value && o != null)
                                revenue = Convert.ToDecimal(o);
                        }
                    }
                    else if (TableExists("appointments_services"))
                    {
                        using (var cmd = new SqlCommand(@"
                            SELECT SUM(s.price)
                            FROM appointments a
                            JOIN appointments_services aps ON a.appointment_id = aps.appointment_id
                            JOIN services s ON aps.service_id = s.service_id
                            WHERE CAST(a.appointment_date AS DATE) = @date
                              AND LOWER(ISNULL(a.notes,'')) LIKE '%completed%';", conn))
                        {
                            cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                            var o = cmd.ExecuteScalar();
                            if (o != DBNull.Value && o != null)
                                revenue = Convert.ToDecimal(o);
                        }
                    }
                    else
                    {
                        // fallback: sum prices by the single service_id on appointments if services.price exists
                        if (ColumnExists("services", "price"))
                        {
                            using (var cmd = new SqlCommand(@"
                                SELECT SUM(s.price)
                                FROM appointments a
                                LEFT JOIN services s ON a.service_id = s.service_id
                                WHERE CAST(a.appointment_date AS DATE) = @date
                                  AND LOWER(ISNULL(a.notes,'')) LIKE '%completed%';", conn))
                            {
                                cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                                var o = cmd.ExecuteScalar();
                                if (o != DBNull.Value && o != null)
                                    revenue = Convert.ToDecimal(o);
                            }
                        }
                    }
                }

                // update UI
                lblTodayAppt.Text = totalToday.ToString();
                lblCompleted.Text = completed.ToString();
                lblCancelled.Text = cancelled.ToString();
                lblRevenue.Text = "₱" + revenue.ToString("N2");

                // load activity log
                LoadActivityLog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update dashboard stats: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // load recent activity log entries into dataGridView2
        private void LoadActivityLog()
        {
            try
            {
                dataGridView2.Rows.Clear();
                dataGridView2.Columns.Clear();

                // create columns explicitly so we can control sizing and alignment
                var msgCol = new DataGridViewTextBoxColumn { Name = "message", HeaderText = "Activity", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
                var timeCol = new DataGridViewTextBoxColumn { Name = "created_at", HeaderText = "Time", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells };

                dataGridView2.Columns.Add(msgCol);
                dataGridView2.Columns.Add(timeCol);

                // right-align the time column header and cells
                dataGridView2.Columns["created_at"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView2.Columns["created_at"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT TOP 100 message, created_at FROM dbo.activity_log ORDER BY created_at DESC", conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var msg = rdr["message"] != DBNull.Value ? rdr["message"].ToString() : string.Empty;
                            var dt = rdr["created_at"] != DBNull.Value ? Convert.ToDateTime(rdr["created_at"]) : (DateTime?)null;
                            // Format example: "10:30 AM January 2, 2025"
                            var timeText = dt.HasValue ? dt.Value.ToString("h:mm tt MMMM d, yyyy") : string.Empty;
                            dataGridView2.Rows.Add(msg, timeText);
                        }
                    }
                }

                // grid appearance
                dataGridView2.ReadOnly = true;
                dataGridView2.RowHeadersVisible = false;

                // ensure message column fills remaining space and time column stays compact and right aligned
                dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                if (dataGridView2.Columns.Contains("message"))
                    dataGridView2.Columns["message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (dataGridView2.Columns.Contains("created_at"))
                {
                    dataGridView2.Columns["created_at"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    // cap width so it remains small; increased slightly to accommodate month names
                    dataGridView2.Columns["created_at"].Width = Math.Min(dataGridView2.Columns["created_at"].Width, 240);
                }
            }
            catch
            {
                // ignore activity load failures
            }
        }
    }
}