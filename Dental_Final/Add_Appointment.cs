﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Add_Appointment : Form
    {
        // 💡 Adjust this or use your config file
        private string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";


        public Add_Appointment()
        {
            InitializeComponent();

            // load lookups
            LoadPatientsIntoComboBox();
            LoadDentistsIntoComboBox(); // loads based on the current date picker value
            LoadStaffIntoComboBoxes();

            // services checkbox list
            LoadServicesIntoCheckedListBox();

            this.button2.Click -= Button2_Click;
            this.button2.Click += Button2_Click;

            // Re-load dentists when the appointment date changes so only available dentists show
            if (this.dateTimePicker1 != null)
            {
                this.dateTimePicker1.ValueChanged -= DateTimePicker1_ValueChanged;
                this.dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
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
                // fallback to id if no name columns exist
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
                        comboBox2.Items.Clear(); // comboBox2 is Patient in designer
                        while (rdr.Read())
                        {
                            var id = rdr["patient_id"] != DBNull.Value ? Convert.ToInt32(rdr["patient_id"]) : -1;
                            var name = rdr["display_name"] != DBNull.Value ? rdr["display_name"].ToString() : string.Empty;
                            comboBox2.Items.Add(new ServiceItem(id, name));
                        }
                    }
                }

                comboBox2.DisplayMember = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDentistsIntoComboBox()
        {
            // Determine selected day name from date picker (falls back to today if control missing)
            string dayName = (this.dateTimePicker1 != null) ? this.dateTimePicker1.Value.DayOfWeek.ToString() : DateTime.Today.DayOfWeek.ToString();

            // Build base select depending on available name columns
            string selectBase;
            string orderBy;
            if (ColumnExists("dentists", "first_name") && ColumnExists("dentists", "last_name"))
            {
                selectBase = "SELECT dentist_id, RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) AS display_name FROM dentists";
                orderBy = " ORDER BY last_name, first_name";
            }
            else if (ColumnExists("dentists", "name"))
            {
                selectBase = "SELECT dentist_id, RTRIM(ISNULL(name,'')) AS display_name FROM dentists";
                orderBy = " ORDER BY name";
            }
            else
            {
                selectBase = "SELECT dentist_id, CONVERT(varchar(20), dentist_id) AS display_name FROM dentists";
                orderBy = " ORDER BY dentist_id";
            }

            // If dentists table has available_days column, filter dentists by the dayName
            bool hasAvailableDays = ColumnExists("dentists", "available_days");
            string finalSql = selectBase;
            if (hasAvailableDays)
            {
                // Use CHARINDEX to find the day token; assumes available_days contains day names (e.g. 'Monday, Tuesday')
                finalSql += " WHERE CHARINDEX(@day, available_days) > 0";
            }
            finalSql += orderBy;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(finalSql, conn))
                {
                    if (hasAvailableDays)
                        cmd.Parameters.AddWithValue("@day", dayName);

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
                        // cmbGender = Staff 1, textBox1 = Staff 2 (both are ComboBox controls in designer)
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

                    checkedListBoxServices.Items.Clear(); // Clear old items if any

                    while (reader.Read())
                    {
                        // 💡 Store both ID and Name so you can retrieve ID later
                        var serviceId = reader.GetInt32(0);
                        var serviceName = reader.GetString(1);

                        // Wrap in custom object or anonymous type
                        checkedListBoxServices.Items.Add(new ServiceItem(serviceId, serviceName));
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading services: " + ex.Message);
                }
            }

            // So that only the name shows, not the object
            checkedListBoxServices.DisplayMember = "Name";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Patient
            var patientItem = comboBox2.SelectedItem as ServiceItem;
            if (patientItem == null || patientItem.Id <= 0)
            {
                MessageBox.Show("Please select a patient.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Dentist
            var dentistItem = comboBox1.SelectedItem as ServiceItem;
            if (dentistItem == null || dentistItem.Id <= 0)
            {
                MessageBox.Show("Please select a dentist.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Staff (optional)
            var staff1Item = cmbGender.SelectedItem as ServiceItem;
            var staff2Item = textBox1.SelectedItem as ServiceItem;
            int? staff1Id = (staff1Item != null && staff1Item.Id > 0) ? staff1Item.Id : (int?)null;
            int? staff2Id = (staff2Item != null && staff2Item.Id > 0) ? staff2Item.Id : (int?)null;

            // Services (multiple)
            var selectedServiceItems = checkedListBoxServices.CheckedItems
                .OfType<ServiceItem>()
                .ToList();

            if (selectedServiceItems.Count == 0)
            {
                MessageBox.Show("Please select at least one service.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Appointment date (from date picker) and time (from time picker)
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

            // NOTES: use the actual designer TextBox (textBox2) for notes
            // Designer shows a multiline TextBox named textBox2 for notes.
            string notes = null;
            if (this.textBox2 != null)
            {
                notes = string.IsNullOrWhiteSpace(this.textBox2.Text) ? null : this.textBox2.Text.Trim();
            }

            // collect service IDs and names
            var serviceIds = selectedServiceItems.Select(si => si.Id).ToList();
            var serviceNames = selectedServiceItems.Select(si => si.Name).ToList();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // compute total price for selected services
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

                        // Insert one appointment row. service_id column keeps first service for backward compatibility.
                        int? serviceIdForColumn = serviceIds.FirstOrDefault();

                        // include price column if it exists
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

                            newAppointmentId = (int)cmd.ExecuteScalar();
                        }

                        // If a linking table exists, insert rows mapping the appointment to each selected service.
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

                        // signal success to caller and close
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        MessageBox.Show("Error saving appointment(s): " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    // Small helper class to store ID and display name
    public class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ServiceItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name; // This ensures the name appears in the list
        }
    }
}