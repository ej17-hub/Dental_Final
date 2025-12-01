using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Add_Appointment : Form
    {
        string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Add_Appointment()
        {
            InitializeComponent();

            // load lookups
            LoadPatientsIntoComboBox();
            LoadDentistsIntoComboBox();
            LoadStaffIntoComboBoxes();

            // services checkbox list
            LoadServicesIntoCheckedListBox();

            this.button2.Click -= Button2_Click;
            this.button2.Click += Button2_Click;

            // Wire up dentist selection change event to filter services
            this.comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            this.comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

            // Wire up date picker change to reload dentists when date changes
            if (this.dateTimePicker1 != null)
            {
                this.dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
                this.dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // Reload dentists when date changes to filter by new day
            LoadDentistsIntoComboBox();
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

        private void LoadPatientsIntoComboBox()
        {
            string select;
            if (ColumnExists("patients", "first_name") && ColumnExists("patients", "last_name"))
            {
                select = "SELECT patient_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name FROM patients ORDER BY last_name, first_name";
            }
            else if (ColumnExists("patients", "name"))
            {
                select = "SELECT patient_id, RTRIM(ISNULL(name,'')) AS display_name FROM patients ORDER BY name";
            }
            else
            {
                select = "SELECT patient_id, CONVERT(varchar(20), patient_id) AS display_name FROM patients ORDER BY patient_id";
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(select, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        comboBox2.Items.Clear();
                        while (rdr.Read())
                        {
                            var id = rdr["patient_id"] != DBNull.Value ? Convert.ToInt32(rdr["patient_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;
                            comboBox2.Items.Add(new ServiceItem(id, name));
                        }
                    }
                }

                comboBox2.DisplayMember = "Name";
                comboBox2.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                comboBox2.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Loading patients");
            }
        }

        private void LoadDentistsIntoComboBox()
        {
            string dayName = (this.dateTimePicker1 != null) ? this.dateTimePicker1.Value.DayOfWeek.ToString() : DateTime.Today.DayOfWeek.ToString();

            string selectBase;
            string orderBy;
            bool hasSpecialization = ColumnExists("dentists", "specialization");

            if (ColumnExists("dentists", "first_name") && ColumnExists("dentists", "last_name"))
            {
                selectBase = hasSpecialization
                    ? "SELECT dentist_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name, specialization, available_days FROM dentists"
                    : "SELECT dentist_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name, available_days FROM dentists";
                orderBy = " ORDER BY last_name, first_name";
            }
            else if (ColumnExists("dentists", "name"))
            {
                selectBase = hasSpecialization
                    ? "SELECT dentist_id, RTRIM(ISNULL(name,'')) AS display_name, specialization, available_days FROM dentists"
                    : "SELECT dentist_id, RTRIM(ISNULL(name,'')) AS display_name, available_days FROM dentists";
                orderBy = " ORDER BY name";
            }
            else
            {
                selectBase = hasSpecialization
                    ? "SELECT dentist_id, CONVERT(varchar(20), dentist_id) AS display_name, specialization, available_days FROM dentists"
                    : "SELECT dentist_id, CONVERT(varchar(20), dentist_id) AS display_name, available_days FROM dentists";
                orderBy = " ORDER BY dentist_id";
            }

            bool hasAvailableDays = ColumnExists("dentists", "available_days");
            string finalSql = selectBase + orderBy;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(finalSql, conn))
                {
                    conn.Open();
                    comboBox1.Items.Clear();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var id = rdr["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr["dentist_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;

                            if (hasAvailableDays)
                            {
                                string availableDays = rdr["available_days"] != DBNull.Value ? rdr["available_days"].ToString() : string.Empty;
                                bool isAvailable = string.IsNullOrWhiteSpace(availableDays);

                                if (!isAvailable)
                                {
                                    var days = availableDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(d => d.Trim())
                                                            .ToList();
                                    isAvailable = days.Any(d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase));
                                }

                                if (!isAvailable)
                                    continue;
                            }

                            var item = new ServiceItem(id, name);

                            if (hasSpecialization && rdr["specialization"] != DBNull.Value)
                            {
                                item.Tag = rdr["specialization"].ToString();
                            }

                            comboBox1.Items.Add(item);
                        }
                    }
                }

                comboBox1.DisplayMember = "Name";
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Loading dentists");
            }
        }

        private void LoadStaffIntoComboBoxes()
        {
            string select;
            if (ColumnExists("staff", "first_name") && ColumnExists("staff", "last_name"))
            {
                select = "SELECT staff_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name FROM staff ORDER BY last_name, first_name";
            }
            else if (ColumnExists("staff", "name"))
            {
                select = "SELECT staff_id, RTRIM(ISNULL(name,'')) AS display_name FROM staff ORDER BY name";
            }
            else
            {
                select = "SELECT staff_id, CONVERT(varchar(20), staff_id) AS display_name FROM staff ORDER BY staff_id";
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(select, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        cmbGender.Items.Clear();
                        textBox1.Items.Clear();

                        while (rdr.Read())
                        {
                            var id = rdr["staff_id"] != DBNull.Value ? Convert.ToInt32(rdr["staff_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;
                            var item = new ServiceItem(id, name);
                            cmbGender.Items.Add(item);
                            textBox1.Items.Add(item);
                        }
                    }
                }

                cmbGender.DisplayMember = "Name";
                textBox1.DisplayMember = "Name";
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Loading staff");
            }
        }

        private void LoadServicesIntoCheckedListBox(string specializationFilter = null)
        {
            bool hasCategoryColumn = ColumnExists("services", "category");

            string query = "SELECT service_id, name FROM services";

            if (!string.IsNullOrWhiteSpace(specializationFilter) && hasCategoryColumn)
            {
                query += " WHERE category = @category OR category IS NULL OR category = ''";
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrWhiteSpace(specializationFilter) && hasCategoryColumn)
                {
                    cmd.Parameters.AddWithValue("@category", specializationFilter);
                }

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    checkedListBoxServices.Items.Clear();

                    while (reader.Read())
                    {
                        var serviceId = reader.GetInt32(0);
                        var serviceName = reader.GetString(1);
                        checkedListBoxServices.Items.Add(new ServiceItem(serviceId, serviceName));
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Loading services");
                }
            }

            checkedListBoxServices.DisplayMember = "Name";
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedDentist = comboBox1.SelectedItem as ServiceItem;
            if (selectedDentist == null || selectedDentist.Id <= 0)
            {
                LoadServicesIntoCheckedListBox();
                return;
            }

            string specialization = selectedDentist.Tag as string;
            LoadServicesIntoCheckedListBox(specialization);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Validate Patient
            var patientItem = comboBox2.SelectedItem as ServiceItem;
            if (patientItem == null || patientItem.Id <= 0)
            {
                ErrorHandler.HandleValidationError("Please select a patient.", "Validation");
                return;
            }

            // Validate Dentist
            var dentistItem = comboBox1.SelectedItem as ServiceItem;
            if (dentistItem == null || dentistItem.Id <= 0)
            {
                ErrorHandler.HandleValidationError("Please select a dentist.", "Validation");
                return;
            }

            // Staff (optional)
            var staff1Item = cmbGender.SelectedItem as ServiceItem;
            var staff2Item = textBox1.SelectedItem as ServiceItem;
            int? staff1Id = (staff1Item != null && staff1Item.Id > 0) ? staff1Item.Id : (int?)null;
            int? staff2Id = (staff2Item != null && staff2Item.Id > 0) ? staff2Item.Id : (int?)null;

            // Validate Services
            var selectedServiceItems = checkedListBoxServices.CheckedItems
                .OfType<ServiceItem>()
                .ToList();

            if (selectedServiceItems.Count == 0)
            {
                ErrorHandler.HandleValidationError("Please select at least one service.", "Validation");
                return;
            }

            // Get Appointment Date and Time
            DateTime apptDate;
            TimeSpan apptTime;

            if (dateTimePicker1 != null)
                apptDate = dateTimePicker1.Value.Date;
            else
                apptDate = DateTime.Today;

            if (dateTimePicker2 != null)
                apptTime = dateTimePicker2.Value.TimeOfDay;
            else
                apptTime = TimeSpan.Zero;

            // VALIDATE APPOINTMENT TIME (8 AM - 5 PM)
            if (!ErrorHandler.ValidateAppointmentTime(apptTime, out string timeError))
            {
                return;
            }

            // VALIDATE DATE (must be today or future)
            if (!ErrorHandler.ValidateDate(apptDate, "Appointment Date", futureOnly: true))
            {
                return;
            }

            // CHECK FOR DUPLICATE APPOINTMENT AND CONFLICTS
            if (!ErrorHandler.ValidateAppointmentBooking(
                connectionString,
                dentistItem.Id,
                apptDate,
                apptTime,
                excludeAppointmentId: null,
                checkConflicts: true,
                bufferMinutes: 30))
            {
                return;
            }

            // CHECK PATIENT APPOINTMENT LIMIT
            if (!ErrorHandler.CheckPatientAppointmentLimit(connectionString, patientItem.Id, out string limitError, maxPendingAppointments: 5))
            {
                return;
            }

            // Get Notes
            string notes = null;
            if (this.textBox2 != null)
            {
                notes = string.IsNullOrWhiteSpace(this.textBox2.Text) ? null : this.textBox2.Text.Trim();
            }

            var serviceIds = selectedServiceItems.Select(si => si.Id).ToList();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Calculate total price
                        decimal? totalPrice = null;
                        if (serviceIds.Any())
                        {
                            var pnames = serviceIds.Select((s, i) => "@s" + i).ToArray();
                            var sumSql = $"SELECT SUM(price) FROM services WHERE service_id IN ({string.Join(",", pnames)})";

                            using (var sumCmd = new SqlCommand(sumSql, conn, tran))
                            {
                                for (int i = 0; i < serviceIds.Count; i++)
                                    sumCmd.Parameters.AddWithValue(pnames[i], serviceIds[i]);

                                var s = sumCmd.ExecuteScalar();
                                if (s != DBNull.Value && s != null)
                                    totalPrice = Convert.ToDecimal(s);
                            }
                        }

                        int? serviceIdForColumn = serviceIds.FirstOrDefault();
                        bool hasPriceColumn = ColumnExists("appointments", "price");

                        string insertSql;
                        if (hasPriceColumn)
                        {
                            insertSql = @"
                                INSERT INTO appointments
                                    (patient_id, dentist_id, service_id, appointment_date, appointment_time, staff_assign_1, staff_assign_2, notes, price, created_at)
                                VALUES
                                    (@patient_id, @dentist_id, @service_id, @appointment_date, @appointment_time, @staff1, @staff2, @notes, @price, GETDATE());
                                SELECT CAST(SCOPE_IDENTITY() AS int);
                            ";
                        }
                        else
                        {
                            insertSql = @"
                                INSERT INTO appointments
                                    (patient_id, dentist_id, service_id, appointment_date, appointment_time, staff_assign_1, staff_assign_2, notes, created_at)
                                VALUES
                                    (@patient_id, @dentist_id, @service_id, @appointment_date, @appointment_time, @staff1, @staff2, @notes, GETDATE());
                                SELECT CAST(SCOPE_IDENTITY() AS int);
                            ";
                        }

                        int newAppointmentId;
                        using (var cmd = new SqlCommand(insertSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@patient_id", patientItem.Id);
                            cmd.Parameters.AddWithValue("@dentist_id", dentistItem.Id);
                            if (serviceIdForColumn.HasValue)
                                cmd.Parameters.AddWithValue("@service_id", serviceIdForColumn.Value);
                            else
                                cmd.Parameters.AddWithValue("@service_id", DBNull.Value);

                            // Use DATETIME for appointment_date and VARCHAR for appointment_time
                            cmd.Parameters.AddWithValue("@appointment_date", apptDate);
                            cmd.Parameters.AddWithValue("@appointment_time", apptTime.ToString(@"hh\:mm"));

                            cmd.Parameters.AddWithValue("@staff1", staff1Id.HasValue ? (object)staff1Id.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@staff2", staff2Id.HasValue ? (object)staff2Id.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);

                            if (hasPriceColumn)
                            {
                                if (totalPrice.HasValue)
                                {
                                    var p = cmd.Parameters.Add("@price", SqlDbType.Decimal);
                                    p.Precision = 10;
                                    p.Scale = 2;
                                    p.Value = totalPrice.Value;
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@price", DBNull.Value);
                                }
                            }

                            newAppointmentId = (int)cmd.ExecuteScalar();
                        }

                        // Insert into appointments_services linking table
                        if (TableExists("appointments_services"))
                        {
                            const string linkInsert = "INSERT INTO appointments_services (appointment_id, service_id) VALUES (@aid, @sid)";
                            using (var linkCmd = new SqlCommand(linkInsert, conn, tran))
                            {
                                linkCmd.Parameters.Add("@aid", SqlDbType.Int).Value = newAppointmentId;
                                linkCmd.Parameters.Add("@sid", SqlDbType.Int);

                                foreach (var sid in serviceIds)
                                {
                                    linkCmd.Parameters["@sid"].Value = sid;
                                    linkCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();

                        // Log activity
                        try
                        {
                            DateTime dt = apptDate.Date + apptTime;
                            ActivityLogger.Log($"Admin added an appointment for {patientItem.Name} on {dt:h:mm tt MMMM d, yyyy}");
                        }
                        catch { }

                        ErrorHandler.ShowSuccess("Appointment created successfully!");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ErrorHandler.HandleDatabaseError(ex, "Saving appointment");
                    }
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            var result = ErrorHandler.ShowConfirmation(
                "Are you sure you want to cancel? Unsaved information will be lost.",
                "Confirm Cancel");

            if (result)
            {
                this.Close();
            }
        }
    }

    public class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public object Tag { get; set; }

        public ServiceItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
