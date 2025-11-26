using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using Dental_Final;

namespace Dental_Final.User
{
    public partial class Add_Appointment_Patient : Form
    {
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        // New: caller should set this to the current patient's id before showing the form.
        // Example: var f = new Add_Appointment_Patient { PatientId = currentPatientId }; f.Show();
        public int? PatientId { get; set; }

        public Add_Appointment_Patient()
        {
            InitializeComponent();

            // wire events
            this.Load += Add_Appointment_Patient_Load;
            button1.Click -= Button1_Click;
            button1.Click += Button1_Click;
            button2.Click -= Button2_Click;
            button2.Click += Button2_Click;

            // wire category change (comboBox1) to reload services
            comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
        }

        private void Add_Appointment_Patient_Load(object sender, EventArgs e)
        {
            LoadCategoriesIntoComboBox();

            // Populate services using current category selection (default will be "(None)")
            var selCat = comboBox1.SelectedItem as string;
            LoadServicesIntoCheckedList(selCat);

            // Populate label3 with the patient's full name if PatientId is provided
            if (PatientId.HasValue)
            {
                TryLoadPatientDisplayName(PatientId.Value);
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var category = comboBox1.SelectedItem as string;
            LoadServicesIntoCheckedList(category);
        }

        private void TryLoadPatientDisplayName(int patientId)
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
                        if (rdr.Read())
                        {
                            var first = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                            var middle = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty;
                            var last = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;

                            // use shared formatter
                            label3.Text = NameFormatter.FormatPatientDisplayName(first, middle, last);
                        }
                        else
                        {
                            // not found -> clear or set default
                            label3.Text = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // don't crash the form; show a friendly fallback and log if needed
                label3.Text = string.Empty;
                MessageBox.Show("Failed to load patient name: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Load categories into comboBox1. First entry is "(None)". If services.category is not available/populated,
        // fall back to dentist specializations and finally to a sensible default list.
        private void LoadCategoriesIntoComboBox()
        {
            try
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("(None)"); // default to no services shown until a category is selected

                // try to get distinct categories from services table
                const string catSql = @"
                    SELECT DISTINCT LTRIM(RTRIM(category)) AS category
                    FROM services
                    WHERE category IS NOT NULL AND LTRIM(RTRIM(category)) <> ''
                    ORDER BY category;";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(catSql, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var cat = rdr["category"] != DBNull.Value ? rdr["category"].ToString() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(cat) && !comboBox1.Items.Contains(cat))
                                comboBox1.Items.Add(cat);
                        }
                    }
                }

                // If no categories found in services table, try dentist specializations (fallback used in Add_Services)
                if (comboBox1.Items.Count <= 1)
                {
                    const string specSql = @"
                        SELECT DISTINCT specialization
                        FROM dentists
                        WHERE specialization IS NOT NULL AND LTRIM(RTRIM(specialization)) <> ''
                        ORDER BY specialization;";

                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand(specSql, conn))
                    {
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var spec = rdr["specialization"] != DBNull.Value ? rdr["specialization"].ToString() : string.Empty;
                                if (!string.IsNullOrWhiteSpace(spec) && !comboBox1.Items.Contains(spec))
                                    comboBox1.Items.Add(spec);
                            }
                        }
                    }
                }

                // final fallback default categories if still none found
                if (comboBox1.Items.Count <= 1)
                {
                    var defaults = new[]
                    {
                        "General Dentistry",
                        "Restorative Dentistry",
                        "Pediatric Dentistry",
                        "Orthodontics",
                        "Prosthodontics",
                        "Cosmetic Dentistry",
                        "Oral Surgery",
                        "Endodontics"
                    };
                    foreach (var d in defaults)
                    {
                        if (!comboBox1.Items.Contains(d))
                            comboBox1.Items.Add(d);
                    }
                }

                comboBox1.SelectedIndex = 0;
            }
            catch
            {
                // On any error populate sensible defaults and keep app usable
                comboBox1.Items.Clear();
                comboBox1.Items.Add("(None)");
                comboBox1.Items.Add("General Dentistry");
                comboBox1.Items.Add("Restorative Dentistry");
                comboBox1.Items.Add("Pediatric Dentistry");
                comboBox1.Items.Add("Orthodontics");
                comboBox1.Items.Add("Prosthodontics");
                comboBox1.Items.Add("Cosmetic Dentistry");
                comboBox1.Items.Add("Oral Surgery");
                comboBox1.Items.Add("Endodontics");
                comboBox1.SelectedIndex = 0;
            }
        }

        // Loads services into checkedListBoxServices filtered by category.
        // If category is null, "(None)" or "None" then NO services are listed.
        private void LoadServicesIntoCheckedList(string category = null)
        {
            try
            {
                checkedListBoxServices.Items.Clear();

                // If category is explicitly "(None)" or "None" or empty -> do not list any services
                if (string.IsNullOrWhiteSpace(category) || category == "(None)" || category.Equals("None", StringComparison.OrdinalIgnoreCase))
                    return;

                // Use COALESCE to handle either 'category' or legacy 'specialization' column when present
                string sqlByCategory = @"
                    SELECT service_id, name, description, price, duration_minutes,
                           COALESCE(category, specialization, '') AS specialization
                    FROM services
                    WHERE LTRIM(RTRIM(COALESCE(category, ''))) = @cat
                    ORDER BY name;";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sqlByCategory, conn))
                {
                    cmd.Parameters.AddWithValue("@cat", category.Trim());
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var s = new Dental_Final.ServiceDto
                            {
                                ServiceId = rdr["service_id"] != DBNull.Value ? Convert.ToInt32(rdr["service_id"]) : 0,
                                Name = rdr["name"] != DBNull.Value ? rdr["name"].ToString() : string.Empty,
                                Description = rdr["description"] != DBNull.Value ? rdr["description"].ToString() : string.Empty,
                                Price = rdr["price"] != DBNull.Value ? (decimal?)Convert.ToDecimal(rdr["price"]) : null,
                                DurationMinutes = rdr["duration_minutes"] != DBNull.Value ? (int?)Convert.ToInt32(rdr["duration_minutes"]) : null,
                                Specialization = rdr["specialization"] != DBNull.Value ? rdr["specialization"].ToString() : string.Empty
                            };

                            // ServiceDto.ToString() returns Name so it will display correctly in the CheckedListBox
                            checkedListBoxServices.Items.Add(s);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // If 'category' column doesn't exist (invalid column name), attempt to filter by specialization instead.
                if (sqlEx.Number == 207) // Invalid column name
                {
                    try
                    {
                        checkedListBoxServices.Items.Clear();

                        // If user selected "(None)" or "None" we already returned earlier; here category is a real value.
                        string fallbackBySpec = @"
                            SELECT service_id, name, description, price, duration_minutes, specialization
                            FROM services
                            WHERE LTRIM(RTRIM(ISNULL(specialization,''))) = @cat
                            ORDER BY name;";

                        using (var conn = new SqlConnection(connectionString))
                        using (var cmd = new SqlCommand(fallbackBySpec, conn))
                        {
                            cmd.Parameters.AddWithValue("@cat", (category ?? string.Empty).Trim());
                            conn.Open();
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    var s = new Dental_Final.ServiceDto
                                    {
                                        ServiceId = rdr["service_id"] != DBNull.Value ? Convert.ToInt32(rdr["service_id"]) : 0,
                                        Name = rdr["name"] != DBNull.Value ? rdr["name"].ToString() : string.Empty,
                                        Description = rdr["description"] != DBNull.Value ? rdr["description"].ToString() : string.Empty,
                                        Price = rdr["price"] != DBNull.Value ? (decimal?)Convert.ToDecimal(rdr["price"]) : null,
                                        DurationMinutes = rdr["duration_minutes"] != DBNull.Value ? (int?)Convert.ToInt32(rdr["duration_minutes"]) : null,
                                        Specialization = rdr["specialization"] != DBNull.Value ? rdr["specialization"].ToString() : string.Empty
                                    };
                                    checkedListBoxServices.Items.Add(s);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load services: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to load services: " + sqlEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load services: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            // Gather selected services
            var selected = checkedListBoxServices.CheckedItems
                                .OfType<ServiceDto>()   // now resolves because of 'using Dental_Final;'
                                .ToList();

            if (!selected.Any())
            {
                MessageBox.Show("Please select at least one service.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!PatientId.HasValue)
            {
                MessageBox.Show("Patient id not set. Please log in first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build appointment date/time from pickers
            var date = dateTimePicker1.Value.Date;
            var time = dateTimePicker2.Value.TimeOfDay;
            var appointmentDate = date;                         // date part
            var appointmentTime = time;                         // time part
            var appointmentDateTime = date.Add(time);

            try
            {
                int newRequestId = -1;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert one appointment request row
                            const string insertRequestSql = @"
                                INSERT INTO appointment_requests
                                    (patient_id, requested_date, requested_time, notes, status, created_at)
                                VALUES
                                    (@patient_id, @requested_date, @requested_time, @notes, @status, GETDATE());
                                SELECT CAST(SCOPE_IDENTITY() AS int);";

                            using (var cmd = new SqlCommand(insertRequestSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@patient_id", PatientId.Value);
                                cmd.Parameters.Add("@requested_date", SqlDbType.Date).Value = appointmentDate;
                                cmd.Parameters.Add("@requested_time", SqlDbType.Time).Value = appointmentTime;
                                cmd.Parameters.AddWithValue("@notes", "[Pending]");
                                cmd.Parameters.AddWithValue("@status", "Pending");
                                var idObj = cmd.ExecuteScalar();
                                newRequestId = (idObj != null && idObj != DBNull.Value) ? Convert.ToInt32(idObj) : -1;
                            }

                            if (newRequestId <= 0)
                                throw new Exception("Failed to create appointment request record.");

                            // Insert into appointment_requests_services for each selected service
                            const string insertReqSvcSql = @"
                                INSERT INTO appointment_requests_services (request_id, service_id)
                                VALUES (@request_id, @service_id);";

                            using (var cmd2 = new SqlCommand(insertReqSvcSql, conn, tx))
                            {
                                cmd2.Parameters.Add("@request_id", SqlDbType.Int).Value = newRequestId;
                                var pService = cmd2.Parameters.Add("@service_id", SqlDbType.Int);

                                foreach (var svc in selected)
                                {
                                    pService.Value = svc.ServiceId;
                                    cmd2.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                // Build services display string (single line)
                var servicesDisplay = string.Join(", ", selected.Select(s => s.Name).ToArray());

                // Try to find an existing Patient_Appointments form and refresh it instead of opening a new one
                var openAppts = Application.OpenForms.Cast<Form>().OfType<Patient_Appointments>().FirstOrDefault();
                if (openAppts != null)
                {
                    // refresh the existing form's grid from DB
                    openAppts.LoadRequestsForPatient(PatientId.Value);
                    openAppts.BringToFront();
                    openAppts.Focus();
                }
                else
                {
                    // Show Patient_Appointments and add single pending row with joined services
                    var apptsForm = new Patient_Appointments();
                    apptsForm.Tag = PatientId.Value;
                    apptsForm.LoadRequestsForPatient(PatientId.Value);
                    apptsForm.Show();
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to request appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            // Return/back - close this form (caller can remain)
            this.Close();
        }

    }
}