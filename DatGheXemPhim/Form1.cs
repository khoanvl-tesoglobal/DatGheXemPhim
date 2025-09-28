using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DatGheXemPhim
{
    public partial class Form1 : Form
    {
        private HashSet<Button> selectedButtons = new HashSet<Button>();

        private const int PRICE_THUONG = 80000;
        private const int PRICE_VIP = 120000;
        private const int PRICE_SWEETBOX = 200000;

        private string connectionString = @"Data Source=localhost;Initial Catalog=CinemaDB;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
            txtThanhTien.ReadOnly = true;
            txtThanhTien.Text = FormatVND(0);
        }

        private string FormatVND(long amount)
        {
            return amount.ToString("N0") + " VNĐ";
        }

        private int GetRowIndex(char row)
        {
            switch (row)
            {
                case 'A': return 0;
                case 'B': return 1;
                case 'C': return 2;
                case 'D': return 3;
                case 'E': return 4;
                case 'F': return 5;
                case 'G': return 6;
                case 'H': return 7;
                case 'J': return 8;
                case 'K': return 9;
                default: return 0;
            }
        }

        // 🔹 Load ghế từ DB
        private void LoadSeats()
        {
            panelSeats.Controls.Clear();
            panelSeats.AutoScroll = true;

            int seatWidth = 34, seatHeight = 34, margin = 2, step = seatWidth + margin;
            int baseLeft = step;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Ghe", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string hang = reader["Hang"].ToString();
                    int soGhe = (int)reader["SoGhe"];
                    string loai = reader["Loai"].ToString();
                    string trangThai = reader["TrangThai"].ToString();

                    Button btn = new Button();
                    btn.Width = seatWidth;
                    btn.Height = seatHeight;
                    btn.Text = hang + soGhe;
                    btn.Name = $"btn_{hang}_{soGhe}";

                    // tính vị trí
                    int rowIndex = GetRowIndex(hang[0]);
                    int cols = (hang == "K") ? 15 : 13;
                    int offsetX = (hang == "K") ? baseLeft - step : baseLeft;

                    // ⚠️ ghế phải đánh số từ phải sang trái
                    btn.Left = offsetX + (cols - soGhe) * step;
                    btn.Top = rowIndex * (seatHeight + margin);

                    // Tag để lưu loại ghế + giá
                    int price = GetPriceByRow(hang[0]);
                    btn.Tag = new SeatInfo { Row = hang[0], Price = price, OriginalLabel = btn.Text };

                    // set màu theo trạng thái
                    if (trangThai == "Trong")
                    {
                        if (loai == "Thuong")
                        {
                            btn.BackColor = Color.White;
                            btn.ForeColor = Color.Black;
                            btn.FlatStyle = FlatStyle.Flat;
                            btn.FlatAppearance.BorderColor = Color.Green;
                            btn.FlatAppearance.BorderSize = 2;
                        }
                        else if (loai == "VIP")
                        {
                            btn.BackColor = Color.White;
                            btn.ForeColor = Color.Black;
                            btn.FlatStyle = FlatStyle.Flat;
                            btn.FlatAppearance.BorderColor = Color.Red;
                            btn.FlatAppearance.BorderSize = 2;
                        }
                        else if (loai == "SweetBox")
                        {
                            btn.BackColor = Color.HotPink;
                            btn.ForeColor = Color.White;
                        }
                    }
                    else if (trangThai == "DaChon")
                    {
                        btn.BackColor = Color.Red;
                        btn.ForeColor = Color.White;
                    }
                    else if (trangThai == "KhongTheChon")
                    {
                        btn.BackColor = Color.Gray;
                        btn.ForeColor = Color.Black;
                        btn.Enabled = false;
                    }

                    btn.Click += Seat_Click;
                    panelSeats.Controls.Add(btn);
                }
            }

            UpdateTotal();
        }

        private int GetPriceByRow(char row)
        {
            if (row >= 'A' && row <= 'D') return PRICE_THUONG;
            if (row >= 'E' && row <= 'J') return PRICE_VIP;
            if (row == 'K') return PRICE_SWEETBOX;
            return PRICE_THUONG;
        }

        // 🔹 Chọn ghế
        private void Seat_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string[] parts = btn.Name.Split('_'); // btn_A_5
            string hang = parts[1];
            int soGhe = int.Parse(parts[2]);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Ghe SET TrangThai = @TrangThai WHERE Hang = @Hang AND SoGhe = @SoGhe", conn);

                if (btn.BackColor == Color.White || btn.BackColor == Color.HotPink) // chọn
                {
                    btn.BackColor = Color.Red;
                    btn.ForeColor = Color.White;
                    cmd.Parameters.AddWithValue("@TrangThai", "DaChon");
                    selectedButtons.Add(btn);
                }
                else if (btn.BackColor == Color.Red) // bỏ chọn
                {
                    RestoreButtonAppearance(btn);
                    cmd.Parameters.AddWithValue("@TrangThai", "Trong");
                    selectedButtons.Remove(btn);
                }

                cmd.Parameters.AddWithValue("@Hang", hang);
                cmd.Parameters.AddWithValue("@SoGhe", soGhe);
                cmd.ExecuteNonQuery();
            }

            UpdateTotal();
        }

        private void UpdateTotal()
        {
            long total = 0;
            foreach (var btn in selectedButtons)
            {
                if (btn.Tag is SeatInfo si)
                    total += si.Price;
            }
            txtThanhTien.Text = FormatVND(total);
        }

        private void RestoreButtonAppearance(Button btn)
        {
            if (!(btn.Tag is SeatInfo si)) return;
            char row = si.Row;

            if (row >= 'A' && row <= 'D')
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.Black;
                btn.FlatAppearance.BorderColor = Color.Green;
                btn.FlatAppearance.BorderSize = 2;
                btn.Text = si.OriginalLabel;
            }
            else if (row >= 'E' && row <= 'J')
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.Black;
                btn.FlatAppearance.BorderColor = Color.Red;
                btn.FlatAppearance.BorderSize = 2;
                btn.Text = si.OriginalLabel;
            }
            else if (row == 'K')
            {
                btn.BackColor = Color.HotPink;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.Red;
                btn.FlatAppearance.BorderSize = 1;
                btn.Text = si.OriginalLabel;
            }
        }

        private void MarkAsUnavailable(Button btn)
        {
            if (btn == null) return;
            btn.BackColor = Color.Silver;
            btn.ForeColor = Color.Black;
            btn.FlatAppearance.BorderColor = Color.Gray;
            btn.FlatAppearance.BorderSize = 1;
            btn.Text = "X";
            btn.Enabled = false;
        }

        // 🔹 Thanh toán
        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedButtons.Count == 0)
            {
                MessageBox.Show("Bạn chưa chọn ghế nào.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long total = selectedButtons.Sum(btn => ((SeatInfo)btn.Tag).Price);

            var res = MessageBox.Show($"Tổng {FormatVND(total)}. Xác nhận thanh toán?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                foreach (var btn in selectedButtons.ToList())
                {
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Ghe SET TrangThai = 'KhongTheChon' WHERE Hang = @Hang AND SoGhe = @SoGhe", conn);
                    string[] parts = btn.Name.Split('_');
                    cmd.Parameters.AddWithValue("@Hang", parts[1]);
                    cmd.Parameters.AddWithValue("@SoGhe", int.Parse(parts[2]));
                    cmd.ExecuteNonQuery();

                    MarkAsUnavailable(btn);
                    selectedButtons.Remove(btn);
                }
            }

            UpdateTotal();
            MessageBox.Show("Thanh toán thành công!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSeats();
            this.btnThanhToan.Click -= button1_Click;
            this.btnThanhToan.Click += button1_Click;
        }

        // class phụ
        private class SeatInfo
        {
            public char Row { get; set; }
            public int Price { get; set; }
            public string OriginalLabel { get; set; }
        }
    }
}
