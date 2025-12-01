using Dental_Final.Admin;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Dental_Final
{
    public partial class Appointments : Form
    {
        string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Appointments()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            // configure the designer DataGridViews (columns, buttons, handlers)
            SetupDesignerGrids();

            // setup status combobox (Today's Active, Today's all, Completed, Cancelled)
            if (comboBoxStatus != null)
            {
                comboBoxStatus.Items.Clear();
                comboBoxStatus.Items.Add("Active");
                comboBoxStatus.Items.Add("All");
                comboBoxStatus.Items.Add("Completed");
                comboBoxStatus.Items.Add("Cancelled");
                // make "All" the default selection
                comboBoxStatus.SelectedItem = "All";
                comboBoxStatus.SelectedIndexChanged -= ComboBoxStatus_SelectedIndexChanged;
                comboBoxStatus.SelectedIndexChanged += ComboBoxStatus_SelectedIndexChanged;
            }

            // initial render using dateTimePicker1 from designer
            RenderGrids(dateTimePicker1.Value.Date);

            // wire date change to reload
            dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
        }

        // New: constructor overload that sets initial combobox value
        public Appointments(string initialStatus) : this()
        {
            SetStatus(initialStatus);
        }

        // New: public method to set combobox selection safely from other forms
        public void SetStatus(string status)
        {
            if (comboBoxStatus == null)
                return;

            // prefer existing items, add if missing
            if (status == null)
                return;

            var match = comboBoxStatus.Items.Cast<object>().FirstOrDefault(i => string.Equals(i?.ToString(), status, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                comboBoxStatus.SelectedItem = match;
            }
            else
            {
                comboBoxStatus.Items.Add(status);
                comboBoxStatus.SelectedItem = status;
            }

            // force refresh of the grid after changing selection
            RenderGrids(dateTimePicker1.Value.Date);
        }

        private void ComboBoxStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            RenderGrids(dateTimePicker1.Value.Date);
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

        // Configure dataGridView1 (Today)
        private void SetupDesignerGrids()
        {
            bool hasPriceColumn = ColumnExists("appointments", "price");

            // --- Today grid (dataGridView1) ---
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = Color.White;

            // apply bold header style
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

            // clear previous columns and recreate stable schema
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("appointment_id", "Id");
            dataGridView1.Columns["appointment_id"].Visible = false;
            dataGridView1.Columns.Add("Patient", "Patient");
            dataGridView1.Columns.Add("Service", "Service");
            dataGridView1.Columns.Add("Time", "Time");
            dataGridView1.Columns.Add("Dentist", "Dentist");

            // add Price column (before action buttons) only if appointments.price exists
            if (hasPriceColumn)
            {
                dataGridView1.Columns.Add("Price", "Price");
                // make sure Price column doesn't auto-expand too much
                dataGridView1.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            // Add Status column as text column (not button) so we can show Completed/Cancelled when requested
            if (!dataGridView1.Columns.Contains("Status"))
            {
                DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Status",
                    Width = 120
                };
                dataGridView1.Columns.Add(statusColumn);
            }

            // action columns - create as per-cell buttons (UseColumnTextForButtonValue = false)
            if (!dataGridView1.Columns.Contains("Edit"))
            {
                var editCol = new DataGridViewButtonColumn { Name = "Edit", UseColumnTextForButtonValue = false, Width = 80 };
                dataGridView1.Columns.Add(editCol);
            }
            if (!dataGridView1.Columns.Contains("Complete"))
            {
                var compCol = new DataGridViewButtonColumn { Name = "Complete", UseColumnTextForButtonValue = false, Width = 90 };
                dataGridView1.Columns.Add(compCol);
            }
            if (!dataGridView1.Columns.Contains("Cancel"))
            {
                var cancelCol = new DataGridViewButtonColumn { Name = "Cancel", UseColumnTextForButtonValue = false, Width = 80 };
                dataGridView1.Columns.Add(cancelCol);
            }

            // ensure handlers
            dataGridView1.CellContentClick -= DgvToday_CellContentClick;
            dataGridView1.CellContentClick += DgvToday_CellContentClick;

            dataGridView1.CellFormatting -= DataGridView1_CellFormatting;
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Status" && e.RowIndex >= 0)
            {
                var statusCell = dataGridView1.Rows[e.RowIndex].Cells["Status"];
                if (statusCell.Value != null)
                {
                    string status = statusCell.Value.ToString();

                    if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.BackColor = Color.FromArgb(92, 184, 92); // Green
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                    }
                    else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.BackColor = Color.FromArgb(217, 83, 79); // Red
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                    }
                    else if (status.Equals("Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(status))
                    {
                        e.CellStyle.BackColor = Color.White;
                        e.CellStyle.ForeColor = Color.Black;
                        e.CellStyle.Font = new Font(dataGridView1.Font, FontStyle.Regular);
                    }
                }
            }
        }

        private void RenderGrids(DateTime selectedDate)
        {
            dataGridView1.Rows.Clear();

            // Determine desired filter from combo box once so we can add/remove action columns accordingly
            string selectedStatus = comboBoxStatus != null && comboBoxStatus.SelectedItem != null
                ? comboBoxStatus.SelectedItem.ToString()
                : "All";

            // If user is viewing Completed or Cancelled lists, remove the action button columns entirely.
            // Otherwise ensure they exist so actions are available for Active/All views.
            if (selectedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                selectedStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                if (dataGridView1.Columns.Contains("Edit"))
                    dataGridView1.Columns.Remove("Edit");
                if (dataGridView1.Columns.Contains("Complete"))
                    dataGridView1.Columns.Remove("Complete");
                if (dataGridView1.Columns.Contains("Cancel"))
                    dataGridView1.Columns.Remove("Cancel");
            }
            else
            {
                if (!dataGridView1.Columns.Contains("Edit"))
                {
                    var editCol = new DataGridViewButtonColumn { Name = "Edit", UseColumnTextForButtonValue = false, Width = 80 };
                    dataGridView1.Columns.Add(editCol);
                }
                if (!dataGridView1.Columns.Contains("Complete"))
                {
                    var compCol = new DataGridViewButtonColumn { Name = "Complete", UseColumnTextForButtonValue = false, Width = 90 };
                    dataGridView1.Columns.Add(compCol);
                }
                if (!dataGridView1.Columns.Contains("Cancel"))
                {
                    var cancelCol = new DataGridViewButtonColumn { Name = "Cancel", UseColumnTextForButtonValue = false, Width = 80 };
                    dataGridView1.Columns.Add(cancelCol);
                }
            }

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

            // include price column in SELECT only if it exists
            bool hasPrice = ColumnExists("appointments", "price");

            // Aggregate services via appointments_services (if present) using FOR XML PATH (compatible with older SQL Server)
            string sql =
                "SELECT a.appointment_id, a.appointment_date, a.appointment_time" +
                (hasPrice ? ", a.price" : "") + ", ISNULL(a.notes,'') AS notes, " +
                "p.patient_id, " + patientExpr + " AS patient_name, " +
                "d.dentist_id, " + dentistExpr + " AS dentist_name, " +
                "s.service_id, " + serviceExpr + " AS service_name, " +
                "st1.staff_id AS staff1_id, " + staffExpr1 + " AS staff1_name, " +
                "st2.staff_id AS staff2_id, " + staffExpr2 + " AS staff2_name, " +
                // subquery returns aggregated service names if linking table exists
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
                "LEFT JOIN staff st1 ON a.staff_assign_1 = st1.staff_id " +
                "LEFT JOIN staff st2 ON a.staff_assign_2 = st2.staff_id " +
                "WHERE (CAST(a.appointment_date AS DATE) = @date) " +
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

                            // read raw appointment_time (could be TIME, DATETIME or string)
                            object apptTimeObj = rdr["appointment_time"];

                            // Format price if present
                            decimal? price = null;
                            if (hasPrice && rdr["price"] != DBNull.Value)
                                price = Convert.ToDecimal(rdr["price"]);
                            var priceText = price.HasValue ? "₱" + price.Value.ToString("N2") : string.Empty;

                            var patient = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString() : string.Empty;
                            var dentist = rdr["dentist_name"] != DBNull.Value ? rdr["dentist_name"].ToString() : string.Empty;
                            var singleService = rdr["service_name"] != DBNull.Value ? rdr["service_name"].ToString() : string.Empty;
                            var servicesAgg = rdr["services_list"] != DBNull.Value ? rdr["services_list"].ToString() : string.Empty;
                            var notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;

                            // prefer aggregated service list when present
                            string serviceDisplay = !string.IsNullOrEmpty(servicesAgg) ? servicesAgg : singleService;

                            // still handle legacy "Services:" text in notes if required (fallback)
                            if (string.IsNullOrEmpty(serviceDisplay))
                            {
                                var servicesMarkerIndex = notes.IndexOf("Services:", StringComparison.OrdinalIgnoreCase);
                                if (servicesMarkerIndex >= 0)
                                {
                                    const string marker = "Services:";
                                    int markerStart = notes.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                                    string after = notes.Substring(markerStart + marker.Length).Trim();
                                    int statusMarker = after.IndexOf(" [", StringComparison.Ordinal);
                                    if (statusMarker >= 0) after = after.Substring(0, statusMarker).Trim();
                                    serviceDisplay = after.Trim(new char[] { ':', '-', ' ', '\t' });
                                }
                            }

                            // Convert appointment_time to user-friendly 12-hour format (e.g. "4:00 PM")
                            string timeText = string.Empty;
                            if (apptTimeObj != null && apptTimeObj != DBNull.Value)
                            {
                                var apptTimeStr = apptTimeObj.ToString();

                                // Try parse as TimeSpan first (SQL TIME -> "HH:mm:ss")
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
                                    // fallback: show raw string
                                    timeText = apptTimeStr;
                                }
                            }
                            else if (apptDate != DateTime.MinValue)
                            {
                                // when time column is absent use appointment_date time part if available
                                timeText = apptDate.ToString("h:mm tt");
                            }

                            // Determine status markers from notes
                            var isCancelled = notes.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
                            var isCompleted = notes.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0;
                            string currentStatus = isCancelled ? "Cancelled" : (isCompleted ? "Completed" : "Active");

                            bool include = false;
                            // Accept both "All" and legacy "Today's all Appointments"
                            if (selectedStatus.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                                selectedStatus.Equals("Today's all Appointments", StringComparison.OrdinalIgnoreCase))
                            {
                                // include all statuses
                                include = true;
                            }
                            else if (selectedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            {
                                include = isCompleted;
                            }
                            else if (selectedStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            {
                                include = isCancelled;
                            }
                            else // "Active" or default
                            {
                                include = !isCancelled && !isCompleted;
                            }

                            if (!include || apptDate.Date != selectedDate.Date)
                                continue;

                            // include price column when present (dataGridView1 schema already created in SetupDesignerGrids)
                            int r;
                            if (hasPrice)
                                r = dataGridView1.Rows.Add(id, patient, serviceDisplay, timeText, dentist, priceText, currentStatus);
                            else
                                r = dataGridView1.Rows.Add(id, patient, serviceDisplay, timeText, dentist, currentStatus);

                            // store id on row
                            dataGridView1.Rows[r].Tag = id;

                            // Set button cell values per-row. If appointment is Completed or Cancelled, remove action labels and disable clicks.
                            bool isFinal = currentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                           currentStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);

                            // If columns exist, set per-cell text (we created button columns with per-cell values)
                            try
                            {
                                if (dataGridView1.Columns.Contains("Edit"))
                                {
                                    var cell = dataGridView1.Rows[r].Cells["Edit"];
                                    cell.Value = isFinal ? string.Empty : "Edit";
                                    cell.ReadOnly = isFinal;
                                    cell.Style.ForeColor = isFinal ? Color.Gray : Color.Black;
                                }
                                if (dataGridView1.Columns.Contains("Complete"))
                                {
                                    var cell = dataGridView1.Rows[r].Cells["Complete"];
                                    cell.Value = isFinal ? string.Empty : "Complete";
                                    cell.ReadOnly = isFinal;
                                    cell.Style.ForeColor = isFinal ? Color.Gray : Color.Black;
                                }
                                if (dataGridView1.Columns.Contains("Cancel"))
                                {
                                    var cell = dataGridView1.Rows[r].Cells["Cancel"];
                                    cell.Value = isFinal ? string.Empty : "Cancel";
                                    cell.ReadOnly = isFinal;
                                    cell.Style.ForeColor = isFinal ? Color.Gray : Color.Black;
                                }
                            }
                            catch
                            {
                                // ignore per-row button setup errors
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

            // prevent actions on final statuses
            var statusCell = grid.Rows[e.RowIndex].Cells["Status"];
            if (statusCell != null && statusCell.Value != null)
            {
                var st = statusCell.Value.ToString();
                if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    // no actions allowed
                    return;
                }
            }

            var idObj = grid.Rows[e.RowIndex].Cells["appointment_id"].Value;
            if (idObj == null) return;

            int apptId = Convert.ToInt32(idObj);

            if (colName == "Edit")
            {
                // Open Edit_Appointment passing appointment id. Edit_Appointment constructor will load data.
                try
                {
                    using (var editForm = new Edit_Appointment(apptId))
                    {
                        // ensure the form has the appointment id available for its save logic
                        editForm.Tag = apptId;

                        // show modal so user must finish editing before returning
                        var dr = editForm.ShowDialog(this);
                        if (dr == DialogResult.OK)
                        {
                            // refresh grids after successful edit
                            RenderGrids(dateTimePicker1.Value.Date);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to open Edit form: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (colName == "Complete")
            {
                // Open the Complete_Appointment form populated with the appointment details
                OpenCompleteForm(apptId);
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

        // Opens Complete_Appointment and fills labels 6,7,9,10,11,13,14 (patient, dentist, staff1, staff2, date, services, total price)
        private void OpenCompleteForm(int appointmentId)
        {
            // Build expressions the same way RenderGrids does so names display correctly
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

            string serviceExpr;
            if (ColumnExists("services", "name"))
                serviceExpr = "s.name";
            else
                serviceExpr = "CONVERT(varchar(20), s.service_id)";

            string sql =
                "SELECT a.appointment_id, a.appointment_date, a.appointment_time, ISNULL(a.notes,'') AS notes, " +
                patientExpr + " AS patient_name, " +
                dentistExpr + " AS dentist_name, " +
                serviceExpr + " AS service_name, " +
                staffExpr1 + " AS staff1_name, " +
                staffExpr2 + " AS staff2_name, " +
                "a.service_id, " +
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
                "LEFT JOIN staff st1 ON a.staff_assign_1 = st1.staff_id " +
                "LEFT JOIN staff st2 ON a.staff_assign_2 = st2.staff_id " +
                "WHERE a.appointment_id = @id";

            try
            {
                // Read appointment into locals, then close reader/connection before running other commands
                string patient, dentist, staff1, staff2, notes, singleServiceName, servicesText;
                DateTime apptDate;
                int? singleServiceId;

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", appointmentId);
                    conn.Open();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read())
                        {
                            MessageBox.Show("Appointment not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        patient = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString() : string.Empty;
                        dentist = rdr["dentist_name"] != DBNull.Value ? rdr["dentist_name"].ToString() : string.Empty;
                        staff1 = rdr["staff1_name"] != DBNull.Value ? rdr["staff1_name"].ToString() : string.Empty;
                        staff2 = rdr["staff2_name"] != DBNull.Value ? rdr["staff2_name"].ToString() : string.Empty;
                        notes = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;
                        apptDate = rdr["appointment_date"] != DBNull.Value ? Convert.ToDateTime(rdr["appointment_date"]) : DateTime.MinValue;
                        singleServiceName = rdr["service_name"] != DBNull.Value ? rdr["service_name"].ToString() : string.Empty;
                        singleServiceId = rdr["service_id"] != DBNull.Value ? Convert.ToInt32(rdr["service_id"]) : (int?)null;
                        servicesText = rdr["services_list"] != DBNull.Value ? rdr["services_list"].ToString() : singleServiceName;
                    } // reader disposed here, connection closed below
                }

                // compute total price using a new connection (no reader open)
                decimal? totalPrice = null;
                var names = servicesText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (names.Any())
                {
                    var paramNames = names.Select((n, i) => "@n" + i).ToArray();
                    var sumSql = $"SELECT SUM(price) FROM services WHERE name IN ({string.Join(",", paramNames)})";

                    using (var conn2 = new SqlConnection(connectionString))
                    using (var sumCmd = new SqlCommand(sumSql, conn2))
                    {
                        for (int i = 0; i < names.Count; i++)
                            sumCmd.Parameters.AddWithValue(paramNames[i], names[i]);

                        conn2.Open();
                        var s = sumCmd.ExecuteScalar();
                        if (s != DBNull.Value && s != null)
                            totalPrice = Convert.ToDecimal(s);
                    }
                }

                // fallback: single service_id on its own connection
                if (!totalPrice.HasValue && singleServiceId.HasValue)
                {
                    using (var conn3 = new SqlConnection(connectionString))
                    using (var priceCmd = new SqlCommand("SELECT price FROM services WHERE service_id = @sid", conn3))
                    {
                        priceCmd.Parameters.AddWithValue("@sid", singleServiceId.Value);
                        conn3.Open();
                        var s = priceCmd.ExecuteScalar();
                        if (s != DBNull.Value && s != null)
                            totalPrice = Convert.ToDecimal(s);
                    }
                }

                // open the Complete_Appointment form with appointmentId as first arg
                var completeForm = new Complete_Appointment(
                    appointmentId,
                    patient,
                    dentist,
                    staff1,
                    staff2,
                    apptDate,
                    servicesText,
                    totalPrice);

                // show as non-modal secondary form while keeping Appointments open
                completeForm.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open Complete form: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // expose a public refresh so another form can ask Appointments to reload
        public void RefreshGridsPublic()
        {
            RenderGrids(dateTimePicker1.Value.Date);
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

        private void button8_Click(object sender, EventArgs e)
        {
            Pending_Appointments pending_Appointments = new Pending_Appointments();
            pending_Appointments.Show();
            this.Hide();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Pending_Appointments pending_Appointments = new Pending_Appointments();
            pending_Appointments.Show();
            this.Hide();
        }

        private void button11_Click(object sender, EventArgs e)
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
    }
}