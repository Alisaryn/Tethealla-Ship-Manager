/*
    Tethealla Ship Manager
    Copyright 2022 Michelle Powers

    Icon assets from the Tethealla project (https://pioneer2.net) were used in this project.
    This project is reliant on Tethealla, but is not affiliated in any way with its developers.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tethealla_Ship_Settings_Editor
{
    public partial class AddGM : Form
    {
        public AddGM()
        {
            InitializeComponent();
        }

        // Cancel button - close the form
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        // "OK" button handler; add a GM only if it doesn't already exist in our list
        private void button2_Click(object sender, EventArgs e)
        {
            if (txtAddGM.Text != string.Empty)
            {
                if (((MainForm)Owner).lstGM.Items.Contains(txtAddGM.Text) == false) 
                {

                    int new_idx = ((MainForm)Owner).lstGM.Items.Add(txtAddGM.Text);
                    MainForm.GM_info.Add(txtAddGM.Text, "0");
                    ((MainForm)Owner).lstGM.SelectedIndex = new_idx;
                    this.Close();
                }
                else MessageBox.Show("The GM '" + txtAddGM.Text + "' already exists.", "Add GM", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void AddGM_Load(object sender, EventArgs e)
        {
            this.ActiveControl = txtAddGM;     
        }

        // Only allow numeric values for GC#s
        private void txtAddGM_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
