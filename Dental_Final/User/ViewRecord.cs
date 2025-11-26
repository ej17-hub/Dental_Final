using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class ViewRecord : Form
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;
        private int _patientId = -1;

        public ViewRecord()
        {
            InitializeComponent();

            // Basic grid styling
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
        }

        public ViewRecord(int patientId) : this()
        {
            _patientId = patientId;
            LoadPatientRecord(patientId);
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

        private string CleanNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return string.Empty;

            // Remove status markers like [Completed], [Cancelled], [Pending]
            string cleaned = Regex.Replace(notes, @"\s*\[(Completed|Cancelled|Pending)\]\s*", "", RegexOptions.IgnoreCase).Trim();
            return cleaned;
        }

        private void LoadPatientRecord(int patientId)
        {
            try
            {
                LoadPatientDetails(patientId);
                LoadAppointmentHistory(patientId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient record: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPatientDetails(int patientId)
        {
            const string sql = @"SELECT first_name, last_name, middle_initial, suffix, birth_date, gender, address, phone, email
                                 FROM patients WHERE patient_id = @id";
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", patientId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        string first = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                        string last = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;
                        string mi = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty;
                        string suffix = rdr["suffix"] != DBNull.Value ? rdr["suffix"].ToString().Trim() : string.Empty;

                        // Use the same robust formatter as Add_Appointment_Patient to avoid swapped fields and to format middle initial
                        string displayName = NameFormatter.FormatPatientDisplayName(first, mi, last);
                        if (!string.IsNullOrEmpty(suffix))
                            displayName = (displayName + " " + suffix).Trim();

                        label3.Text = "Name: " + displayName;

                        if (rdr["birth_date"] != DBNull.Value && DateTime.TryParse(rdr["birth_date"].ToString(), out DateTime bd))
                        {
                            int age = DateTime.Now.Year - bd.Year;
                            if (DateTime.Now.DayOfYear < bd.DayOfYear) age--;
                            label6.Text = "Age: " + age;
                            label8.Text = "Birthdate: " + bd.ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            label6.Text = "Age: N/A";
                            label8.Text = "Birthdate: N/A";
                        }

                        label7.Text = "Gender: " + (rdr["gender"] != DBNull.Value ? rdr["gender"].ToString() : "N/A");
                        label9.Text = "Address: " + (rdr["address"] != DBNull.Value ? rdr["address"].ToString() : "N/A");
                        label10.Text = "Contact Number: " + (rdr["phone"] != DBNull.Value ? rdr["phone"].ToString() : "N/A");
                        label11.Text = "Email Address: " + (rdr["email"] != DBNull.Value ? rdr["email"].ToString() : "N/A");
                    }
                }
            }
        }

        private void LoadAppointmentHistory(int patientId)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "DATE", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Treatment", HeaderText = "TREATMENT", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "PRICE BILLED", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });

            bool hasPrice = ColumnExists("appointments", "price");
            bool hasTooth = ColumnExists("appointments", "tooth_number");
            bool hasAppsServices = TableExists("appointments_services");
            bool hasStatusColumn = ColumnExists("appointments", "status");

            string serviceExpr = hasAppsServices
                ? "ISNULL((SELECT STUFF((SELECT ', ' + ISNULL(s2.name, CONVERT(varchar(20), aps2.service_id)) FROM appointments_services aps2 LEFT JOIN services s2 ON aps2.service_id = s2.service_id WHERE aps2.appointment_id = a.appointment_id FOR XML PATH('')),1,2,'')), ISNULL(s.name, ''))"
                : "ISNULL(s.name, CONVERT(varchar(20), a.service_id))";

            string toothExpr = hasTooth ? "ISNULL(a.tooth_number, '')" : "ISNULL(a.notes, '')";
            string priceExpr = hasPrice ? "a.price" : "NULL";

            // Build status select and filter: prefer explicit status column, else fall back to notes
            string statusSelect;
            string filterClause;
            if (hasStatusColumn)
            {
                // Normalize status values to 'Completed' / 'Cancelled' for display and filter strictly
                statusSelect = "CASE WHEN UPPER(ISNULL(a.status,'')) = 'COMPLETED' THEN 'Completed' WHEN UPPER(ISNULL(a.status,'')) = 'CANCELLED' THEN 'Cancelled' ELSE '' END AS status";
                filterClause = "AND UPPER(ISNULL(a.status,'')) IN ('COMPLETED','CANCELLED')";
            }
            else
            {
                // Fallback: detect from notes (case-insensitive)
                statusSelect = "CASE WHEN UPPER(ISNULL(a.notes,'')) LIKE '%COMPLETED%' THEN 'Completed' WHEN UPPER(ISNULL(a.notes,'')) LIKE '%CANCELLED%' THEN 'Cancelled' ELSE '' END AS status";
                filterClause = "AND (UPPER(ISNULL(a.notes,'')) LIKE '%COMPLETED%' OR UPPER(ISNULL(a.notes,'')) LIKE '%CANCELLED%')";
            }

            string sql = $@"
SELECT a.appointment_id,
       CONVERT(varchar(10), a.appointment_date, 101) AS appointment_date,
       {serviceExpr} AS treatment,
       {toothExpr} AS tooth_number,
       {priceExpr} AS price_billed,
       a.notes AS original_notes,
       {statusSelect}
FROM appointments a
LEFT JOIN services s ON a.service_id = s.service_id
WHERE a.patient_id = @PatientId
  {filterClause}
ORDER BY a.appointment_date DESC;";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string date = rdr["appointment_date"] != DBNull.Value ? rdr["appointment_date"].ToString() : string.Empty;
                        string treatment = rdr["treatment"] != DBNull.Value ? rdr["treatment"].ToString() : string.Empty;
                        string tooth = rdr["tooth_number"] != DBNull.Value ? rdr["tooth_number"].ToString() : string.Empty;
                        decimal? price = rdr["price_billed"] != DBNull.Value ? (decimal?)Convert.ToDecimal(rdr["price_billed"]) : null;
                        string status = rdr["status"] != DBNull.Value ? rdr["status"].ToString() : string.Empty;
                        string originalNotes = rdr["original_notes"] != DBNull.Value ? rdr["original_notes"].ToString() : string.Empty;

                        // Only Completed/Cancelled rows should reach here due to filterClause,
                        // but guard against unexpected values by skipping empty statuses.
                        if (string.IsNullOrWhiteSpace(status))
                            continue;

                        // Clean notes by removing status markers
                        string cleanedNotes = CleanNotes(originalNotes);

                        int r = dataGridView1.Rows.Add(
                            date,
                            treatment,
                            cleanedNotes,
                            price.HasValue ? "₱" + price.Value.ToString("N2") : "N/A",
                            status
                        );

                        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                            dataGridView1.Rows[r].Cells["Status"].Style.ForeColor = Color.Green;
                        else if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                            dataGridView1.Rows[r].Cells["Status"].Style.ForeColor = Color.Red;
                        else
                            dataGridView1.Rows[r].Cells["Status"].Style.ForeColor = Color.Orange;
                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Handle row-level interactions inside the ViewRecord form if needed
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void ViewRecord_Load(object sender, EventArgs e)
        {

        }
    }
}
