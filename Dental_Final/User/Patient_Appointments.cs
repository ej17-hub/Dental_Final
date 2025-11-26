using Dental_Final.User;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patient_Appointments : Form
    {
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        // runtime-only label to show patient name (keeps designer label3 as "Date:")
        private Label labelPatientName;

        public Patient_Appointments()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // create a dedicated label for the patient name so label3 remains "Date:"
            labelPatientName = new Label
            {
                Name = "labelPatientName",
                AutoSize = true,
                Font = new Font("Arial", 18F, FontStyle.Bold),
                ForeColor = Color.Black,
                Text = string.Empty
            };

            // position relative to existing date label (designer)
            try
            {
                var pt = label3.Location;
                // place patient name above the date label (adjust offset if you want another position)
                labelPatientName.Location = new Point(pt.X, pt.Y - 36);
            }
            catch
            {
                labelPatientName.Location = new Point(314, 120);
            }

            this.Controls.Add(labelPatientName);

            this.Load -= Patient_Appointments_Load;
            this.Load += Patient_Appointments_Load;

            // reload grid when date changes
            if (this.dateTimePicker1 != null)
            {
                dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
                dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            }
        }

        private void Patient_Appointments_Load(object sender, EventArgs e)
        {
            // keep designer label3 unchanged (it shows "Date:")
            // If caller passed patient id via Tag, try to resolve and show name and load that patient's appointments for the selected date
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
            {
                TrySetPatientName(pid);
                LoadRequestsForPatient(pid); // load appointments filtered by dateTimePicker1
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
            {
                LoadRequestsForPatient(pid);
            }
        }

        private void TrySetPatientName(int patientId)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT first_name, middle_initial, last_name FROM patients WHERE patient_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", patientId);
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (!rdr.Read())
                        {
                            labelPatientName.Text = string.Empty;
                            return;
                        }

                        string first = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                        string mi = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty;
                        string last = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;

                        string full = string.Empty;
                        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
                            full = string.IsNullOrEmpty(mi) ? $"{first} {last}".Trim() : $"{first} {mi} {last}".Trim();

                        labelPatientName.Text = string.IsNullOrWhiteSpace(full) ? string.Empty : full;
                    }
                }
            }
            catch
            {
                labelPatientName.Text = string.Empty;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Add_Appointment_Patient aap = new Add_Appointment_Patient();
            // forward Tag->PatientId if available
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                aap.PatientId = pid;
            aap.Show();
        }

        // Existing API kept for compatibility (if you still call it)
        public void AddPendingAppointments(IEnumerable<ServiceDto> services, DateTime appointmentDateTime)
        {
            // Convert to single-line services string and delegate
            var servicesDisplay = services != null ? string.Join(", ", services.Select(s => s.Name)) : string.Empty;
            AddPendingAppointmentsSingle(servicesDisplay, appointmentDateTime);
        }

        // New: add single pending row with joined services text
        public void AddPendingAppointmentsSingle(string servicesDisplay, DateTime appointmentDateTime)
        {
            EnsureGridColumns();

            string timeText = appointmentDateTime.ToString("h:mm tt");

            int rowIndex = dataGridView1.Rows.Add(
                /* time */ timeText,
                /* service */ servicesDisplay,
                /* status */ "Pending"
            );

            // style the status cell as Pending (white bg)
            var statusCell = dataGridView1.Rows[rowIndex].Cells["status"];
            statusCell.Style.BackColor = Color.White;
            statusCell.Style.ForeColor = Color.Black;
            statusCell.Style.Font = new Font(dataGridView1.Font, FontStyle.Bold);
        }

        // New: load persisted requests for a patient and refresh the grid
        public void LoadRequestsForPatient(int patientId)
        {
            EnsureGridColumns();
            dataGridView1.Rows.Clear();

            // filter by the date picker (use its date part). If dateTimePicker1 is missing, fall back to no date filter.
            bool useDateFilter = this.dateTimePicker1 != null;
            DateTime selectedDate = useDateFilter ? this.dateTimePicker1.Value.Date : DateTime.MinValue;

            string sql = @"
                SELECT ar.request_id, ar.requested_date, ar.requested_time, ar.notes, ar.status,
                    ISNULL((
                        SELECT STUFF((
                            SELECT ', ' + s.name
                            FROM appointment_requests_services ars
                            INNER JOIN services s ON ars.service_id = s.service_id
                            WHERE ars.request_id = ar.request_id
                            FOR XML PATH('')), 1, 2, '')
                    ), '') AS services_list
                FROM appointment_requests ar
                WHERE ar.patient_id = @pid";

            if (useDateFilter)
                sql += " AND CAST(ar.requested_date AS DATE) = @date";

            sql += " ORDER BY ar.requested_date, ar.requested_time;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", patientId);
                    if (useDateFilter)
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = selectedDate;

                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var reqDate = rdr["requested_date"] != DBNull.Value ? Convert.ToDateTime(rdr["requested_date"]) : DateTime.MinValue;
                            var reqTimeObj = rdr["requested_time"];
                            string timeText = string.Empty;
                            if (reqTimeObj != null && reqTimeObj != DBNull.Value)
                            {
                                TimeSpan ts;
                                DateTime dt;
                                var tstr = reqTimeObj.ToString();
                                if (TimeSpan.TryParse(tstr, out ts))
                                    timeText = DateTime.Today.Add(ts).ToString("h:mm tt");
                                else if (DateTime.TryParse(tstr, out dt))
                                    timeText = dt.ToString("h:mm tt");
                                else
                                    timeText = tstr;
                            }
                            else if (reqDate != DateTime.MinValue)
                            {
                                timeText = reqDate.ToString("h:mm tt");
                            }

                            var services = rdr["services_list"] != DBNull.Value ? rdr["services_list"].ToString() : string.Empty;
                            var status = rdr["status"] != DBNull.Value ? rdr["status"].ToString() : "Pending";

                            int row = dataGridView1.Rows.Add(timeText, services, status);

                            var statusCell = dataGridView1.Rows[row].Cells["status"];
                            if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                            {
                                statusCell.Style.BackColor = Color.White;
                                statusCell.Style.ForeColor = Color.Black;
                            }
                            else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                            {
                                statusCell.Style.BackColor = Color.FromArgb(92, 184, 92);
                                statusCell.Style.ForeColor = Color.White;
                            }
                            else if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                            {
                                statusCell.Style.BackColor = Color.FromArgb(217, 83, 79);
                                statusCell.Style.ForeColor = Color.White;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load appointment requests: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureGridColumns()
        {
            // If columns are already created (designer or previous load), just return
            if (dataGridView1.Columns.Count > 0)
                return;

            dataGridView1.Columns.Clear();

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "time",
                HeaderText = "Time",
                Width = 80,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
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

            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            var dr = MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                var login = new Log_in();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open login screen: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Open Settings and pass PatientId via Patient_Settings.PatientId
            Patient_Settings patient_Settings = new Patient_Settings();
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                patient_Settings.PatientId = pid;
            patient_Settings.Show();
            this.Hide();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            // Return to Dashboard, forward Tag so dashboard keeps context
            Patient_Dashboard pd = new Patient_Dashboard();
            if (this.Tag != null)
                pd.Tag = this.Tag;
            pd.Show();
            this.Hide();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            // Open Services and forward Tag (patient id)
            Patient_Services ps = new Patient_Services();
            if (this.Tag != null)
                ps.Tag = this.Tag;
            ps.Show();
            this.Hide();
        }
    }
}