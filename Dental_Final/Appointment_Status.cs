using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Appointment_Status : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

        public Appointment_Status()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            // Add "Today's all Appointments" option if not already present
            if (!comboBox1.Items.Contains("Today's all Appointments"))
            {
                comboBox1.Items.Insert(0, "Today's all Appointments");
            }

            // Set default selection for dropdown
            comboBox1.SelectedIndex = 0; // Default to "Today's all Appointments"

            // Wire up event handlers
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

            // Initial setup and load
            SetupDataGridViewCompleteCancelled();
            LoadAppointmentsByStatus();
        }

        private void SetupDataGridViewCompleteCancelled()
        {
            dataGridViewCompleteCancelled.AllowUserToAddRows = false;
            dataGridViewCompleteCancelled.ReadOnly = true;
            dataGridViewCompleteCancelled.RowHeadersVisible = false;
            dataGridViewCompleteCancelled.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCompleteCancelled.BackgroundColor = Color.White;

            // Apply bold header style
            dataGridViewCompleteCancelled.EnableHeadersVisualStyles = false;
            dataGridViewCompleteCancelled.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridViewCompleteCancelled.Font, FontStyle.Bold);

            // Clear and create columns
            dataGridViewCompleteCancelled.Columns.Clear();
            dataGridViewCompleteCancelled.Columns.Add("appointment_id", "Id");
            dataGridViewCompleteCancelled.Columns["appointment_id"].Visible = false;
            dataGridViewCompleteCancelled.Columns.Add("Patient", "Patient");
            dataGridViewCompleteCancelled.Columns.Add("Service", "Service");
            dataGridViewCompleteCancelled.Columns.Add("Dentist", "Dentist");
            dataGridViewCompleteCancelled.Columns.Add("Time", "Time");
            dataGridViewCompleteCancelled.Columns.Add("Price", "Price");

            // Add Status column as text column (not button)
            DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                Width = 120
            };
            dataGridViewCompleteCancelled.Columns.Add(statusColumn);

            // Handle cell formatting for colored status text
            dataGridViewCompleteCancelled.CellFormatting += DataGridViewCompleteCancelled_CellFormatting;
        }

        private void DataGridViewCompleteCancelled_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridViewCompleteCancelled.Columns[e.ColumnIndex].Name == "Status" && e.RowIndex >= 0)
            {
                var statusCell = dataGridViewCompleteCancelled.Rows[e.RowIndex].Cells["Status"];
                if (statusCell.Value != null)
                {
                    string status = statusCell.Value.ToString();

                    if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.BackColor = Color.FromArgb(92, 184, 92); // Green
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.Font = new Font(dataGridViewCompleteCancelled.Font, FontStyle.Bold);
                    }
                    else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.BackColor = Color.FromArgb(217, 83, 79); // Red
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.Font = new Font(dataGridViewCompleteCancelled.Font, FontStyle.Bold);
                    }
                }
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LoadAppointmentsByStatus();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetupDataGridViewCompleteCancelled();
            LoadAppointmentsByStatus();
        }

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

        private void LoadAppointmentsByStatus()
        {
            dataGridViewCompleteCancelled.Rows.Clear();

            if (comboBox1.SelectedIndex < 0)
                return;

            // Determine which status to filter
            string selectedStatus = comboBox1.SelectedItem.ToString();
            string statusFilter = "";
            bool showStatusColumn = false;

            if (selectedStatus.Contains("Completed"))
            {
                statusFilter = "Completed";
                showStatusColumn = false;
            }
            else if (selectedStatus.Contains("Cancelled"))
            {
                statusFilter = "Cancelled";
                showStatusColumn = false;
            }
            else if (selectedStatus.Contains("Today's all Appointments"))
            {
                statusFilter = "All"; // Show all appointments (completed and cancelled)
                showStatusColumn = true;
            }

            // Show or hide Status column based on selection
            if (dataGridViewCompleteCancelled.Columns["Status"] != null)
            {
                dataGridViewCompleteCancelled.Columns["Status"].Visible = showStatusColumn;
            }

            DateTime selectedDate = dateTimePicker1.Value.Date;

            // Build dynamic name expressions
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

            bool hasPrice = ColumnExists("appointments", "price");

            // Query for appointments
            string sql =
                "SELECT a.appointment_id, a.appointment_date, a.appointment_time" +
                (hasPrice ? ", a.price" : "") + ", ISNULL(a.notes,'') AS notes, " +
                patientExpr + " AS patient_name, " +
                dentistExpr + " AS dentist_name, " +
                serviceExpr + " AS service_name, " +
                "ISNULL((SELECT STUFF((" +
                    "SELECT ', ' + ISNULL(s2.name, CONVERT(varchar(20), aps2.service_id)) " +
                    "FROM appointments_services aps2 " +
                    "LEFT JOIN services s2 ON aps2.service_id = s2.service_id " +
                    "WHERE aps2.appointment_id = a.appointment_id " +
                    "FOR XML PATH('')), 1, 2, '')), '') AS services_list " +
                "FROM appointments a " +
                "LEFT JOIN patients p ON a.patient_id = p.patient_id " +
                "LEFT JOIN dentists d ON a.dentist_id = d.dentist_id " +
                "LEFT JOIN services s ON a.service_id = s.service_id " +
                "WHERE CAST(a.appointment_date AS DATE) = @date " +
                "ORDER BY a.appointment_time";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@date", SqlDbType.Date).Value = selectedDate;
                    conn.Open();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var id = rdr["appointment_id"] != DBNull.Value ? Convert.ToInt32(rdr["appointment_id"]) : -1;
                            var notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;

                            // Check if appointment matches the selected status
                            bool isCancelled = notes.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool isCompleted = notes.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0;

                            string currentStatus = "";
                            if (isCancelled)
                                currentStatus = "Cancelled";
                            else if (isCompleted)
                                currentStatus = "Completed";
                            else
                                continue; // Skip appointments that are neither completed nor cancelled

                            // Filter based on dropdown selection
                            if (statusFilter != "All" && currentStatus != statusFilter)
                                continue;

                            // Read appointment time
                            object apptTimeObj = rdr["appointment_time"];
                            string timeText = string.Empty;
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

                            // Read price
                            decimal? price = null;
                            if (hasPrice && rdr["price"] != DBNull.Value)
                                price = Convert.ToDecimal(rdr["price"]);
                            var priceText = price.HasValue ? "₱" + price.Value.ToString("N2") : string.Empty;

                            var patient = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString() : string.Empty;
                            var dentist = rdr["dentist_name"] != DBNull.Value ? rdr["dentist_name"].ToString() : string.Empty;
                            var singleService = rdr["service_name"] != DBNull.Value ? rdr["service_name"].ToString() : string.Empty;
                            var servicesAgg = rdr["services_list"] != DBNull.Value ? rdr["services_list"].ToString() : string.Empty;

                            // Prefer aggregated services
                            string serviceDisplay = !string.IsNullOrEmpty(servicesAgg) ? servicesAgg : singleService;

                            // Add row to grid
                            int r = dataGridViewCompleteCancelled.Rows.Add(id, patient, serviceDisplay, dentist, timeText, priceText, currentStatus);
                            dataGridViewCompleteCancelled.Rows[r].Tag = id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Appointments appointmentsForm = new Appointments();
            appointmentsForm.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Dashboard dashboardForm = new Dashboard();
            dashboardForm.Show();
            this.Hide();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            Patients patientsForm = new Patients();
            patientsForm.Show();
            this.Hide();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Staff staff_Form = new Staff();
            staff_Form.Show();
            this.Hide();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            Services servicesForm = new Services();
            servicesForm.Show();
            this.Hide();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            Adding_Patient adding_PatientForm = new Adding_Patient();
            adding_PatientForm.Show();
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            Add_Appointment add_AppointmentForm = new Add_Appointment();
            add_AppointmentForm.Show();
        }
    }
}