using Renderer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonTest1_Click(object sender, EventArgs e)
        {
            var test = new Test1();
            pictureBox1.Image = test.Run();
        }

        private void buttonTest2_Click(object sender, EventArgs e)
        {
            var test = new Test2();
            pictureBox1.Image = test.Run();
        }
    }
}
