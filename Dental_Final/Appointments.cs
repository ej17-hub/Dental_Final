
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Appointments : Form
    {
        private readonly string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

        public Appointments()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            // configure the designer DataGridViews (columns, buttons, handlers)
            SetupDesignerGrids();

            // initial render using dateTimePicker1 from designer
            RenderGrids(dateTimePicker1.Value.Date);

            // wire date change to reload
            dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            RenderGrids(dateTimePicker1.Value.Date);
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

        // Configure dataGridView1 (Today) / dataGridView2 (Completed) / dataGridView3 (Cancelled)
        private void SetupDesignerGrids()
        {
            // --- Today grid (dataGridView1) ---
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = Color.White;

            // clear previous columns and recreate stable schema
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("appointment_id", "Id");
            dataGridView1.Columns["appointment_id"].Visible = false;
            dataGridView1.Columns.Add("Patient", "Patient");
            dataGridView1.Columns.Add("Service", "Service");
            dataGridView1.Columns.Add("Time", "Time");
            dataGridView1.Columns.Add("Dentist", "Dentist");

            // action columns
            if (!dataGridView1.Columns.Contains("Edit"))
            {
                var editCol = new DataGridViewButtonColumn { Name = "Edit", Text = "Edit", UseColumnTextForButtonValue = true, Width = 80 };
                dataGridView1.Columns.Add(editCol);
            }
            if (!dataGridView1.Columns.Contains("Complete"))
            {
                var compCol = new DataGridViewButtonColumn { Name = "Complete", Text = "Complete", UseColumnTextForButtonValue = true, Width = 90 };
                dataGridView1.Columns.Add(compCol);
            }
            if (!dataGridView1.Columns.Contains("Cancel"))
            {
                var cancelCol = new DataGridViewButtonColumn { Name = "Cancel", Text = "Cancel", UseColumnTextForButtonValue = true, Width = 80 };
                dataGridView1.Columns.Add(cancelCol);
            }

            // ensure handler
            dataGridView1.CellContentClick -= DgvToday_CellContentClick;
            dataGridView1.CellContentClick += DgvToday_CellContentClick;

            // --- Completed grid (dataGridView2) ---
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.ReadOnly = true;
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView2.BackgroundColor = Color.White;

            dataGridView2.Columns.Clear();
            dataGridView2.Columns.Add("appointment_id", "Id");
            dataGridView2.Columns["appointment_id"].Visible = false;
            dataGridView2.Columns.Add("Patient", "Patient");
            dataGridView2.Columns.Add("Service", "Service");
            dataGridView2.Columns.Add("Time", "Time");
            dataGridView2.Columns.Add("Dentist", "Dentist");

            // --- Cancelled grid (dataGridView3) ---
            dataGridView3.AllowUserToAddRows = false;
            dataGridView3.ReadOnly = true;
            dataGridView3.RowHeadersVisible = false;
            dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView3.BackgroundColor = Color.White;

            dataGridView3.Columns.Clear();
            dataGridView3.Columns.Add("appointment_id", "Id");
            dataGridView3.Columns["appointment_id"].Visible = false;
            dataGridView3.Columns.Add("Patient", "Patient");
            dataGridView3.Columns.Add("Service", "Service");
            dataGridView3.Columns.Add("Time", "Time");
            dataGridView3.Columns.Add("Dentist", "Dentist");
        }

        private void RenderGrids(DateTime selectedDate)
        {
            dataGridView1.Rows.Clear();
            dataGridView2.Rows.Clear();
            dataGridView3.Rows.Clear();

            // Build safe name expressions depending on which columns exist.
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

            string staffExpr1;
            if (ColumnExists("staff", "first_name") && ColumnExists("staff", "last_name"))
                staffExpr1 = "RTRIM(ISNULL(st1.first_name,'')) + ' ' + RTRIM(ISNULL(st1.last_name,''))";
            else if (ColumnExists("staff", "name"))
                staffExpr1 = "RTRIM(ISNULL(st1.name,''))";
            else
                staffExpr1 = "CONVERT(varchar(20), st1.staff_id)";

            string staffExpr2;
            if (ColumnExists("staff", "first_name") && ColumnExists("staff", "last_name"))
                staffExpr2 = "RTRIM(ISNULL(st2.first_name,'')) + ' ' + RTRIM(ISNULL(st2.last_name,''))";
            else if (ColumnExists("staff", "name"))
                staffExpr2 = "RTRIM(ISNULL(st2.name,''))";
            else
                staffExpr2 = "CONVERT(varchar(20), st2.staff_id)";

            // Services table: assume service.name exists; if not fallback to id
            string serviceExpr;
            if (ColumnExists("services", "name"))
                serviceExpr = "s.name";
            else
                serviceExpr = "CONVERT(varchar(20), s.service_id)";

            string sql =
                "SELECT a.appointment_id, a.appointment_date, a.appointment_time, ISNULL(a.notes,'') AS notes, " +
                "p.patient_id, " + patientExpr + " AS patient_name, " +
                "d.dentist_id, " + dentistExpr + " AS dentist_name, " +
                "s.service_id, " + serviceExpr + " AS service_name, " +
                "st1.staff_id AS staff1_id, " + staffExpr1 + " AS staff1_name, " +
                "st2.staff_id AS staff2_id, " + staffExpr2 + " AS staff2_name " +
                "FROM appointments a " +
                "LEFT JOIN patients p ON a.patient_id = p.patient_id " +
                "LEFT JOIN dentists d ON a.dentist_id = d.dentist_id " +
                "LEFT JOIN services s ON a.service_id = s.service_id " +
                "LEFT JOIN staff st1 ON a.staff_assign_1 = st1.staff_id " +
                "LEFT JOIN staff st2 ON a.staff_assign_2 = st2.staff_id " +
                "WHERE (CAST(a.appointment_date AS DATE) = @date) OR (CAST(a.appointment_date AS DATE) < @date) OR (LOWER(ISNULL(a.notes,'')) LIKE '%cancel%') " +
                "ORDER BY a.appointment_date, a.appointment_time";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@date", SqlDbType.Date).Value = selectedDate.Date;
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var id = rdr["appointment_id"] != DBNull.Value ? Convert.ToInt32(rdr["appointment_id"]) : -1;
                            var apptDate = rdr["appointment_date"] != DBNull.Value ? Convert.ToDateTime(rdr["appointment_date"]) : DateTime.MinValue;
                            var apptTime = rdr["appointment_time"] != DBNull.Value ? rdr["appointment_time"].ToString() : string.Empty;
                            var patient = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString() : string.Empty;
                            var dentist = rdr["dentist_name"] != DBNull.Value ? rdr["dentist_name"].ToString() : string.Empty;
                            var service = rdr["service_name"] != DBNull.Value ? rdr["service_name"].ToString() : string.Empty;
                            var notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;

                            var timeText = !string.IsNullOrEmpty(apptTime) ? apptTime : (apptDate != DateTime.MinValue ? apptDate.ToString("h:mm tt") : string.Empty);

                            // classify: cancelled if notes mention cancel, today if date == selectedDate, completed if date < selectedDate
                            var isCancelled = notes.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (isCancelled)
                            {
                                int r = dataGridView3.Rows.Add(id, patient, service, timeText, dentist);
                                dataGridView3.Rows[r].Tag = id;
                            }
                            else if (apptDate.Date == selectedDate.Date)
                            {
                                int r = dataGridView1.Rows.Add(id, patient, service, timeText, dentist);
                                dataGridView1.Rows[r].Tag = id;
                            }
                            else if (apptDate.Date < selectedDate.Date)
                            {
                                int r = dataGridView2.Rows.Add(id, patient, service, timeText, dentist);
                                dataGridView2.Rows[r].Tag = id;
                            }
                            else
                            {
                                // future appointments beyond selected date — place in Completed as fallback
                                int r = dataGridView2.Rows.Add(id, patient, service, timeText, dentist);
                                dataGridView2.Rows[r].Tag = id;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvToday_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            var colName = grid.Columns[e.ColumnIndex].Name;
            var idObj = grid.Rows[e.RowIndex].Cells["appointment_id"].Value;
            if (idObj == null) return;

            int apptId = Convert.ToInt32(idObj);

            if (colName == "Edit")
            {
                // Open Add_Appointment in edit mode — you can extend Add_Appointment to accept an id and load data
                MessageBox.Show($"Open edit for appointment #{apptId}", "Edit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (colName == "Complete")
            {
                UpdateAppointmentNotes(apptId, "Completed");
                RenderGrids(dateTimePicker1.Value.Date);
            }
            else if (colName == "Cancel")
            {
                var result = MessageBox.Show(
                "Are you sure you want to cancel?",
                "Confirm Cancel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    UpdateAppointmentNotes(apptId, "Cancelled");
                    RenderGrids(dateTimePicker1.Value.Date);
                }
                
            }
        }

        // lightweight helper to mark appointment by writing into notes (or implement proper status column if you add one)
        private void UpdateAppointmentNotes(int appointmentId, string marker)
        {
            const string updateSql = "UPDATE appointments SET notes = ISNULL(notes,'') + @marker WHERE appointment_id = @id";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@marker", " [" + marker + "]");
                    cmd.Parameters.AddWithValue("@id", appointmentId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            using (var addApptForm = new Add_Appointment())
            {
                if (addApptForm.ShowDialog() == DialogResult.OK)
                {
                    // refresh only when Add_Appointment saved successfully
                    RenderGrids(dateTimePicker1.Value.Date);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }
    }
}