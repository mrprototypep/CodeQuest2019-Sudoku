using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>
/// This code is a mess. I'm not going to comment it because it serves no purpose other than to show graphically what the program is doing. I'm sorry anyone outside of Vanguard's robotics team who might be looking at this. Hopefully you can read it without comments.
/// Assuming you're on Vanguard's Robotics team, I told you not to try and look at this, so don't. Not yet at least.
/// </summary>
namespace Sudoku
{
    public partial class Form1 : Form
    {
        Label[,] labels;

        public Form1()
        {
            InitializeComponent();

            labels = new Label[9, 9];
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    labels[x, y] = new Label
                    {
                        Height = 30,
                        Width = 30,
                        Location = new Point(80 + x * 32, 80 + y * 32)
                    };

                    if (x / 3 == 1 && y / 3 != 1)
                    {
                        labels[x, y].BackColor = Color.Gray;
                    } else if (y / 3 == 1 && x / 3 != 1)
                        labels[x, y].BackColor = Color.Gray;
                    else
                        labels[x, y].BackColor = Color.LightGray;
                    Controls.Add(labels[x, y]);
                    labels[x, y].Show();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Refresh();
        }

        public void UpdateTable(Program.SudokuTable table, bool calibrate = false)
        {
            if (calibrate)
            {
                foreach (Label l in labels)
                {
                    l.Font = new Font(l.Font, FontStyle.Regular);
                }
            }

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    if (table.table[x, y] != null)
                        labels[x, y].Text = table.table[x, y].ToString();
                    else
                        labels[x, y].Text = "_";

                    if (calibrate && table.knownCorrect[x, y])
                    {
                        labels[x, y].Font = new Font(labels[x, y].Font, FontStyle.Bold);
                    }
                }
            }

            Update();
        }
    }
}
