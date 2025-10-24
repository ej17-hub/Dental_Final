using System;
using System.Collections.Generic;
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
        private string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

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

            // load appointment values
            LoadAppointment(appointmentId);
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
            string select;
            if (ColumnExists("dentists", "first_name") && ColumnExists("dentists", "last_name"))
            {
                select = "SELECT dentist_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name FROM dentists ORDER BY last_name, first_name";
            }
            else if (ColumnExists("dentists", "name"))
            {
                select = "SELECT dentist_id, RTRIM(ISNULL(name,'')) AS display_name FROM dentists ORDER BY name";
            }
            else
            {
                select = "SELECT dentist_id, CONVERT(varchar(20), dentist_id) AS display_name FROM dentists ORDER BY dentist_id";
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(select, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        comboBox1.Items.Clear(); // comboBox1 is Dentist in designer
                        while (rdr.Read())
                        {
                            var id = rdr["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr["dentist_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;
                            comboBox1.Items.Add(new ServiceItem(id, name));
                        }
                    }
                }

                comboBox1.DisplayMember = "Name";
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

        private void LoadServicesIntoCheckedListBox()
        {
            string query = "SELECT service_id, name FROM services";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
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
                    MessageBox.Show("Error loading services: " + ex.Message);
                }
            }

            checkedListBoxServices.DisplayMember = "Name";
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

                        // dentist
                        if (rdr["dentist_id"] != DBNull.Value)
                        {
                            int did = Convert.ToInt32(rdr["dentist_id"]);
                            var item = comboBox1.Items.OfType<ServiceItem>().FirstOrDefault(x => x.Id == did);
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

                        // date/time
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
                            ActivityLogger.Log($"Admin updated an appointment for {label7.Text} on {dt:yyyy-MM-dd h:mm tt}");
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