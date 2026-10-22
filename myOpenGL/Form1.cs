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
        DrawGame cGL;
        private Character character;

        private Point lastMousePos;
        private bool isMouseLookActive = true;
        private bool isFirstMouseMove = true;
        private bool isCenteringCursor = false;
        private bool isCursorHidden = false;
        private Timer renderTimer;

        public Form1()
        {
            InitializeComponent();
            cGL = new DrawGame(panel1);
            character = new Character(0, 0, 0);
            cGL.SetCharacter(character);

            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseMove += panel1_MouseMove;
            panel1.MouseClick += panel1_MouseClick;
            panel1.MouseWheel += panel1_MouseWheel;
            this.MouseWheel += panel1_MouseWheel;
            this.Activated += (s, e) => 
            { 
                isFirstMouseMove = true;
                UpdateCursorVisibility();
            };
            this.Deactivate += (s, e) => 
            { 
                UpdateCursorVisibility(); 
            };
            this.FormClosing += (s, e) =>
            {
                if (isCursorHidden)
                {
                    Cursor.Show();
                    isCursorHidden = false;
                }
            };

            renderTimer = new Timer();
            renderTimer.Interval = 25;
            renderTimer.Tick += (s, e) => { cGL.DrawAll(); };
            renderTimer.Start();

            this.KeyPreview = true;
        }

        private void UpdateCursorVisibility()
        {
            bool shouldHide = isMouseLookActive && Form.ActiveForm == this;
            if (shouldHide && !isCursorHidden)
            {
                Cursor.Hide();
                isCursorHidden = true;
            }
            else if (!shouldHide && isCursorHidden)
            {
                Cursor.Show();
                isCursorHidden = false;
            }
        }

        private void SetMouseLook(bool active)
        {
            isMouseLookActive = active;
            isFirstMouseMove = true;
            UpdateCursorVisibility();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            lastMousePos = e.Location;
            if (!isMouseLookActive && e.Button == MouseButtons.Left)
            {
                SetMouseLook(true);
                return;
            }

            if (character != null)
            {
                if (character.IsAttacking)
                {
                    return;
                }

                if (e.Button == MouseButtons.Right)
                {
                    character.PerformKick();
                }
                else if (e.Button == MouseButtons.Left)
                {
                    character.PerformPunch();
                }
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCenteringCursor)
            {
                isCenteringCursor = false;
                return;
            }

            if (!isMouseLookActive || Form.ActiveForm != this)
            {
                if (e.Button == MouseButtons.Left)
                {
                    int dx = e.X - lastMousePos.X;
                    int dy = e.Y - lastMousePos.Y;
                    lastMousePos = e.Location;
                    if (dx != 0 || dy != 0)
                    {
                        cGL.RotateCamera(dx, dy);
                    }
                }
                return;
            }

            Point center = new Point(panel1.Width / 2, panel1.Height / 2);

            if (isFirstMouseMove)
            {
                isFirstMouseMove = false;
                isCenteringCursor = true;
                Cursor.Position = panel1.PointToScreen(center);
                lastMousePos = center;
                return;
            }

            int deltaX = e.X - center.X;
            int deltaY = e.Y - center.Y;

            if (deltaX != 0 || deltaY != 0)
            {
                cGL.RotateCamera(deltaX, deltaY);

                isCenteringCursor = true;
                Cursor.Position = panel1.PointToScreen(center);
            }
        }

        private void panel1_MouseWheel(object sender, MouseEventArgs e)
        {
            cGL.ZoomCamera(e.Delta);
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
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    SetMouseLook(!isMouseLookActive);
                    return true;
                case Keys.Y:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_SHOULDER, 30f, 10);
                    break;
                case Keys.H:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_SHOULDER, -30f, 10);
                    break;
                case Keys.U:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_ELBOW, 30f, 10);
                    break;
                case Keys.J:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_ELBOW, -30f, 10);
                    break;
                case Keys.I:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_HIP, 30f, 10);
                    break;
                case Keys.K:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_HIP, -30f, 10);
                    break;
                case Keys.O:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_KNEE, 30f, 10);
                    break;
                case Keys.L:
                    if (!character.IsAttacking) _ = character.MoveJointByDegree(Character.JointType.LEFT_KNEE, -30f, 10);
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
            SetMouseLook(true);
        }
    }
}