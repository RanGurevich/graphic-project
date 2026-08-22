using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenGL;
using System.Runtime.InteropServices; 

namespace myOpenGL
{
    public partial class Form1 : Form
    {
        cOGL cGL;

        public Form1()
        {
            InitializeComponent();
            cGL = new cOGL(panel1);
            panel1.MouseClick += panel1_MouseClick;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            cGL.DrawAll();
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            cGL.OnResize();
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            cGL.GetOpenGLCoords(e.X, e.Y);
        }
    }
}