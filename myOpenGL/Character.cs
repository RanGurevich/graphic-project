using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myOpenGL
{
    public class Character
    {
        private float xPosition;
        private float yPosition;
        private float zPosition;

        public float X => xPosition;
        public float Y => yPosition;
        public float Z => zPosition;

        public bool IsMovingForward;
        public bool IsMovingBackward;
        public bool IsTurningLeft;
        public bool IsTurningRight;
        private bool isWalkingLoopRunning = false;

        public async void UpdateMovementLoop()
        {
            if (isWalkingLoopRunning) return;
            isWalkingLoopRunning = true;

            float walkSpeed = 0.1f;
            float turnSpeed = 5.0f;
            float swingMaxAngle = 45f;
            float swingSpeed = 0.3f;
            float time = 0;

            while (IsMovingForward || IsMovingBackward || IsTurningLeft || IsTurningRight)
            {
                // Turn
                if (IsTurningLeft) CHARACTER_angle += turnSpeed;
                if (IsTurningRight) CHARACTER_angle -= turnSpeed;

                // Move
                if (IsMovingForward || IsMovingBackward)
                {
                    float dir = IsMovingForward ? 1f : -1f;
                    float rad = CHARACTER_angle * (float)Math.PI / 180f;
                    xPosition -= (float)Math.Sin(rad) * walkSpeed * dir;
                    yPosition += (float)Math.Cos(rad) * walkSpeed * dir;

                    // Animation
                    time += swingSpeed;
                    float legSwing = (float)Math.Sin(time) * swingMaxAngle;
                    float armSwing = (float)Math.Sin(time) * swingMaxAngle;

                    LEFT_HIP_angle = legSwing;
                    RIGHT_HIP_angle = -legSwing;
                    
                    // Bend knees slightly when swinging backward
                    LEFT_KNEE_angle = legSwing < 0 ? 40 : 10;
                    RIGHT_KNEE_angle = -legSwing < 0 ? 40 : 10;

                    // Arms swing opposite to legs
                    LEFT_SHOULDER_angle = -armSwing;
                    RIGHT_SHOULDER_angle = armSwing;
                }
                else
                {
                    ResetPose();
                }

                OnAnimationStep?.Invoke();
                await Task.Delay(16); // ~60 FPS
            }

            ResetPose();
            OnAnimationStep?.Invoke();
            isWalkingLoopRunning = false;
        }

        private void ResetPose()
        {
            LEFT_SHOULDER_angle = 10;
            RIGHT_SHOULDER_angle = -10;
            LEFT_HIP_angle = 5;
            RIGHT_HIP_angle = -5;
            LEFT_KNEE_angle = 10;
            RIGHT_KNEE_angle = 10;
        }

        // ===== Joint angles =====
        public float CHARACTER_angle;        // whole body rotation around Z
        public float HEAD_angle;             // head turn around Z

        public float LEFT_SHOULDER_angle;    // left upper arm rotation
        public float RIGHT_SHOULDER_angle;   // right upper arm rotation
        public float LEFT_ELBOW_angle;       // left forearm rotation
        public float RIGHT_ELBOW_angle;      // right forearm rotation

        public float LEFT_HIP_angle;         // left thigh rotation
        public float RIGHT_HIP_angle;        // right thigh rotation
        public float LEFT_KNEE_angle;        // left shin rotation
        public float RIGHT_KNEE_angle;       // right shin rotation

        public float alpha;

        // ===== Display lists =====
        uint CHARACTER_LIST, HEAD_LIST;
        uint UPPER_ARM_LIST, LOWER_ARM_LIST;
        uint UPPER_LEG_LIST, LOWER_LEG_LIST;

        // ===== Body dimensions =====
        float bodyRadius, bodyLength;
        float headRadius;
        float neckRadius, neckLength;
        float upperArmRadius, upperArmLength;
        float lowerArmRadius, lowerArmLength;
        float upperLegRadius, upperLegLength;
        float lowerLegRadius, lowerLegLength;

        GLUquadric obj;

        public enum JointType
        {
            CHARACTER,
            HEAD,
            LEFT_SHOULDER,
            RIGHT_SHOULDER,
            LEFT_ELBOW,
            RIGHT_ELBOW,
            LEFT_HIP,
            RIGHT_HIP,
            LEFT_KNEE,
            RIGHT_KNEE
        }

        public event Action OnAnimationStep;

        public async Task AnimateJoint(JointType joint, float targetAngle, int durationMs)
        {
            float startAngle = GetJointAngle(joint);
            float angleDifference = targetAngle - startAngle;
            
            int frameDuration = 16; // ~60 FPS
            int steps = durationMs / frameDuration;
            if (steps <= 0) steps = 1;
            
            float angleStep = angleDifference / steps;

            for (int i = 0; i < steps; i++)
            {
                SetJointAngle(joint, GetJointAngle(joint) + angleStep);
                OnAnimationStep?.Invoke();
                await Task.Delay(frameDuration);
            }
            
            // מוודא שהגענו בדיוק לזווית היעד בסוף
            SetJointAngle(joint, targetAngle);
            OnAnimationStep?.Invoke();
        }

        public async Task MoveJointByDegree(JointType joint, float degreeDelta, int durationMs)
        {
            float currentAngle = GetJointAngle(joint);
            float targetAngle = currentAngle + degreeDelta;
            await AnimateJoint(joint, targetAngle, durationMs);
        }

        private float GetJointAngle(JointType joint)
        {
            switch (joint)
            {
                case JointType.CHARACTER: return CHARACTER_angle;
                case JointType.HEAD: return HEAD_angle;
                case JointType.LEFT_SHOULDER: return LEFT_SHOULDER_angle;
                case JointType.RIGHT_SHOULDER: return RIGHT_SHOULDER_angle;
                case JointType.LEFT_ELBOW: return LEFT_ELBOW_angle;
                case JointType.RIGHT_ELBOW: return RIGHT_ELBOW_angle;
                case JointType.LEFT_HIP: return LEFT_HIP_angle;
                case JointType.RIGHT_HIP: return RIGHT_HIP_angle;
                case JointType.LEFT_KNEE: return LEFT_KNEE_angle;
                case JointType.RIGHT_KNEE: return RIGHT_KNEE_angle;
                default: return 0;
            }
        }

        private void SetJointAngle(JointType joint, float angle)
        {
            switch (joint)
            {
                case JointType.CHARACTER: CHARACTER_angle = angle; break;
                case JointType.HEAD: HEAD_angle = angle; break;
                case JointType.LEFT_SHOULDER: LEFT_SHOULDER_angle = angle; break;
                case JointType.RIGHT_SHOULDER: RIGHT_SHOULDER_angle = angle; break;
                case JointType.LEFT_ELBOW: LEFT_ELBOW_angle = angle; break;
                case JointType.RIGHT_ELBOW: RIGHT_ELBOW_angle = angle; break;
                case JointType.LEFT_HIP: LEFT_HIP_angle = angle; break;
                case JointType.RIGHT_HIP: RIGHT_HIP_angle = angle; break;
                case JointType.LEFT_KNEE: LEFT_KNEE_angle = angle; break;
                case JointType.RIGHT_KNEE: RIGHT_KNEE_angle = angle; break;
            }
        }

        public Character(float xPosition, float yPosition, float zPosition)
        {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
        }

        bool isInitialized = false;

        public void PrepareLists()
        {
            if (!isInitialized)
            {
                isInitialized = true;

                // --- Body proportions (half scale) ---
                bodyRadius = 0.2f;
                bodyLength = 1.0f;
                headRadius = 0.175f;
                neckRadius = 0.06f;
                neckLength = 0.1f;
                upperArmRadius = 0.06f;
                upperArmLength = 0.6f;
                lowerArmRadius = 0.05f;
                lowerArmLength = 0.5f;
                upperLegRadius = 0.075f;
                upperLegLength = 0.65f;
                lowerLegRadius = 0.06f;
                lowerLegLength = 0.6f;

                // --- Default joint angles ---
                CHARACTER_angle = 0;
                HEAD_angle = 0;
                LEFT_SHOULDER_angle = 10;
                RIGHT_SHOULDER_angle = -10;
                LEFT_ELBOW_angle = -30;
                RIGHT_ELBOW_angle = -30;
                LEFT_HIP_angle = 5;
                RIGHT_HIP_angle = -5;
                LEFT_KNEE_angle = 10;
                RIGHT_KNEE_angle = 10;

                obj = GLU.gluNewQuadric();

                // Allocate 6 consecutive display lists
                CHARACTER_LIST = GL.glGenLists(6);
                HEAD_LIST = CHARACTER_LIST + 1;
                UPPER_ARM_LIST = CHARACTER_LIST + 2;
                LOWER_ARM_LIST = CHARACTER_LIST + 3;
                UPPER_LEG_LIST = CHARACTER_LIST + 4;
                LOWER_LEG_LIST = CHARACTER_LIST + 5;

                // ========== HEAD LIST ==========
                GL.glPushMatrix();
                GL.glNewList(HEAD_LIST, GL.GL_COMPILE);
                // head sphere (glowing ice-blue)
                GL.glColor3f(0.7f, 0.9f, 1.0f);
                GLU.gluSphere(obj, headRadius, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER ARM LIST ==========
                // Draws the upper-arm cylinder, translates to its end, draws elbow joint
                GL.glPushMatrix();
                GL.glNewList(UPPER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.95f, 0.0f, 0.6f);              // neon magenta
                GLU.gluCylinder(obj, upperArmRadius, upperArmRadius, upperArmLength, 20, 20);
                GL.glTranslated(0, 0, upperArmLength);
                // elbow joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);                // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER ARM LIST ==========
                // Draws the forearm cylinder, translates to its end, draws hand
                GL.glPushMatrix();
                GL.glNewList(LOWER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.85f, 0.0f, 0.5f);              // deep magenta
                GLU.gluCylinder(obj, lowerArmRadius, lowerArmRadius, lowerArmLength, 20, 20);
                GL.glTranslated(0, 0, lowerArmLength);
                // hand sphere
                GL.glColor3f(1.0f, 0.5f, 0.0f);                // neon orange hand
                GLU.gluSphere(obj, lowerArmRadius * 1.5f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER LEG LIST ==========
                // Draws the thigh cylinder, translates to its end, draws knee joint
                GL.glPushMatrix();
                GL.glNewList(UPPER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.4f, 0.0f, 0.8f);               // electric purple
                GLU.gluCylinder(obj, upperLegRadius, upperLegRadius, upperLegLength, 20, 20);
                GL.glTranslated(0, 0, upperLegLength);
                // knee joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);                // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.2f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER LEG LIST ==========
                // Draws the shin cylinder, translates to its end, draws foot
                GL.glPushMatrix();
                GL.glNewList(LOWER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.3f, 0.0f, 0.7f);               // deep purple
                GLU.gluCylinder(obj, lowerLegRadius, lowerLegRadius, lowerLegLength, 20, 20);
                GL.glTranslated(0, 0, lowerLegLength);
                // foot (flattened sphere)
                GL.glColor3f(1.0f, 0.5f, 0.0f);                // neon orange boot
                GL.glPushMatrix();
                GL.glScalef(1.0f, 0.6f, 1.5f);
                GLU.gluSphere(obj, lowerLegRadius * 1.3f, 20, 20);
                GL.glPopMatrix();
                GL.glEndList();
                GL.glPopMatrix();
            }

            CreateCharacterList();
        }


        public void CreateCharacterList()
        {
            GL.glPushMatrix();
            //
            // hierarchical list
            //
            GL.glNewList(CHARACTER_LIST, GL.GL_COMPILE);

            // ==========================================
            //  BODY (TORSO) — root of the hierarchy
            // ==========================================
            // Torso cylinder along +Z  (bottom = hips, top = shoulders)
            GL.glColor3f(0.0f, 0.9f, 0.9f);               // neon cyan armor
            GLU.gluCylinder(obj, bodyRadius, bodyRadius * 0.85f, bodyLength, 20, 20);
            // bottom disk (belt area)
            GL.glColor3f(0.0f, 0.7f, 0.7f);               // darker cyan belt
            GLU.gluDisk(obj, 0, bodyRadius, 20, 20);
            // end body

            // ==========================================
            //  HEAD  (child of body)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(0, 0, bodyLength);
                // top disk of torso
                GL.glColor3f(0.0f, 0.9f, 0.9f);               // cyan top
                GLU.gluDisk(obj, 0, bodyRadius * 0.85f, 20, 20);
                // neck
                GL.glColor3f(0.5f, 0.5f, 0.6f);               // metallic silver neck
                GLU.gluCylinder(obj, neckRadius, neckRadius, neckLength, 20, 20);
                GL.glTranslated(0, 0, neckLength + headRadius * 0.7f);
                // head rotation
                GL.glRotatef(HEAD_angle, 0, 0, 1);
                // call HEAD
                GL.glCallList(HEAD_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  LEFT ARM  (child of body)
            //    └── left forearm  (child of left arm)
            // ==========================================
            GL.glPushMatrix();
                // move to left shoulder
                GL.glTranslated(bodyRadius * 0.85f + upperArmRadius, 0, bodyLength - 0.1f);
                // shoulder joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);               // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);

                // transformations: flip downward + shoulder rotation
                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(LEFT_SHOULDER_angle, 1, 0, 0);

                // call UPPER ARM
                GL.glCallList(UPPER_ARM_LIST);

                // transformations: elbow rotation
                // (no extra translate needed — UPPER_ARM_LIST already moved to the end)
                GL.glRotatef(LEFT_ELBOW_angle, 1, 0, 0);

                // call LOWER ARM
                GL.glCallList(LOWER_ARM_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  RIGHT ARM  (child of body)
            //    └── right forearm  (child of right arm)
            // ==========================================
            GL.glPushMatrix();
                // move to right shoulder
                GL.glTranslated(-(bodyRadius * 0.85f + upperArmRadius), 0, bodyLength - 0.1f);
                // shoulder joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);               // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);

                // transformations
                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(RIGHT_SHOULDER_angle, 1, 0, 0);

                // call UPPER ARM
                GL.glCallList(UPPER_ARM_LIST);

                // transformations
                GL.glRotatef(RIGHT_ELBOW_angle, 1, 0, 0);

                // call LOWER ARM
                GL.glCallList(LOWER_ARM_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  LEFT LEG  (child of body)
            //    └── left shin  (child of left leg)
            // ==========================================
            GL.glPushMatrix();
                // move to left hip
                GL.glTranslated(bodyRadius * 0.4f, 0, 0);
                // hip joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);               // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.1f, 20, 20);

                // transformations: flip downward + hip rotation
                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(LEFT_HIP_angle, 1, 0, 0);

                // call UPPER LEG
                GL.glCallList(UPPER_LEG_LIST);

                // transformations: knee rotation
                GL.glRotatef(LEFT_KNEE_angle, 1, 0, 0);

                // call LOWER LEG
                GL.glCallList(LOWER_LEG_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  RIGHT LEG  (child of body)
            //    └── right shin  (child of right leg)
            // ==========================================
            GL.glPushMatrix();
                // move to right hip
                GL.glTranslated(-(bodyRadius * 0.4f), 0, 0);
                // hip joint sphere
                GL.glColor3f(0.0f, 1.0f, 0.4f);               // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.1f, 20, 20);

                // transformations
                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(RIGHT_HIP_angle, 1, 0, 0);

                // call UPPER LEG
                GL.glCallList(UPPER_LEG_LIST);

                // transformations
                GL.glRotatef(RIGHT_KNEE_angle, 1, 0, 0);

                // call LOWER LEG
                GL.glCallList(LOWER_LEG_LIST);
            GL.glPopMatrix();

            GL.glEndList();
            GL.glPopMatrix();
        }

        public void DrawCube()
        {
            // cube
            GL.glBegin(GL.GL_QUADS);

            //1

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            //2

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);


            //3

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);


            //4

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);


            //5

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);


            //6

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);


            GL.glEnd();

        }

        public void DrawCharacter()
        {
            PrepareLists();
            
            // מעדכן את רשימת הציור כדי להכיל את הזוויות החדשות
            CreateCharacterList();

            // ===== Enable lighting for the character =====
            GL.glEnable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_LIGHT0);
            GL.glEnable(GL.GL_NORMALIZE);

            // Light position (above-right-front)
            float[] lightPos = { 5.0f, 5.0f, 10.0f, 1.0f };
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_POSITION, lightPos);

            // Light properties
            float[] ambientLight = { 0.2f, 0.2f, 0.25f, 1.0f };
            float[] diffuseLight = { 0.9f, 0.9f, 1.0f, 1.0f };
            float[] specularLight = { 1.0f, 1.0f, 1.0f, 1.0f };
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_AMBIENT, ambientLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_DIFFUSE, diffuseLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_SPECULAR, specularLight);

            // Enable color material: glColor3f will set both ambient+diffuse material
            GL.glEnable(0x0B57); // GL_COLOR_MATERIAL = 0x0B57
            GL.glColorMaterial(GL.GL_FRONT_AND_BACK, GL.GL_AMBIENT_AND_DIFFUSE);

            // Add specular shininess to all surfaces
            float[] matSpecular = { 0.6f, 0.6f, 0.7f, 1.0f };
            GL.glMaterialfv(GL.GL_FRONT_AND_BACK, GL.GL_SPECULAR, matSpecular);
            GL.glMaterialf(GL.GL_FRONT_AND_BACK, GL.GL_SHININESS, 60.0f);

            // ===== Draw the character =====
            GL.glPushMatrix();
            GL.glTranslatef(xPosition, yPosition, zPosition);
            GL.glRotatef(CHARACTER_angle, 0, 0, 1);
            GL.glCallList(CHARACTER_LIST);
            GL.glPopMatrix();

            // ===== Disable lighting so it doesn't affect the rest of the scene =====
            GL.glDisable(0x0B57); // GL_COLOR_MATERIAL
            GL.glDisable(GL.GL_LIGHT0);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_NORMALIZE);
        }
    }
}
