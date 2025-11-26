using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Edit_Appointment : Form
    {
        // match Add_Appointment connection string
        string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";


        // Keep parameterless ctor for designer; use overload when opening for edit
        public Edit_Appointment()
        {
            InitializeComponent();
        }

        // Use this ctor to open and load an existing appointment for editing
        public Edit_Appointment(int appointmentId) : this()
        {
            // store appointment id for save logic
            this.Tag = appointmentId;

            // load lookups
            LoadDentistsIntoComboBox();
            LoadStaffIntoComboBoxes();
            LoadServicesIntoCheckedListBox();

            // wire handlers like Add_Appointment
            this.button2.Click -= Button2_Click;
            this.button2.Click += Button2_Click;

            this.button1.Click -= Button1_Click;
            this.button1.Click += Button1_Click;

            // Wire up dentist selection change event to filter services
            this.comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            this.comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

            // Wire up date picker change to reload dentists when date changes
            if (this.dateTimePicker1 != null)
            {
                this.dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
                this.dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            }

            // load appointment values
            LoadAppointment(appointmentId);
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

        private void LoadDentistsIntoComboBox()
        {
            // Determine selected day name from date picker (falls back to today if control missing)
            string dayName = (this.dateTimePicker1 != null) ? this.dateTimePicker1.Value.DayOfWeek.ToString() : DateTime.Today.DayOfWeek.ToString();

            // Build base select depending on available name columns - include specialization
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

            // If dentists table has available_days column, filter dentists by the dayName
            bool hasAvailableDays = ColumnExists("dentists", "available_days");
            string finalSql = selectBase + orderBy;

            try
            {
                // Store current selection to restore after reload
                int currentDentistId = -1;
                var currentItem = comboBox1.SelectedItem as ServiceItem;
                if (currentItem != null)
                    currentDentistId = currentItem.Id;

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(finalSql, conn))
                {
                    conn.Open();
                    comboBox1.Items.Clear(); // comboBox1 is Dentist in designer
                    
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var id = rdr["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr["dentist_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;
                            
                            // Filter by available_days in C# code (more reliable than SQL CHARINDEX)
                            if (hasAvailableDays)
                            {
                                string availableDays = rdr["available_days"] != DBNull.Value ? rdr["available_days"].ToString() : string.Empty;
                                
                                // If available_days is NULL or empty, dentist is available all days
                                bool isAvailable = string.IsNullOrWhiteSpace(availableDays);
                                
                                if (!isAvailable)
                                {
                                    // Split by comma and check if the day exists (case-insensitive)
                                    var days = availableDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(d => d.Trim())
                                                            .ToList();
                                    isAvailable = days.Any(d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase));
                                }
                                
                                // Skip this dentist if not available on selected day
                                if (!isAvailable)
                                    continue;
                            }
                            
                            // Store specialization in the ServiceItem if available
                            var item = new ServiceItem(id, name);
                            
                            // Store specialization as Tag for later retrieval
                            if (hasSpecialization && rdr["specialization"] != DBNull.Value)
                            {
                                item.Tag = rdr["specialization"].ToString();
                            }
                            
                            comboBox1.Items.Add(item);
                        }
                    }
                }

                comboBox1.DisplayMember = "Name";

                // Restore previous selection if still available
                if (currentDentistId > 0)
                {
                    var restoredItem = comboBox1.Items.OfType<ServiceItem>().FirstOrDefault(x => x.Id == currentDentistId);
                    if (restoredItem != null)
                        comboBox1.SelectedItem = restoredItem;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dentists: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Error loading staff: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadServicesIntoCheckedListBox(string specializationFilter = null)
        {
            // Check if services table has a category column
            bool hasCategoryColumn = ColumnExists("services", "category");

            string query = "SELECT service_id, name FROM services";
            
            // If specialization filter is provided and column exists, filter services
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

                    // Store currently checked items before clearing
                    var checkedIds = new HashSet<int>();
                    for (int i = 0; i < checkedListBoxServices.Items.Count; i++)
                    {
                        if (checkedListBoxServices.GetItemChecked(i))
                        {
                            var si = checkedListBoxServices.Items[i] as ServiceItem;
                            if (si != null)
                                checkedIds.Add(si.Id);
                        }
                    }

                    checkedListBoxServices.Items.Clear();

                    while (reader.Read())
                    {
                        var serviceId = reader.GetInt32(0);
                        var serviceName = reader.GetString(1);
                        var item = new ServiceItem(serviceId, serviceName);
                        checkedListBoxServices.Items.Add(item);
                    }

                    reader.Close();

                    // Restore checked state
                    for (int i = 0; i < checkedListBoxServices.Items.Count; i++)
                    {
                        var si = checkedListBoxServices.Items[i] as ServiceItem;
                        if (si != null && checkedIds.Contains(si.Id))
                            checkedListBoxServices.SetItemChecked(i, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading services: " + ex.Message);
                }
            }

            checkedListBoxServices.DisplayMember = "Name";
        }

        // Event handler for when dentist selection changes
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedDentist = comboBox1.SelectedItem as ServiceItem;
            if (selectedDentist == null || selectedDentist.Id <= 0)
            {
                // No dentist selected, show all services
                LoadServicesIntoCheckedListBox();
                return;
            }

            // Get the dentist's specialization from cached Tag property
            string specialization = selectedDentist.Tag as string;
            
            // Reload services filtered by specialization
            LoadServicesIntoCheckedListBox(specialization);
        }

        // Loads appointment data into controls. Stores patient id in label7.Tag and displays name in label7.Text (read-only)
        private void LoadAppointment(int appointmentId)
        {
            // Build SELECT so we don't reference a column that may not exist (error you reported).
            bool hasFirstLast = ColumnExists("patients", "first_name") && ColumnExists("patients", "last_name");
            bool hasName = ColumnExists("patients", "name");

            // patient_name will always be returned (converted to string) so consuming code is unchanged
            string patientSelect;
            if (hasFirstLast)
                patientSelect = "p.first_name, p.last_name, NULL AS patient_name";
            else if (hasName)
                patientSelect = "NULL AS first_name, NULL AS last_name, RTRIM(ISNULL(p.name,'')) AS patient_name";
            else
                patientSelect = "NULL AS first_name, NULL AS last_name, CONVERT(varchar(20), p.patient_id) AS patient_name";

            string sql = $@"
                SELECT a.appointment_id, a.patient_id, {patientSelect},
                       a.dentist_id, a.staff_assign_1, a.staff_assign_2, a.notes, a.appointment_date, a.appointment_time
                FROM appointments a
                LEFT JOIN patients p ON p.patient_id = a.patient_id
                WHERE a.appointment_id = @id;
            ";

            try
            {
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
                            this.Close();
                            return;
                        }

                        // patient: build display name similar to Add_Appointment
                        int patientId = rdr["patient_id"] != DBNull.Value ? Convert.ToInt32(rdr["patient_id"]) : -1;
                        string displayName = string.Empty;
                        if (hasFirstLast)
                        {
                            var fn = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                            var ln = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;
                            displayName = (fn + " " + ln).Trim();
                        }
                        else
                        {
                            displayName = rdr["patient_name"] != DBNull.Value ? rdr["patient_name"].ToString().Trim() : string.Empty;
                        }

                        // store patient id in label7.Tag so it's available when saving (read-only to user)
                        label7.Tag = patientId;
                        label7.Text = string.IsNullOrWhiteSpace(displayName) ? "(Unknown)" : displayName;

                        // date/time - Load these FIRST before loading dentist combo
                        if (rdr["appointment_date"] != DBNull.Value)
                        {
                            DateTime dt = Convert.ToDateTime(rdr["appointment_date"]);
                            dateTimePicker1.Value = dt.Date;
                        }

                        if (rdr["appointment_time"] != DBNull.Value)
                        {
                            TimeSpan ts;
                            var timeObj = rdr["appointment_time"];
                            if (timeObj is TimeSpan)
                                ts = (TimeSpan)timeObj;
                            else
                                ts = TimeSpan.Parse(rdr["appointment_time"].ToString());

                            dateTimePicker2.Value = dateTimePicker1.Value.Date + ts;
                        }

                        // Now reload dentists based on the appointment date (this will filter by day of week)
                        int dentistIdToSelect = rdr["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr["dentist_id"]) : -1;
                        LoadDentistsIntoComboBox();

                        // dentist - select after reloading
                        if (dentistIdToSelect > 0)
                        {
                            var item = comboBox1.Items.OfType<ServiceItem>().FirstOrDefault(x => x.Id == dentistIdToSelect);
                            if (item != null)
                                comboBox1.SelectedItem = item;
                        }

                        // staff 1 and 2
                        if (rdr["staff_assign_1"] != DBNull.Value)
                        {
                            int s1 = Convert.ToInt32(rdr["staff_assign_1"]);
                            var item = cmbGender.Items.OfType<ServiceItem>().FirstOrDefault(x => x.Id == s1);
                            if (item != null)
                                cmbGender.SelectedItem = item;
                        }

                        if (rdr["staff_assign_2"] != DBNull.Value)
                        {
                            int s2 = Convert.ToInt32(rdr["staff_assign_2"]);
                            var item = textBox1.Items.OfType<ServiceItem>().FirstOrDefault(x => x.Id == s2);
                            if (item != null)
                                textBox1.SelectedItem = item;
                        }

                        // notes
                        textBox2.Text = rdr["notes"] != DBNull.Value ? rdr["notes"].ToString() : string.Empty;
                    }
                }

                // mark checked services for this appointment if linking table exists
                if (TableExists("appointments_services"))
                {
                    const string ssql = "SELECT service_id FROM appointments_services WHERE appointment_id = @id";
                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand(ssql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", appointmentId);
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            var ids = new HashSet<int>();
                            while (rdr.Read())
                                ids.Add(Convert.ToInt32(rdr["service_id"]));

                            for (int i = 0; i < checkedListBoxServices.Items.Count; i++)
                            {
                                var si = checkedListBoxServices.Items[i] as ServiceItem;
                                if (si != null && ids.Contains(si.Id))
                                    checkedListBoxServices.SetItemChecked(i, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Save changes (UPDATE) using controls and patient id stored in label7.Tag (read-only)
        private void Button1_Click(object sender, EventArgs e)
        {
            // patient id stored in label7.Tag (read-only)
            int patientId = label7.Tag != null ? Convert.ToInt32(label7.Tag) : -1;
            if (patientId <= 0)
            {
                MessageBox.Show("Patient information is missing or invalid.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // dentist
            var dentistItem = comboBox1.SelectedItem as ServiceItem;
            if (dentistItem == null || dentistItem.Id <= 0)
            {
                MessageBox.Show("Please select a dentist.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // staff (optional)
            var staff1Item = cmbGender.SelectedItem as ServiceItem;
            var staff2Item = textBox1.SelectedItem as ServiceItem;
            int? staff1Id = (staff1Item != null && staff1Item.Id > 0) ? staff1Item.Id : (int?)null;
            int? staff2Id = (staff2Item != null && staff2Item.Id > 0) ? staff2Item.Id : (int?)null;

            // services (multiple)
            var selectedServiceItems = checkedListBoxServices.CheckedItems
                .OfType<ServiceItem>()
                .ToList();

            if (selectedServiceItems.Count == 0)
            {
                MessageBox.Show("Please select at least one service.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // appointment date/time
            DateTime apptDate = dateTimePicker1.Value.Date;
            TimeSpan apptTime = dateTimePicker2.Value.TimeOfDay;

            string notes = string.IsNullOrWhiteSpace(this.textBox2.Text) ? null : this.textBox2.Text.Trim();

            var serviceIds = selectedServiceItems.Select(si => si.Id).ToList();
            var serviceIdForColumn = serviceIds.FirstOrDefault();

            // determine appointment id from form state: should be set in Tag by caller/constructor
            if (!(this.Tag is int) || ((int)this.Tag) <= 0)
            {
                MessageBox.Show("Internal error: appointment id is not set for this editor.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int appointmentId = (int)this.Tag;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        bool hasPriceColumn = ColumnExists("appointments", "price");

                        // compute total price if column exists
                        decimal? totalPrice = null;
                        if (hasPriceColumn && serviceIds.Any())
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

                        // update main appointment row
                        var updateSql = new StringBuilder();
                        updateSql.Append("UPDATE appointments SET ");
                        updateSql.Append("patient_id = @patient_id, ");
                        updateSql.Append("dentist_id = @dentist_id, ");
                        updateSql.Append("service_id = @service_id, ");
                        updateSql.Append("appointment_date = @appointment_date, ");
                        updateSql.Append("appointment_time = @appointment_time, ");
                        updateSql.Append("staff_assign_1 = @staff1, ");
                        updateSql.Append("staff_assign_2 = @staff2, ");
                        updateSql.Append("notes = @notes");
                        if (hasPriceColumn)
                            updateSql.Append(", price = @price");
                        updateSql.Append(" WHERE appointment_id = @id");

                        using (var cmd = new SqlCommand(updateSql.ToString(), conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@patient_id", patientId);
                            cmd.Parameters.AddWithValue("@dentist_id", dentistItem.Id);
                            if (serviceIdForColumn > 0)
                                cmd.Parameters.AddWithValue("@service_id", serviceIdForColumn);
                            else
                                cmd.Parameters.AddWithValue("@service_id", DBNull.Value);

                            cmd.Parameters.Add("@appointment_date", SqlDbType.Date).Value = apptDate;
                            cmd.Parameters.Add("@appointment_time", SqlDbType.Time).Value = apptTime;
                            cmd.Parameters.AddWithValue("@staff1", staff1Id.HasValue ? (object)staff1Id.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@staff2", staff2Id.HasValue ? (object)staff2Id.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);

                            if (hasPriceColumn)
                            {
                                if (totalPrice.HasValue)
                                {
                                    var p = cmd.Parameters.Add("@price", SqlDbType.Decimal);
                                    p.Value = totalPrice.Value;
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@price", DBNull.Value);
                                }
                            }

                            cmd.Parameters.AddWithValue("@id", appointmentId);
                            cmd.ExecuteNonQuery();
                        }

                        // update linking table if it exists
                        if (TableExists("appointments_services"))
                        {
                            // delete existing
                            using (var del = new SqlCommand("DELETE FROM appointments_services WHERE appointment_id = @id", conn, tran))
                            {
                                del.Parameters.AddWithValue("@id", appointmentId);
                                del.ExecuteNonQuery();
                            }

                            // insert selected
                            if (serviceIds.Any())
                            {
                                using (var linkCmd = new SqlCommand("INSERT INTO appointments_services (appointment_id, service_id) VALUES (@aid, @sid)", conn, tran))
                                {
                                    linkCmd.Parameters.Add("@aid", SqlDbType.Int).Value = appointmentId;
                                    linkCmd.Parameters.Add("@sid", SqlDbType.Int);

                                    foreach (var sid in serviceIds)
                                    {
                                        linkCmd.Parameters["@sid"].Value = sid;
                                        linkCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tran.Commit();

                        // Log activity
                        try
                        {
                            DateTime dt = apptDate.Date + apptTime;
                            ActivityLogger.Log($"Admin updated an appointment for {label7.Text} on {dt:h:mm tt MMMM d, yyyy}");
                        }
                        catch { /* ignore logging errors */ }

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        MessageBox.Show("Error updating appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel? Unsaved information will be lost.",
                "Confirm Cancel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Close();
            }
        }
    }
}