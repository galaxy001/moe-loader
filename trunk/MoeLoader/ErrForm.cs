using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MoeLoader
{
    public partial class ErrForm : Form
    {
        public ErrForm(string err)
        {
            InitializeComponent();

            textBox1.Text = err;
            pictureBox1.Image = System.Drawing.SystemIcons.Error.ToBitmap();
        }
    }
}
