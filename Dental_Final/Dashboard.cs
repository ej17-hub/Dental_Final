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
        private readonly  string connectionString = "Server=DESKTOP-PB8NME4\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";


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

                // load today's appointments into dataGridView1
                LoadTodaysAppointments();
                
                // load activity log into dataGridView2
                LoadActivityLog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update dashboard stats: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Load today's appointments into dataGridView1
        // Load today's appointments into dataGridView1
        private void LoadTodaysAppointments()
        {
            try
            {
                DateTime today = DateTime.Today;

                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                // Create columns for appointment details
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "appointment_id",
                    HeaderText = "ID",
                    Visible = false
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "time",
                    HeaderText = "Time",
                    Width = 80,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "patient",
                    HeaderText = "Patient",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "dentist",
                    HeaderText = "Dentist",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "service",
                    HeaderText = "Service",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "status",
                    HeaderText = "Status",
                    Width = 100,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                });

                // Build safe name expressions
                string patientExpr;
                if (ColumnExists("patients", "first_name") && ColumnExists("patients", "last_name"))
                    patientExpr = "RTRIM(ISNULL(p.first_name,'')) + ' ' + RTRIM(ISNULL(p.last_name,''))";
                else if (ColumnExists("patients", "name"))
                    patientExpr = "RTRIM(ISNULL(p.name,''))";
                else
                    patientExpr = "CONVERT(varchar(20), p.patient_id)";

                string dentistExpr;
                if (ColumnExists("dentists", "first_name") && ColumnExists("dentists", "last_name"))
                    dentistExpr = "RTRIM(ISNULL(d.first_name,'')) + ' ' + RTRIM(ISNULL(d.last_name,''))";
                else if (ColumnExists("dentists", "name"))
                    dentistExpr = "RTRIM(ISNULL(d.name,''))";
                else
                    dentistExpr = "CONVERT(varchar(20), d.dentist_id)";

                string serviceExpr;
                if (ColumnExists("services", "name"))
                    serviceExpr = "s.name";
                else
                    serviceExpr = "CONVERT(varchar(20), s.service_id)";

                // Query to get today's appointments with aggregated services
                string sql = @"
            SELECT 
                a.appointment_id,
                a.appointment_time,
                " + patientExpr + @" AS patient_name,
                " + dentistExpr + @" AS dentist_name,
                " + serviceExpr + @" AS service_name,
                ISNULL(a.notes,'') AS notes,
                ISNULL((SELECT STUFF((
                    SELECT ', ' + ISNULL(s2.name, CONVERT(varchar(20), aps2.service_id)) 
                    FROM appointments_services aps2 
                    LEFT JOIN services s2 ON aps2.service_id = s2.service_id 
                    WHERE aps2.appointment_id = a.appointment_id 
                    FOR XML PATH('')), 1, 2, '')), '') AS services_list
            FROM appointments a
            LEFT JOIN patients p ON a.patient_id = p.patient_id
            LEFT JOIN dentists d ON a.dentist_id = d.dentist_id
            LEFT JOIN services s ON a.service_id = s.service_id
            WHERE CAST(a.appointment_date AS DATE) = @date
            ORDER BY a.appointment_time";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@date", SqlDbType.Date).Value = today;
                    conn.Open();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var id = rdr["appointment_id"] != DBNull.Value ? Convert.ToInt32(rdr["appointment_id"]) : -1;

                            // Format appointment time
                            string timeText = string.Empty;
                            var apptTimeObj = rdr["appointment_time"];
                            if (apptTimeObj != null && apptTimeObj != DBNull.Value)
                            {
                                var apptTimeStr = apptTimeObj.ToString();
                                TimeSpan ts;
                                DateTime dt;
                                if (TimeSpan.TryParse(apptTimeStr, out ts))
                                {
                                    timeText = DateTime.Today.Add(ts).ToString("h:mm tt");
                                }
                                else if (DateTime.TryParse(apptTimeStr, out dt))
                                {
                                    timeText = dt.ToString("h:mm tt");
                                }
                                else
                                {
                                    timeText = apptTimeStr;
                                }
                            }

                            var patient = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString() : string.Empty;
                            var dentist = rdr["dentist_name"] != DBNull.Value ? rdr["dentist_name"].ToString() : string.Empty;
                            var singleService = rdr["service_name"] != DBNull.Value ? rdr["service_name"].ToString() : string.Empty;
                            var servicesAgg = rdr["services_list"] != DBNull.Value ? rdr["services_list"].ToString() : string.Empty;
                            var notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;

                            // Prefer aggregated service list when present
                            string serviceDisplay = !string.IsNullOrEmpty(servicesAgg) ? servicesAgg : singleService;

                            // Determine status
                            string status = "Pending";
                            if (notes.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0)
                                status = "Completed";
                            else if (notes.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0)
                                status = "Cancelled";

                            // Add row to grid
                            int rowIndex = dataGridView1.Rows.Add(id, timeText, patient, dentist, serviceDisplay, status);

                            // Color code the row based on status - only green for completed, yellow for pending/cancelled
                            var row = dataGridView1.Rows[rowIndex];
                            if (status == "Completed")
                            {
                                row.DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                // Both Pending and Cancelled use light yellow
                                row.DefaultCellStyle.BackColor = Color.LightYellow;
                            }
                        }
                    }
                }

                // Grid appearance - clear header with no background color
                dataGridView1.ReadOnly = true;
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.AllowUserToAddRows = false;
                dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView1.MultiSelect = false;

                // Apply simple header style - bold text, default background (clear/white)
                dataGridView1.EnableHeadersVisualStyles = false;
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control; // Default gray header
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading today's appointments: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void button6_Click(object sender, EventArgs e)
        {
            Adding_Patient adding_Patient = new Adding_Patient();
            adding_Patient.Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Add_Appointment add_Appointment = new Add_Appointment();
            add_Appointment.Show();
        }
    }
}