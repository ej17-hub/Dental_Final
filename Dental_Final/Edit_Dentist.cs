using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Edit_Dentist : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

        private readonly int _dentistId;

        public Edit_Dentist(int dentistId)
        {
            InitializeComponent();
            _dentistId = dentistId;
            LoadDentistData();
        }

        private void LoadDentistData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM dentists WHERE dentist_id = @DentistId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DentistId", _dentistId);

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Assuming same control names as Add_Dentist
                                txtFirstName.Text = reader["first_name"].ToString();
                                txtLastName.Text = reader["last_name"].ToString();
                                txtMiddleInitial.Text = reader["middle_initial"].ToString();
                                txtSuffix.Text = reader["suffix"].ToString();
                                txtEmail.Text = reader["email"].ToString();
                                txtPhone.Text = reader["phone"].ToString();
                                cmbGender.Text = reader["gender"].ToString();
                                textBox1.Text = reader["specialization"].ToString();
                                txtAddress.Text = reader["bio"].ToString();

                                if (reader["birthdate"] != DBNull.Value)
                                    dtpBirthDate.Value = Convert.ToDateTime(reader["birthdate"]);

                                // Handle available days
                                string availableDays = reader["available_days"].ToString();
                                if (!string.IsNullOrEmpty(availableDays))
                                {
                                    string[] days = availableDays.Split(',');
                                    for (int i = 0; i < checkedListBox1.Items.Count; i++)
                                    {
                                        if (days.Contains(checkedListBox1.Items[i].ToString()))
                                            checkedListBox1.SetItemChecked(i, true);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading dentist data: " + ex.Message);
                    }
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {

            // Collect availability days
            var days = checkedListBox1.CheckedItems.Cast<string>().ToArray();
            string availableDays = days.Length > 0 ? string.Join(",", days) : null;

            string query = @"UPDATE dentists SET 
                first_name = @FirstName,
                last_name = @LastName,
                middle_initial = @MiddleInitial,
                suffix = @Suffix,
                email = @Email,
                phone = @Phone,
                gender = @Gender,
                specialization = @Specialization,
                bio = @Bio,
                available_days = @AvailableDays,
                birthdate = @Birthdate
                WHERE dentist_id = @DentistId";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                // Add parameters similar to Add_Dentist save method
                cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                cmd.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                cmd.Parameters.AddWithValue("@MiddleInitial",
                    string.IsNullOrWhiteSpace(txtMiddleInitial.Text) ? DBNull.Value : (object)txtMiddleInitial.Text.Trim());
                cmd.Parameters.AddWithValue("@Suffix",
                    string.IsNullOrWhiteSpace(txtSuffix.Text) ? DBNull.Value : (object)txtSuffix.Text.Trim());
                cmd.Parameters.AddWithValue("@Email",
                    string.IsNullOrWhiteSpace(txtEmail.Text) ? DBNull.Value : (object)txtEmail.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone",
                    string.IsNullOrWhiteSpace(txtPhone.Text) ? DBNull.Value : (object)txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@Gender",
                    string.IsNullOrWhiteSpace(cmbGender.Text) ? DBNull.Value : (object)cmbGender.Text);
                cmd.Parameters.AddWithValue("@Specialization",
                    string.IsNullOrWhiteSpace(textBox1.Text) ? DBNull.Value : (object)textBox1.Text.Trim());
                cmd.Parameters.AddWithValue("@Bio",
                    string.IsNullOrWhiteSpace(txtAddress.Text) ? DBNull.Value : (object)txtAddress.Text.Trim());
                cmd.Parameters.AddWithValue("@AvailableDays",
                    string.IsNullOrWhiteSpace(availableDays) ? DBNull.Value : (object)availableDays);
                cmd.Parameters.AddWithValue("@Birthdate", dtpBirthDate.Value);
                cmd.Parameters.AddWithValue("@DentistId", _dentistId);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Dentist updated successfully.");

                    // Log activity (safe swallow)
                    try
                    {
                        var fullName = (txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim()).Trim();
                        ActivityLogger.Log($"Admin updated dentist '{fullName}'");
                    }
                    catch { }

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating dentist: " + ex.Message);
                }
            }

        }
    }
}