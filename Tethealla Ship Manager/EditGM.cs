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
    public partial class EditGM : Form
    {
        public EditGM()
        {
            InitializeComponent();
        }

        // Cancel button - close the form.
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // "OK" button handler; change the GM's info and update the listbox, then return to our main form. Give an error if the new GM exists already.
        private void button2_Click(object sender, EventArgs e)
        {
            if (txtChangeGM.Text != string.Empty)
            {
                if (((MainForm)Owner).lstGM.Items.Contains(txtChangeGM.Text) == false)
                {
                    int selected_idx = ((MainForm)Owner).lstGM.SelectedIndex;
                    string saved_permissions = MainForm.GM_info[((MainForm)Owner).lstGM.SelectedItem.ToString()];

                    MainForm.GM_info.Remove(((MainForm)Owner).lstGM.SelectedItem.ToString());
                    ((MainForm)Owner).lstGM.Items.RemoveAt(selected_idx);

                    ((MainForm)Owner).lstGM.Items.Insert(selected_idx, txtChangeGM.Text);
                    MainForm.GM_info.Add(txtChangeGM.Text, saved_permissions);

                    ((MainForm)Owner).lstGM.SelectedIndex = selected_idx;

                    this.Close();
                }
                else MessageBox.Show("The GM '" + txtChangeGM.Text + "' already exists.", "Edit GM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditGM_Load(object sender, EventArgs e)
        {
            txtChangeGM.Text = ((MainForm)Owner).lstGM.SelectedItem.ToString();
            this.ActiveControl = txtChangeGM;
        }

        // Only allow numeric values for GC#s
        private void txtChangeGM_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
