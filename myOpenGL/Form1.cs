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
        private Character character;
        public Form1()
        {
            InitializeComponent();
            cGL = new cOGL(panel1);
            panel1.MouseClick += panel1_MouseClick;
            character = new Character(0, 0, 0);
            cGL.SetCharacter(character);

            // חובה כדי שהטופס יתפוס את הלחיצות על המקלדת לפני פקדים אחרים (כמו הפאנל)
            this.KeyPreview = true;
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Y:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_SHOULDER, 3f, 10);
                    break;
                case Keys.H:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_SHOULDER, -3f, 10);
                    break;
                case Keys.U:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_ELBOW, 3f, 10);
                    break;
                case Keys.J:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_ELBOW, -3f, 10);
                    break;
                case Keys.I:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_HIP, 3f, 10);
                    break;
                case Keys.K:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_HIP, -3f, 10);
                    break;
                case Keys.O:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_KNEE, 3f, 10);
                    break;
                case Keys.L:
                    _ = character.MoveJointByDegree(Character.JointType.LEFT_KNEE, -3f, 10);
                    break;


            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool stateChanged = false;
            switch (e.KeyCode)
            {
                case Keys.W: if (!character.IsMovingForward) { character.IsMovingForward = true; stateChanged = true; } break;
                case Keys.S: if (!character.IsMovingBackward) { character.IsMovingBackward = true; stateChanged = true; } break;
                case Keys.A: if (!character.IsTurningLeft) { character.IsTurningLeft = true; stateChanged = true; } break;
                case Keys.D: if (!character.IsTurningRight) { character.IsTurningRight = true; stateChanged = true; } break;
            }
            if (stateChanged) character.UpdateMovementLoop();

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: character.IsMovingForward = false; break;
                case Keys.S: character.IsMovingBackward = false; break;
                case Keys.A: character.IsTurningLeft = false; break;
                case Keys.D: character.IsTurningRight = false; break;
            }
            base.OnKeyUp(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}