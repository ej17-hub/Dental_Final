using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final.Admin
{
    public partial class Approve_Appointment : Form
    {
        // match other files' connection string
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        // store service names passed in so we can insert linking rows
        private readonly List<string> _serviceNames = new List<string>();

        // store request id so we can mark the request Approved after inserting appointment
        private int? _requestId;

        public Approve_Appointment()
        {
            InitializeComponent();
        }

        // New constructor to populate labels when opened from Pending_Appointments (no request id)
        public Approve_Appointment(string patientFullName, IEnumerable<string> services, DateTime requestedDate, string requestedTime)
            : this()
        {
            InitializeForRequest(null, patientFullName, services, requestedDate, requestedTime);
        }

        // Overload that accepts request id (preferred when opened from Pending_Appointments)
        public Approve_Appointment(int requestId, string patientFullName, IEnumerable<string> services, DateTime requestedDate, string requestedTime)
            : this()
        {
            InitializeForRequest(requestId, patientFullName, services, requestedDate, requestedTime);
        }

        private void InitializeForRequest(int? requestId, string patientFullName, IEnumerable<string> services, DateTime requestedDate, string requestedTime)
        {
            _requestId = requestId;

            // Patient full name
            label7.Text = patientFullName ?? string.Empty;

            // Services — display vertically if two or more, otherwise single line
            var serviceList = (services ?? Enumerable.Empty<string>()).Select(s => s?.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (serviceList.Any())
            {
                if (serviceList.Count >= 2)
                    label9.Text = string.Join(Environment.NewLine, serviceList);
                else
                    label9.Text = serviceList.First();
            }
            else
            {
                label9.Text = string.Empty;
            }

            // persist for later DB work
            _serviceNames.Clear();
            _serviceNames.AddRange(serviceList);

            // Date
            label10.Text = (requestedDate == DateTime.MinValue) ? string.Empty : requestedDate.ToString("MMMM d, yyyy");

            // Time (already formatted by caller)
            label11.Text = requestedTime ?? string.Empty;

            // populate dentists and staff comboboxes
            LoadStaffCombos();
            LoadDentistsForServices(serviceList);
        }

        // small helper used for ComboBox items so the displayed text is the name but we can keep the id
        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name ?? string.Empty;
        }

        private void LoadStaffCombos()
        {
            try
            {
                comboBox2.Items.Clear();
                comboBox3.Items.Clear();

                const string sql = @"
                    SELECT staff_id, first_name, middle_initial, last_name, ISNULL(suffix,'') AS suffix
                    FROM staff
                    ORDER BY last_name, first_name;";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int id = rdr["staff_id"] != DBNull.Value ? Convert.ToInt32(rdr["staff_id"]) : 0;
                            var first = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString().Trim() : string.Empty;
                            var mi = rdr["middle_initial"] != DBNull.Value ? rdr["middle_initial"].ToString().Trim() : string.Empty;
                            var last = rdr["last_name"] != DBNull.Value ? rdr["last_name"].ToString().Trim() : string.Empty;
                            var suffix = rdr["suffix"] != DBNull.Value ? rdr["suffix"].ToString().Trim() : string.Empty;

                            string display;
                            if (!string.IsNullOrEmpty(mi))
                                display = $"{first} {mi} {last}" + (string.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix);
                            else
                                display = $"{first} {last}" + (string.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix);

                            var item = new ComboItem { Id = id, Name = display };
                            comboBox2.Items.Add(item);
                            comboBox3.Items.Add(new ComboItem { Id = id, Name = display });
                        }
                    }
                }

                // keep no selection by default
                comboBox2.SelectedIndex = -1;
                comboBox3.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load staff: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Populate dentist combo filtered by service categories derived from provided service names.
        // If service names map to a single category we filter by that category; if multiple categories
        // we load dentists whose specialization matches any of those categories. If no matching dentists
        // are found we fall back to loading all dentists.
        private void LoadDentistsForServices(IList<string> serviceNames)
        {
            try
            {
                comboBox1.Items.Clear();

                // If we have no service names, load no dentists (leave combo empty) — this keeps user explicit selection.
                if (serviceNames == null || !serviceNames.Any())
                    return;

                // Step 1: get distinct categories for the provided service names (use parameterized IN)
                var categories = new List<string>();
                var paramNames = new List<string>();
                for (int i = 0; i < serviceNames.Count; i++)
                    paramNames.Add("@s" + i);

                var catSql = $"SELECT DISTINCT LTRIM(RTRIM(COALESCE(category, specialization, ''))) AS cat FROM services WHERE name IN ({string.Join(",", paramNames)})";

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(catSql, conn))
                {
                    for (int i = 0; i < serviceNames.Count; i++)
                        cmd.Parameters.AddWithValue(paramNames[i], serviceNames[i]);

                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var c = rdr["cat"] != DBNull.Value ? rdr["cat"].ToString().Trim() : string.Empty;
                            if (!string.IsNullOrEmpty(c) && !categories.Contains(c))
                                categories.Add(c);
                        }
                    }

                    // Step 2: build dentist query based on categories
                    List<ComboItem> dentists = new List<ComboItem>();
                    if (categories.Any())
                    {
                        // parameterize categories
                        var catParams = new List<string>();
                        for (int i = 0; i < categories.Count; i++)
                        {
                            catParams.Add("@c" + i);
                        }

                        var dentistSql = $@"
                            SELECT dentist_id, first_name, middle_initial, last_name, ISNULL(suffix,'') AS suffix
                            FROM dentists
                            WHERE LTRIM(RTRIM(ISNULL(specialization,''))) IN ({string.Join(",", catParams)})
                            ORDER BY last_name, first_name;";

                        using (var cmd2 = new SqlCommand(dentistSql, conn))
                        {
                            for (int i = 0; i < categories.Count; i++)
                                cmd2.Parameters.AddWithValue(catParams[i], categories[i]);

                            using (var rdr2 = cmd2.ExecuteReader())
                            {
                                while (rdr2.Read())
                                {
                                    int id = rdr2["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr2["dentist_id"]) : 0;
                                    var first = rdr2["first_name"] != DBNull.Value ? rdr2["first_name"].ToString().Trim() : string.Empty;
                                    var mi = rdr2["middle_initial"] != DBNull.Value ? rdr2["middle_initial"].ToString().Trim() : string.Empty;
                                    var last = rdr2["last_name"] != DBNull.Value ? rdr2["last_name"].ToString().Trim() : string.Empty;
                                    var suffix = rdr2["suffix"] != DBNull.Value ? rdr2["suffix"].ToString().Trim() : string.Empty;

                                    string display = !string.IsNullOrEmpty(mi) ? $"{first} {mi} {last}" : $"{first} {last}";
                                    if (!string.IsNullOrEmpty(suffix)) display += " " + suffix;

                                    dentists.Add(new ComboItem { Id = id, Name = display });
                                }
                            }
                        }
                    }

                    // If no dentists found for category or no category discovered, try loading all dentists as fallback
                    if (!dentists.Any())
                    {
                        const string allDentistsSql = @"
                            SELECT dentist_id, first_name, middle_initial, last_name, ISNULL(suffix,'') AS suffix
                            FROM dentists
                            ORDER BY last_name, first_name;";

                        using (var cmd3 = new SqlCommand(allDentistsSql, conn))
                        using (var rdr3 = cmd3.ExecuteReader())
                        {
                            while (rdr3.Read())
                            {
                                int id = rdr3["dentist_id"] != DBNull.Value ? Convert.ToInt32(rdr3["dentist_id"]) : 0;
                                var first = rdr3["first_name"] != DBNull.Value ? rdr3["first_name"].ToString().Trim() : string.Empty;
                                var mi = rdr3["middle_initial"] != DBNull.Value ? rdr3["middle_initial"].ToString().Trim() : string.Empty;
                                var last = rdr3["last_name"] != DBNull.Value ? rdr3["last_name"].ToString().Trim() : string.Empty;
                                var suffix = rdr3["suffix"] != DBNull.Value ? rdr3["suffix"].ToString().Trim() : string.Empty;

                                string display = !string.IsNullOrEmpty(mi) ? $"{first} {mi} {last}" : $"{first} {last}";
                                if (!string.IsNullOrEmpty(suffix)) display += " " + suffix;

                                dentists.Add(new ComboItem { Id = id, Name = display });
                            }
                        }
                    }

                    // populate comboBox1 with dentist items
                    foreach (var d in dentists)
                        comboBox1.Items.Add(d);

                    // keep no selection by default
                    comboBox1.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load dentists: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // utility checks (copied from Add_Appointment.cs style)
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

        private void button1_Click(object sender, EventArgs e)
        {
            // This mirrors Add_Appointment.button1_Click behaviour and inserts an appointment record
            try
            {
                // resolve patient id from label7 (best-effort)
                int patientId = -1;
                var patientFull = (label7?.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(patientFull))
                {
                    MessageBox.Show("Patient not specified.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Build SQL depending on whether patients.name column exists to avoid "Invalid column name 'name'"
                string patientSql;
                if (ColumnExists("patients", "name"))
                {
                    patientSql = @"
        SELECT TOP 1 patient_id
        FROM patients
        WHERE RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(middle_initial,'')) + ' ' + RTRIM(ISNULL(last_name,'')) = @full
           OR RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) = @full
           OR RTRIM(ISNULL(name,'')) = @full";
                }
                else
                {
                    patientSql = @"
        SELECT TOP 1 patient_id
        FROM patients
        WHERE RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(middle_initial,'')) + ' ' + RTRIM(ISNULL(last_name,'')) = @full
           OR RTRIM(ISNULL(first_name,'')) + ' ' + RTRIM(ISNULL(last_name,'')) = @full";
                }

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(patientSql, conn))
                {
                    cmd.Parameters.AddWithValue("@full", patientFull);
                    conn.Open();
                    var idObj = cmd.ExecuteScalar();
                    if (idObj != null && idObj != DBNull.Value)
                        patientId = Convert.ToInt32(idObj);
                }

                if (patientId <= 0)
                {
                    // try looser match by splitting tokens (first + last)
                    var parts = patientFull.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var first = parts[0];
                        var last = parts[parts.Length - 1];
                        using (var conn = new SqlConnection(connectionString))
                        using (var cmd = new SqlCommand(@"
                            SELECT TOP 1 patient_id FROM patients
                            WHERE (RTRIM(ISNULL(first_name,'')) = @first AND RTRIM(ISNULL(last_name,'')) = @last)
                               OR (RTRIM(ISNULL(first_name,'')) = @first AND RTRIM(ISNULL(last_name,'')) LIKE @lastLike)
                            ", conn))
                        {
                            cmd.Parameters.AddWithValue("@first", first);
                            cmd.Parameters.AddWithValue("@last", last);
                            cmd.Parameters.AddWithValue("@lastLike", last + "%");
                            conn.Open();
                            var idObj = cmd.ExecuteScalar();
                            if (idObj != null && idObj != DBNull.Value)
                                patientId = Convert.ToInt32(idObj);
                        }
                    }
                }

                if (patientId <= 0)
                {
                    MessageBox.Show("Unable to resolve patient id from displayed name. Please ensure patient exists in the database.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Dentist
                var dentistItem = comboBox1.SelectedItem as ComboItem;
                if (dentistItem == null || dentistItem.Id <= 0)
                {
                    MessageBox.Show("Please select a dentist.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int dentistId = dentistItem.Id;

                // Staff
                var staff1Item = comboBox2.SelectedItem as ComboItem;
                var staff2Item = comboBox3.SelectedItem as ComboItem;
                int? staff1Id = (staff1Item != null && staff1Item.Id > 0) ? staff1Item.Id : (int?)null;
                int? staff2Id = (staff2Item != null && staff2Item.Id > 0) ? staff2Item.Id : (int?)null;

                // Appointment date/time parsed from labels (label10 = date, label11 = time). Prefer reading controls if present.
                DateTime apptDate = DateTime.MinValue;
                TimeSpan apptTime = TimeSpan.Zero;
                if (DateTime.TryParse(label10.Text, out var parsedDate))
                    apptDate = parsedDate.Date;
                // Try to parse label11 which is time string like "4:00 PM"
                if (TimeSpan.TryParse(label11.Text, out var parsedTs))
                    apptTime = parsedTs;
                else if (DateTime.TryParse(label11.Text, out var parsedDt))
                    apptTime = parsedDt.TimeOfDay;
                // fallback to today if missing
                if (apptDate == DateTime.MinValue)
                    apptDate = DateTime.Today;

                // Notes - use textBox2 if present (designer has textBox2)
                string notes = null;
                if (this.textBox2 != null)
                    notes = string.IsNullOrWhiteSpace(this.textBox2.Text) ? null : this.textBox2.Text.Trim();

                // Resolve service ids from stored _serviceNames
                var serviceIds = new List<int>();
                var servicePrices = new Dictionary<int, decimal?>();

                if (_serviceNames.Any())
                {
                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        var paramNames = new List<string>();
                        for (int i = 0; i < _serviceNames.Count; i++)
                        {
                            var p = "@s" + i;
                            paramNames.Add(p);
                            cmd.Parameters.AddWithValue(p, _serviceNames[i]);
                        }
                        cmd.CommandText = $"SELECT service_id, price FROM services WHERE name IN ({string.Join(",", paramNames)})";
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int sid = rdr["service_id"] != DBNull.Value ? Convert.ToInt32(rdr["service_id"]) : -1;
                                decimal? price = rdr["price"] != DBNull.Value ? (decimal?)Convert.ToDecimal(rdr["price"]) : null;
                                if (sid > 0)
                                {
                                    serviceIds.Add(sid);
                                    servicePrices[sid] = price;
                                }
                            }
                        }
                    }
                }

                // compute total price if any
                decimal? totalPrice = null;
                if (serviceIds.Any())
                {
                    var prices = serviceIds.Select(id => servicePrices.ContainsKey(id) ? servicePrices[id] : (decimal?)null).Where(p => p.HasValue).Select(p => p.Value).ToList();
                    if (prices.Any())
                        totalPrice = prices.Sum();
                }

                // Insert appointment similar to Add_Appointment.cs
                bool hasPriceColumn = ColumnExists("appointments", "price");
                int newAppointmentId = -1;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            int? serviceIdForColumn = serviceIds.FirstOrDefault();

                            string insertSql;
                            if (hasPriceColumn)
                            {
                                insertSql = @"
                    INSERT INTO appointments
                        (patient_id, dentist_id, service_id, appointment_date, appointment_time, staff_assign_1, staff_assign_2, notes, price, created_at)
                    VALUES
                        (@patient_id, @dentist_id, @service_id, @appointment_date, @appointment_time, @staff1, @staff2, @notes, @price, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS int);";
                            }
                            else
                            {
                                insertSql = @"
                    INSERT INTO appointments
                        (patient_id, dentist_id, service_id, appointment_date, appointment_time, staff_assign_1, staff_assign_2, notes, created_at)
                    VALUES
                        (@patient_id, @dentist_id, @service_id, @appointment_date, @appointment_time, @staff1, @staff2, @notes, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS int);";
                            }

                            using (var cmd = new SqlCommand(insertSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@patient_id", patientId);
                                cmd.Parameters.AddWithValue("@dentist_id", dentistId);
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

                            // link services if linking table exists
                            if (TableExists("appointments_services") && serviceIds.Any())
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

                            // Mark the original request as Approved (if request id provided) — do this inside the same transaction
                            if (_requestId.HasValue)
                            {
                                using (var upd = new SqlCommand("UPDATE appointment_requests SET status = @status, notes = ISNULL(notes,'') + @note WHERE request_id = @id", conn, tran))
                                {
                                    upd.Parameters.AddWithValue("@status", "Approved");
                                    upd.Parameters.AddWithValue("@note", " [Approved]");
                                    upd.Parameters.AddWithValue("@id", _requestId.Value);
                                    upd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();

                            MessageBox.Show("Appointment approved and added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            MessageBox.Show("Failed to save appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create appointment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}