using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Edit_Staff : Form
    {
        private readonly string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
        private readonly int staffId;

        public Edit_Staff(int staffId)
        {
            InitializeComponent();
            this.staffId = staffId;
            LoadStaffData();
        }

        private void LoadStaffData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM staff WHERE staff_id = @StaffId", conn))
                    {
                        cmd.Parameters.AddWithValue("@StaffId", staffId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFirstName.Text = reader["first_name"].ToString();
                                txtLastName.Text = reader["last_name"].ToString();
                                txtMiddleInitial.Text = reader["middle_initial"].ToString();
                                txtSuffix.Text = reader["suffix"].ToString();
                                cbGender.Text = reader["gender"].ToString();
                                txtEmail.Text = reader["email"].ToString();
                                if (reader["birth_date"] != DBNull.Value)
                                {
                                    dtpBirthDate.Value = Convert.ToDateTime(reader["birth_date"]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading staff data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string updateQuery = @"
                        UPDATE staff 
                        SET first_name = @FirstName,
                            last_name = @LastName,
                            middle_initial = @MiddleInitial,
                            suffix = @Suffix,
                            gender = @Gender,
                            email = @Email,
                            birth_date = @BirthDate
                        WHERE staff_id = @StaffId";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@StaffId", staffId);
                            cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text);
                            cmd.Parameters.AddWithValue("@LastName", txtLastName.Text);
                            cmd.Parameters.AddWithValue("@MiddleInitial", string.IsNullOrEmpty(txtMiddleInitial.Text) ? (object)DBNull.Value : txtMiddleInitial.Text);
                            cmd.Parameters.AddWithValue("@Suffix", string.IsNullOrEmpty(txtSuffix.Text) ? (object)DBNull.Value : txtSuffix.Text);
                            cmd.Parameters.AddWithValue("@Gender", string.IsNullOrEmpty(cbGender.Text) ? (object)DBNull.Value : cbGender.Text);
                            cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text);
                            cmd.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);

                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Staff information updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating staff information: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
