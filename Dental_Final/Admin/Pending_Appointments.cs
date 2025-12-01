using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final.Admin
{
    public partial class Pending_Appointments : Form
    {
        private readonly string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Pending_Appointments()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // wire load and date change
            this.Load += Pending_Appointments_Load;
            dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;

            // visual tweaks for multiline services
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // wire cell click for approve/decline buttons
            dataGridView1.CellContentClick -= DataGridView1_CellContentClick;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
        }

        private void Pending_Appointments_Load(object sender, EventArgs e)
        {
            // default to today
            LoadPendingRequests(dateTimePicker1.Value.Date);
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LoadPendingRequests(dateTimePicker1.Value.Date);
        }

        private void EnsureGridColumns()
        {
            if (dataGridView1.Columns.Count > 0) return;

            dataGridView1.Columns.Clear();

            // hidden request id for row actions
            var idCol = new DataGridViewTextBoxColumn
            {
                Name = "request_id",
                HeaderText = "RequestId",
                Visible = false
            };
            dataGridView1.Columns.Add(idCol);

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "patient",
                HeaderText = "Patient",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "requested_time",
                HeaderText = "Requested Time",
                Width = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "services",
                HeaderText = "Services",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });

            // Approve button column
            var approveCol = new DataGridViewButtonColumn
            {
                Name = "Approve",
                HeaderText = "",
                Text = "Approve",
                UseColumnTextForButtonValue = true,
                Width = 90
            };
            dataGridView1.Columns.Add(approveCol);

            // Decline button column
            var declineCol = new DataGridViewButtonColumn
            {
                Name = "Decline",
                HeaderText = "",
                Text = "Decline",
                UseColumnTextForButtonValue = true,
                Width = 90
            };
            dataGridView1.Columns.Add(declineCol);

            dataGridView1.ReadOnly = false; // buttons need writable grid; keep text columns read-only via row-level settings below
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;

            // Make non-button columns readonly
            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                if (!(c is DataGridViewButtonColumn))
                    c.ReadOnly = true;
            }
        }

        // Loads pending appointment requests for the specified date and refreshes the grid.
        public void LoadPendingRequests(DateTime selectedDate)
        {
            EnsureGridColumns();
            dataGridView1.Rows.Clear();

            // Read requests and patient names
            var requests = new List<RequestRow>();
            const string reqSql = @"
                SELECT ar.request_id, ar.patient_id, ar.requested_date, ar.requested_time, ar.notes,
                       p.first_name, p.middle_initial, p.last_name
                FROM appointment_requests ar
                LEFT JOIN patients p ON ar.patient_id = p.patient_id
                WHERE CAST(ar.requested_date AS DATE) = @date AND LOWER(ISNULL(ar.status,'')) = 'pending'
                ORDER BY ar.requested_time, ar.requested_date, ar.request_id;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(reqSql, conn))
                {
                    cmd.Parameters.Add("@date", SqlDbType.Date).Value = selectedDate.Date;
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var r = new RequestRow
                            {
                                RequestId = rdr["request_id"] != DBNull.Value ? Convert.ToInt32(rdr["request_id"]) : -1,
                                PatientId = rdr["patient_id"] != DBNull.Value ? Convert.ToInt32(rdr["patient_id"]) : (int?)null,
                                RequestedDate = rdr["requested_date"] != DBNull.Value ? Convert.ToDateTime(rdr["requested_date"]) : DateTime.MinValue,
                                RequestedTime = rdr["requested_time"] != DBNull.Value ? rdr["requested_time"].ToString() : null,
                                Notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty,
                                FirstName = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty,
                                MiddleInitial = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty,
                                LastName = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty
                            };
                            requests.Add(r);
                        }
                    }

                    if (!requests.Any())
                        return;

                    // Load services for these requests
                    var ids = string.Join(",", requests.Select(x => x.RequestId));
                    var svcSql = $@"
                        SELECT ars.request_id, s.name, ISNULL(s.category,'') AS category
                        FROM appointment_requests_services ars
                        INNER JOIN services s ON ars.service_id = s.service_id
                        WHERE ars.request_id IN ({ids})
                        ORDER BY ars.request_id, s.category, s.name;";

                    var servicesByRequest = new Dictionary<int, List<ServiceInfo>>();
                    using (var cmd2 = new SqlCommand(svcSql, conn))
                    using (var rdr2 = cmd2.ExecuteReader())
                    {
                        while (rdr2.Read())
                        {
                            int reqId = rdr2["request_id"] != DBNull.Value ? Convert.ToInt32(rdr2["request_id"]) : -1;
                            var name = rdr2["name"] != DBNull.Value ? rdr2["name"].ToString() : string.Empty;
                            var category = rdr2["category"] != DBNull.Value ? rdr2["category"].ToString() : string.Empty;

                            if (!servicesByRequest.ContainsKey(reqId))
                                servicesByRequest[reqId] = new List<ServiceInfo>();
                            servicesByRequest[reqId].Add(new ServiceInfo { Name = name, Category = category });
                        }
                    }

                    // Populate grid rows
                    foreach (var req in requests)
                    {
                        string fullName;
                        if (!string.IsNullOrEmpty(req.MiddleInitial))
                            fullName = $"{req.FirstName} {req.MiddleInitial} {req.LastName}";
                        else
                            fullName = $"{req.FirstName} {req.LastName}";

                        // format requested time
                        string timeText = string.Empty;
                        if (!string.IsNullOrEmpty(req.RequestedTime))
                        {
                            TimeSpan ts;
                            DateTime dt;
                            if (TimeSpan.TryParse(req.RequestedTime, out ts))
                                timeText = DateTime.Today.Add(ts).ToString("h:mm tt");
                            else if (DateTime.TryParse(req.RequestedTime, out dt))
                                timeText = dt.ToString("h:mm tt");
                            else
                                timeText = req.RequestedTime;
                        }
                        else if (req.RequestedDate != DateTime.MinValue)
                        {
                            timeText = req.RequestedDate.ToString("h:mm tt");
                        }

                        if (servicesByRequest.TryGetValue(req.RequestId, out var svcList) && svcList.Any())
                        {
                            // group by category
                            var groups = svcList.GroupBy(s => s.Category ?? string.Empty).ToList();

                            if (groups.Count == 1)
                            {
                                // same category -> single row with all services
                                var servicesText = string.Join(", ", groups[0].Select(s => s.Name));
                                dataGridView1.Rows.Add(req.RequestId, fullName, timeText, servicesText);
                            }
                            else
                            {
                                // different categories -> create one row per category group
                                foreach (var g in groups)
                                {
                                    var servicesText = string.Join(", ", g.Select(s => s.Name));
                                    dataGridView1.Rows.Add(req.RequestId, fullName, timeText, servicesText);
                                }
                            }
                        }
                        else
                        {
                            // no services found -- still add an empty services row
                            dataGridView1.Rows.Add(req.RequestId, fullName, timeText, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load pending requests: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName != "Approve" && colName != "Decline") return;

            var reqIdObj = dataGridView1.Rows[e.RowIndex].Cells["request_id"].Value;
            if (reqIdObj == null || reqIdObj == DBNull.Value)
            {
                MessageBox.Show("Request id not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int reqId = Convert.ToInt32(reqIdObj);

            if (colName == "Approve")
            {
                // Open Approve_Appointment form populated with request details (do not auto-update DB here)
                try
                {
                    string fullName = dataGridView1.Rows[e.RowIndex].Cells["patient"].Value as string ?? string.Empty;
                    DateTime requestedDate = DateTime.MinValue;
                    string requestedTimeRaw = dataGridView1.Rows[e.RowIndex].Cells["requested_time"].Value as string;
                    string requestedTimeDisplay = string.Empty;

                    // Try to get the real requested_date and time from DB for accuracy
                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand(@"SELECT requested_date, requested_time, p.first_name, p.middle_initial, p.last_name
                                                      FROM appointment_requests ar
                                                      LEFT JOIN patients p ON ar.patient_id = p.patient_id
                                                      WHERE ar.request_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", reqId);
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (rdr["requested_date"] != DBNull.Value)
                                    requestedDate = Convert.ToDateTime(rdr["requested_date"]);
                                if (rdr["requested_time"] != DBNull.Value)
                                    requestedTimeRaw = rdr["requested_time"].ToString();

                                // prefer DB-built full name if present
                                var fn = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                                var mi = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty;
                                var ln = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;
                                if (!string.IsNullOrEmpty(fn) || !string.IsNullOrEmpty(ln))
                                {
                                    fullName = string.IsNullOrEmpty(mi) ? $"{fn} {ln}" : $"{fn} {mi} {ln}";
                                }
                            }
                        }
                    }

                    // format requestedTimeRaw into user-friendly display
                    if (!string.IsNullOrEmpty(requestedTimeRaw))
                    {
                        TimeSpan ts;
                        DateTime dt;
                        if (TimeSpan.TryParse(requestedTimeRaw, out ts))
                            requestedTimeDisplay = DateTime.Today.Add(ts).ToString("h:mm tt");
                        else if (DateTime.TryParse(requestedTimeRaw, out dt))
                            requestedTimeDisplay = dt.ToString("h:mm tt");
                        else
                            requestedTimeDisplay = requestedTimeRaw;
                    }
                    else if (requestedDate != DateTime.MinValue)
                    {
                        requestedTimeDisplay = requestedDate.ToString("h:mm tt");
                    }

                    // load services for this request
                    var services = new List<string>();
                    using (var conn2 = new SqlConnection(connectionString))
                    using (var cmd2 = new SqlCommand(@"SELECT s.name FROM appointment_requests_services ars
                                                      INNER JOIN services s ON ars.service_id = s.service_id
                                                      WHERE ars.request_id = @id
                                                      ORDER BY s.category, s.name", conn2))
                    {
                        cmd2.Parameters.AddWithValue("@id", reqId);
                        conn2.Open();
                        using (var rdr2 = cmd2.ExecuteReader())
                        {
                            while (rdr2.Read())
                            {
                                if (rdr2["name"] != DBNull.Value)
                                    services.Add(rdr2["name"].ToString());
                            }
                        }
                    }

                    // show Approve_Appointment with populated labels
                    using (var approveForm = new Approve_Appointment(reqId, fullName, services, requestedDate, requestedTimeDisplay))
                    {
                        var dr = approveForm.ShowDialog(this);
                        if (dr == DialogResult.OK)
                        {
                            // The request was approved and updated by Approve_Appointment — refresh the grid.
                            LoadPendingRequests(dateTimePicker1.Value.Date);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to open Approve form: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // do NOT auto-update the appointment_requests.status here — leave approval to form action
                return;
            }

            // Decline flow (unchanged)
            var newStatus = "Declined";

            var drConfirm = MessageBox.Show("Are you sure you want to decline this request?", "Confirm Decline", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (drConfirm != DialogResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("UPDATE appointment_requests SET status = @status, notes = ISNULL(notes,'') + @note WHERE request_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@note", " [Declined]");
                    cmd.Parameters.AddWithValue("@id", reqId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                // refresh grid for the currently selected date
                LoadPendingRequests(dateTimePicker1.Value.Date);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update request status: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class RequestRow
        {
            public int RequestId { get; set; }
            public int? PatientId { get; set; }
            public DateTime RequestedDate { get; set; }
            public string RequestedTime { get; set; }
            public string Notes { get; set; }
            public string FirstName { get; set; }
            public string MiddleInitial { get; set; }
            public string LastName { get; set; }
        }

        private class ServiceInfo
        {
            public string Name { get; set; }
            public string Category { get; set; }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.Show();
            this.Hide();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Pending_Appointments pending_Appointments = new Pending_Appointments();
            pending_Appointments.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patients patients = new Patients();
            patients.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Services services = new Services();
            services.Show();
            this.Hide();
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