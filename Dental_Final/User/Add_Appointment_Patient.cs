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
        private readonly string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public int? PatientId { get; set; }

        public Add_Appointment_Patient()
        {
            InitializeComponent();

            this.Load += Add_Appointment_Patient_Load;
            button1.Click -= Button1_Click;
            button1.Click += Button1_Click;
            button2.Click -= Button2_Click;
            button2.Click += Button2_Click;

            comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

            // Wire up date picker to reload categories when date changes
            if (dateTimePicker1 != null)
            {
                dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
                dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            }
        }

        private void Add_Appointment_Patient_Load(object sender, EventArgs e)
        {
            // Load categories based on available dentists for the selected date
            LoadCategoriesForAvailableDentists(dateTimePicker1.Value);

            var selCat = comboBox1.SelectedItem as string;
            LoadServicesIntoCheckedList(selCat);

            if (PatientId.HasValue)
            {
                TryLoadPatientDisplayName(PatientId.Value);
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // Reload categories when date changes - only show categories for dentists available on this date
            LoadCategoriesForAvailableDentists(dateTimePicker1.Value);
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var category = comboBox1.SelectedItem as string;
            LoadServicesIntoCheckedList(category);
        }

        /// <summary>
        /// Load categories/specializations but ONLY for dentists available on the selected date
        /// </summary>
        private void LoadCategoriesForAvailableDentists(DateTime selectedDate)
        {
            try
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("(None)");

                string dayOfWeek = selectedDate.DayOfWeek.ToString();

                // Get specializations from dentists who are available on the selected day
                string query = @"
                    SELECT DISTINCT 
                        LTRIM(RTRIM(d.specialization)) AS specialization
                    FROM dentists d
                    WHERE d.specialization IS NOT NULL 
                    AND LTRIM(RTRIM(d.specialization)) <> ''
                    ORDER BY specialization";

                HashSet<string> availableSpecializations = new HashSet<string>();

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string specialization = reader["specialization"]?.ToString() ?? "";
                            if (!string.IsNullOrWhiteSpace(specialization))
                            {
                                availableSpecializations.Add(specialization.Trim());
                            }
                        }
                    }
                }

                // Now filter by checking if dentist is available on selected day
                List<string> filteredSpecializations = new List<string>();

                string dentistQuery = @"
                    SELECT 
                        d.specialization,
                        d.available_days
                    FROM dentists d
                    WHERE d.specialization IS NOT NULL 
                    AND LTRIM(RTRIM(d.specialization)) <> ''";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(dentistQuery, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string specialization = reader["specialization"]?.ToString()?.Trim() ?? "";
                            string availableDays = reader["available_days"]?.ToString() ?? "";

                            if (string.IsNullOrWhiteSpace(specialization))
                                continue;

                            // Check if dentist is available on selected day
                            bool isAvailable = string.IsNullOrWhiteSpace(availableDays); // null means available all days

                            if (!isAvailable && !string.IsNullOrEmpty(availableDays))
                            {
                                var days = availableDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(d => d.Trim())
                                                       .ToList();
                                isAvailable = days.Any(d => d.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));
                            }

                            // Only add specialization if dentist is available on this day
                            if (isAvailable && !filteredSpecializations.Contains(specialization))
                            {
                                filteredSpecializations.Add(specialization);
                            }
                        }
                    }
                }

                // Add filtered specializations to combobox
                foreach (var spec in filteredSpecializations.OrderBy(s => s))
                {
                    comboBox1.Items.Add(spec);
                }

                // If no specializations found, also check service categories
                if (comboBox1.Items.Count <= 1)
                {
                    const string catSql = @"
                        SELECT DISTINCT LTRIM(RTRIM(category)) AS category
                        FROM services
                        WHERE category IS NOT NULL AND LTRIM(RTRIM(category)) <> ''
                        ORDER BY category";

                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand(catSql, conn))
                    {
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var cat = rdr["category"]?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrWhiteSpace(cat) && !comboBox1.Items.Contains(cat))
                                {
                                    // Check if there's a dentist with this specialization available on selected day
                                    if (filteredSpecializations.Contains(cat))
                                    {
                                        comboBox1.Items.Add(cat);
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback: if still no categories, add default ones only if dentists are available
                if (comboBox1.Items.Count <= 1 && filteredSpecializations.Any())
                {
                    foreach (var spec in filteredSpecializations)
                    {
                        if (!comboBox1.Items.Contains(spec))
                            comboBox1.Items.Add(spec);
                    }
                }

                comboBox1.SelectedIndex = 0;

                // Show message if no categories available for this date
                if (comboBox1.Items.Count <= 1)
                {
                    MessageBox.Show(
                        $"No dentists are available on {selectedDate:MMMM d, yyyy} ({dayOfWeek}).\n\n" +
                        "Please select a different date.",
                        "No Services Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("(None)");
                comboBox1.SelectedIndex = 0;
                ErrorHandler.HandleDatabaseError(ex, "Loading available categories");
            }
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

                            label3.Text = NameFormatter.FormatPatientDisplayName(first, middle, last);
                        }
                        else
                        {
                            label3.Text = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                label3.Text = string.Empty;
                ErrorHandler.HandleDatabaseError(ex, "Loading patient name");
            }
        }

        private void LoadServicesIntoCheckedList(string category = null)
        {
            try
            {
                checkedListBoxServices.Items.Clear();

                if (string.IsNullOrWhiteSpace(category) || category == "(None)" || category.Equals("None", StringComparison.OrdinalIgnoreCase))
                    return;

                string sqlByCategory = @"
                    SELECT service_id, name, description, price, duration_minutes,
                           COALESCE(category, specialization, '') AS specialization
                    FROM services
                    WHERE LTRIM(RTRIM(COALESCE(category, specialization, ''))) = @cat
                    ORDER BY name";

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

                            checkedListBoxServices.Items.Add(s);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 207)
                {
                    try
                    {
                        checkedListBoxServices.Items.Clear();

                        string fallbackBySpec = @"
                            SELECT service_id, name, description, price, duration_minutes, specialization
                            FROM services
                            WHERE LTRIM(RTRIM(ISNULL(specialization,''))) = @cat
                            ORDER BY name";

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
                        ErrorHandler.HandleDatabaseError(ex, "Loading services");
                    }
                }
                else
                {
                    ErrorHandler.HandleDatabaseError(sqlEx, "Loading services");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Loading services");
            }
        }

        private bool IsTimeSlotAlreadyTaken(DateTime date, TimeSpan time, out int conflictCount)
        {
            conflictCount = 0;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string appointmentQuery = @"
                        SELECT COUNT(*) AS appointment_count
                        FROM appointments a
                        WHERE CAST(a.appointment_date AS DATE) = @date
                        AND a.appointment_time = @time";

                    using (var cmd = new SqlCommand(appointmentQuery, conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = date;
                        cmd.Parameters.AddWithValue("@time", time.ToString(@"hh\:mm"));

                        var result = cmd.ExecuteScalar();
                        int appointmentCount = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
                        conflictCount += appointmentCount;
                    }

                    string requestQuery = @"
                        SELECT COUNT(*) AS request_count
                        FROM appointment_requests ar
                        WHERE ar.requested_date = @date
                        AND ar.requested_time = @time
                        AND ar.status = 'Pending'";

                    using (var cmd = new SqlCommand(requestQuery, conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = date;
                        cmd.Parameters.Add("@time", SqlDbType.Time).Value = time;

                        var result = cmd.ExecuteScalar();
                        int requestCount = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
                        conflictCount += requestCount;
                    }
                }

                return conflictCount > 0;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Checking time slot availability");
                return false;
            }
        }

        private List<string> GetAvailableTimeSlots(DateTime date)
        {
            List<string> availableSlots = new List<string>();
            HashSet<string> bookedTimes = new HashSet<string>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string appointmentQuery = @"
                        SELECT DISTINCT a.appointment_time
                        FROM appointments a
                        WHERE CAST(a.appointment_date AS DATE) = @date";

                    using (var cmd = new SqlCommand(appointmentQuery, conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = date;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string timeStr = reader["appointment_time"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(timeStr))
                                {
                                    bookedTimes.Add(timeStr);
                                }
                            }
                        }
                    }

                    string requestQuery = @"
                        SELECT DISTINCT CONVERT(VARCHAR(5), ar.requested_time, 108) AS appointment_time
                        FROM appointment_requests ar
                        WHERE ar.requested_date = @date
                        AND ar.status = 'Pending'";

                    using (var cmd = new SqlCommand(requestQuery, conn))
                    {
                        cmd.Parameters.Add("@date", SqlDbType.Date).Value = date;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string timeStr = reader["appointment_time"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(timeStr))
                                {
                                    bookedTimes.Add(timeStr);
                                }
                            }
                        }
                    }
                }

                TimeSpan startTime = new TimeSpan(8, 0, 0);
                TimeSpan endTime = new TimeSpan(17, 0, 0);
                TimeSpan interval = TimeSpan.FromMinutes(30);

                for (TimeSpan currentTime = startTime; currentTime < endTime; currentTime = currentTime.Add(interval))
                {
                    string timeStr = currentTime.ToString(@"hh\:mm");

                    if (!bookedTimes.Contains(timeStr))
                    {
                        string displayTime = DateTime.Today.Add(currentTime).ToString("h:mm tt");
                        availableSlots.Add(displayTime);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Getting available time slots");
            }

            return availableSlots;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var selected = checkedListBoxServices.CheckedItems
                                .OfType<ServiceDto>()
                                .ToList();

            if (!selected.Any())
            {
                ErrorHandler.HandleValidationError("Please select at least one service.", "Validation");
                return;
            }

            if (!PatientId.HasValue)
            {
                ErrorHandler.HandleValidationError("Patient id not set. Please log in first.", "Validation");
                return;
            }

            var date = dateTimePicker1.Value.Date;
            var time = dateTimePicker2.Value.TimeOfDay;

            if (!ErrorHandler.ValidateAppointmentTime(time, out string timeError))
            {
                return;
            }

            if (!ErrorHandler.ValidateDate(date, "Appointment Date", futureOnly: true))
            {
                return;
            }

            if (IsTimeSlotAlreadyTaken(date, time, out int conflictCount))
            {
                var availableSlots = GetAvailableTimeSlots(date);

                string message = $"❌ TIME SLOT NOT AVAILABLE\n\n" +
                               $"Date: {date:MMMM d, yyyy}\n" +
                               $"Time: {DateTime.Today.Add(time):h:mm tt}\n\n" +
                               $"This time slot has already been booked by someone.\n\n" +
                               $"Please choose a different time slot.";

                if (availableSlots.Any())
                {
                    int slotsToShow = Math.Min(availableSlots.Count, 15);
                    message += $"\n\n✓ Available time slots on {date:MMMM d, yyyy}:\n" +
                              string.Join(", ", availableSlots.Take(slotsToShow));

                    if (availableSlots.Count > slotsToShow)
                    {
                        message += $"\n... and {availableSlots.Count - slotsToShow} more slots";
                    }
                }
                else
                {
                    message += $"\n\n⚠️ No available time slots on {date:MMMM d, yyyy}.\nPlease choose a different date.";
                }

                MessageBox.Show(message, "Time Slot Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

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
                            const string insertRequestSql = @"
                                INSERT INTO appointment_requests
                                    (patient_id, requested_date, requested_time, notes, status, created_at)
                                VALUES
                                    (@patient_id, @requested_date, @requested_time, @notes, @status, GETDATE());
                                SELECT CAST(SCOPE_IDENTITY() AS int);";

                            using (var cmd = new SqlCommand(insertRequestSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@patient_id", PatientId.Value);
                                cmd.Parameters.Add("@requested_date", SqlDbType.Date).Value = date;
                                cmd.Parameters.Add("@requested_time", SqlDbType.Time).Value = time;

                                string serviceNames = string.Join(", ", selected.Select(s => s.Name));
                                cmd.Parameters.AddWithValue("@notes", $"Requested {selected.Count} service(s): {serviceNames}");
                                cmd.Parameters.AddWithValue("@status", "Pending");

                                var idObj = cmd.ExecuteScalar();
                                newRequestId = (idObj != null && idObj != DBNull.Value) ? Convert.ToInt32(idObj) : -1;
                            }

                            if (newRequestId <= 0)
                                throw new Exception("Failed to create appointment request record.");

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

                var openAppts = Application.OpenForms.Cast<Form>().OfType<Patient_Appointments>().FirstOrDefault();
                if (openAppts != null)
                {
                    openAppts.LoadRequestsForPatient(PatientId.Value);
                    openAppts.BringToFront();
                    openAppts.Focus();
                }
                else
                {
                    var apptsForm = new Patient_Appointments();
                    apptsForm.Tag = PatientId.Value;
                    apptsForm.LoadRequestsForPatient(PatientId.Value);
                    apptsForm.Show();
                }

                ErrorHandler.ShowSuccess($"✓ Appointment request submitted successfully!\n\n" +
                                       $"Date: {date:MMMM d, yyyy}\n" +
                                       $"Time: {DateTime.Today.Add(time):h:mm tt}\n" +
                                       $"Services: {string.Join(", ", selected.Select(s => s.Name))}\n\n" +
                                       $"Your request is pending admin approval.\n" +
                                       $"You will be notified once it's confirmed.");
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Requesting appointment");
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
