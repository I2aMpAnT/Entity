// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Camera2.cs" company="">
//   
// </copyright>
// <summary>
//   The camera 2.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Render
{
    using System;
    using System.Windows.Forms;

    using Microsoft.DirectX;
    using Microsoft.DirectX.DirectInput;

    /// <summary>
    /// The camera 2.
    /// </summary>
    /// <remarks></remarks>
    public class Camera2 : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// The look at.
        /// </summary>
        public Vector3 LookAt = new Vector3(0, 0, 0f);

        /// <summary>
        /// The position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The up vector.
        /// </summary>
        public Vector3 UpVector = new Vector3(0, 0, 1);

        /// <summary>
        /// The device.
        /// </summary>
        public Device device;

        /// <summary>
        /// The fixedrotation.
        /// </summary>
        public bool fixedrotation;

        /// <summary>
        /// The i.
        /// </summary>
        public float i;

        /// <summary>
        /// The j.
        /// </summary>
        public float j;

        /// <summary>
        /// The k.
        /// </summary>
        public float k;

        /// <summary>
        /// The oldx.
        /// </summary>
        public int oldx;

        /// <summary>
        /// The oldy.
        /// </summary>
        public int oldy;

        /// <summary>
        /// The radianh.
        /// </summary>
        public float radianh;

        /// <summary>
        /// The radianv.
        /// </summary>
        public float radianv;

        /// <summary>
        /// The radius.
        /// </summary>
        public float radius = 1.0f;

        /// <summary>
        /// The speed.
        /// </summary>
        public float speed = 0.5f;

        /// <summary>
        /// The x.
        /// </summary>
        public float x;

        /// <summary>
        /// The y.
        /// </summary>
        public float y = -3f;

        /// <summary>
        /// The z.
        /// </summary>
        public float z = 1f;

        /// <summary>
        /// The m_frustum.
        /// </summary>
        private Plane[] m_frustum = new Plane[6];

        /// <summary>
        /// The gamepad device.
        /// </summary>
        private Device gamepad;

        /// <summary>
        /// Whether gamepad is connected.
        /// </summary>
        public bool gamepadConnected = false;

        /// <summary>
        /// Gamepad left stick X axis (-1 to 1).
        /// </summary>
        public float gamepadLeftX = 0;

        /// <summary>
        /// Gamepad left stick Y axis (-1 to 1).
        /// </summary>
        public float gamepadLeftY = 0;

        /// <summary>
        /// Gamepad right stick X axis (-1 to 1).
        /// </summary>
        public float gamepadRightX = 0;

        /// <summary>
        /// Gamepad right stick Y axis (-1 to 1).
        /// </summary>
        public float gamepadRightY = 0;

        /// <summary>
        /// Gamepad left trigger (0 to 1).
        /// </summary>
        public float gamepadLeftTrigger = 0;

        /// <summary>
        /// Gamepad right trigger (0 to 1).
        /// </summary>
        public float gamepadRightTrigger = 0;

        /// <summary>
        /// Gamepad A button pressed this frame.
        /// </summary>
        public bool gamepadAPressed = false;

        /// <summary>
        /// Gamepad B button pressed this frame.
        /// </summary>
        public bool gamepadBPressed = false;

        /// <summary>
        /// Gamepad Back/Select button pressed this frame.
        /// </summary>
        public bool gamepadBackPressed = false;

        /// <summary>
        /// Gamepad Start button pressed this frame.
        /// </summary>
        public bool gamepadStartPressed = false;

        /// <summary>
        /// Gamepad D-pad Up pressed this frame.
        /// </summary>
        public bool gamepadDPadUpPressed = false;

        /// <summary>
        /// Previous A button state for edge detection.
        /// </summary>
        private bool prevAButton = false;

        /// <summary>
        /// Previous D-pad Up state for edge detection.
        /// </summary>
        private bool prevDPadUp = false;

        /// <summary>
        /// Previous Back button state for edge detection.
        /// </summary>
        private bool prevBackButton = false;

        /// <summary>
        /// Deadzone for analog sticks.
        /// </summary>
        private const float DEADZONE = 0.15f;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera2"/> class.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <remarks></remarks>
        public Camera2(Form form)
        {
            device = new Device(SystemGuid.Keyboard);
            device.SetCooperativeLevel(form, CooperativeLevelFlags.Foreground | CooperativeLevelFlags.NonExclusive);

            // Try to initialize gamepad
            InitializeGamepad(form);

            radianh = DegreesToRadian(90.0f);
            radianv = DegreesToRadian(-20.0f);
            Position = new Vector3(x, y, z);
            ComputePosition();
        }

        /// <summary>
        /// Initialize gamepad device if one is connected.
        /// </summary>
        private void InitializeGamepad(Form form)
        {
            try
            {
                // Find a gamepad device
                foreach (DeviceInstance di in Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
                {
                    gamepad = new Device(di.InstanceGuid);
                    gamepad.SetCooperativeLevel(form, CooperativeLevelFlags.Foreground | CooperativeLevelFlags.NonExclusive);

                    // Set axis range to -1000 to 1000
                    foreach (DeviceObjectInstance doi in gamepad.Objects)
                    {
                        if ((doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                        {
                            gamepad.Properties.SetRange(ParameterHow.ById, doi.ObjectId, new InputRange(-1000, 1000));
                        }
                    }

                    gamepadConnected = true;
                    break;
                }
            }
            catch
            {
                gamepadConnected = false;
                gamepad = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The aim camera.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <remarks></remarks>
        public void AimCamera(Vector3 v)
        {
            i = v.X;
            j = v.Y;
            k = v.Z;
            LookAt.X = i;
            LookAt.Y = j;
            LookAt.Z = k;
        }

        /// <summary>
        /// The compute position.
        /// </summary>
        /// <remarks></remarks>
        public void ComputePosition()
        {
            // Keep all rotations between 0 and 2PI
            // if (fixedcam==true){return;}
            radianh = radianh > (float)Math.PI * 2 ? radianh - (float)Math.PI * 2 : radianh;
            radianh = radianh < 0 ? radianh + (float)Math.PI * 2 : radianh;

            radianv = radianv > (float)Math.PI * 2 ? radianv - (float)Math.PI * 2 : radianv;
            radianv = radianv < 0 ? radianv + (float)Math.PI * 2 : radianv;

            i = radius * (float)(Math.Cos(radianh) * Math.Cos(radianv));
            j = radius * (float)(Math.Sin(radianh) * Math.Cos(radianv));
            k = radius * (float)Math.Sin(radianv);
            

            if (fixedrotation)
            {
                Position.X = i + x;
                Position.Y = j + y;
                Position.Z = k + z;
            }
            else
            {
                LookAt.X = i + x;
                LookAt.Y = j + y;
                LookAt.Z = k + z;
            }
        }

        /// <summary>
        /// The compute strafe.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <remarks></remarks>
        public void ComputeStrafe(bool right)
        {
            // Keep all rotations between 0 and 2PI
            radianh = radianh > (float)Math.PI * 2 ? radianh - (float)Math.PI * 2 : radianh;
            radianh = radianh < 0 ? radianh + (float)Math.PI * 2 : radianh;

            radianv = radianv > (float)Math.PI * 2 ? radianv - (float)Math.PI * 2 : radianv;
            radianv = radianv < 0 ? radianv + (float)Math.PI * 2 : radianv;

            // Switch up-vector based on vertical rotation
            // UpVector = Position.X < 0 ? new Vector3(-1, 0, 1) : new Vector3(1, 0, 1);
            // radianv > Math.PI / 2 && radianv < Math.PI / 2 * 3 ?
            // new Vector3(0,  1,0) : new Vector3(0,  -1,0);
            float tempi = radius * (float)(Math.Cos(radianh - 1.57) * Math.Cos(radianv)) * this.speed;
            float tempj = radius * (float)(Math.Sin(radianh - 1.57) * Math.Cos(radianv)) * this.speed;

            if (right)
            {
                LookAt.X += tempi;
                LookAt.Y += tempj;

                x += tempi;
                y += tempj;

                Position.X = x;
                Position.Y = y;
            }
            else
            {
                LookAt.X -= tempi;
                LookAt.Y -= tempj;

                x -= tempi;
                y -= tempj;

                Position.X = x;
                Position.Y = y;
            }
        }

        /// <summary>
        /// The degrees to radian.
        /// </summary>
        /// <param name="degree">The degree.</param>
        /// <returns>The degrees to radian.</returns>
        /// <remarks></remarks>
        public float DegreesToRadian(float degree)
        {
            return (float)(degree * (Math.PI / 180));
        }

        /// <summary>
        /// The set fixed.
        /// </summary>
        /// <remarks></remarks>
        public void SetFixed()
        {
            fixedrotation = true;
        }

        /// <summary>
        /// The change.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <remarks></remarks>
        public void change(int x, int y)
        {
            int tempx = oldx - x;
            int tempy = oldy - y;

            radianh += DegreesToRadian(tempx);
            radianv += DegreesToRadian(tempy);

            ComputePosition();
            oldx = x;
            oldy = y;
        }

        /// <summary>
        /// The move.
        /// </summary>
        /// <returns>The move.</returns>
        /// <remarks></remarks>
        public bool move()
        {
            bool speedChange = false;
            try
            {
                device.Acquire();
            }
            catch
            {
                return speedChange;
            }

            foreach (Key kk in device.GetPressedKeys())
            {
                switch (kk.ToString())
                {
                    case "W":
                        x += i * speed;
                        y += j * speed;
                        z += k * speed;
                        Position.X = x;
                        Position.Y = y;
                        Position.Z = z;
                        break;
                    case "S":
                        x -= i * speed;
                        y -= j * speed;
                        z -= k * speed;
                        Position.X = x;
                        Position.Y = y;
                        Position.Z = z;

                        break;
                    case "A":
                        ComputeStrafe(false);

                        // y += speed;
                        // Position.Y = y;
                        break;
                    case "D":
                        ComputeStrafe(true);

                        // y -= speed;
                        // Position.Y = y;
                        break;
                    case "Z":
                        z -= speed;
                        Position.Z = z;
                        break;
                    case "X":
                        z += speed;
                        Position.Z = z;
                        break;

                    case "Equals":
                    case "Add":
                        if (speed < 1.0)
                        {
                            speed += 0.01f;
                        }
                        else
                        {
                            speed += 0.1f;
                        }

                        if (speed >= 6.0)
                        {
                            speed = 6.0f;
                        }

                        speedChange = true;
                        break;
                    case "Minus":
                    case "NumPadMinus":
                        if (speed <= 1.0)
                        {
                            speed -= 0.01f;
                        }
                        else
                        {
                            speed -= 0.1f;
                        }

                        if (speed <= 0.01f)
                        {
                            speed = 0.01f;
                        }

                        speedChange = true;
                        break;
                }

                ComputePosition();
            }

            return speedChange;
        }

        /// <summary>
        /// Poll gamepad state and apply input to camera.
        /// </summary>
        /// <returns>True if speed changed.</returns>
        public bool PollGamepad()
        {
            bool speedChange = false;

            // Reset button press states
            gamepadAPressed = false;
            gamepadBPressed = false;
            gamepadBackPressed = false;
            gamepadStartPressed = false;
            gamepadDPadUpPressed = false;

            if (!gamepadConnected || gamepad == null)
            {
                return speedChange;
            }

            try
            {
                gamepad.Acquire();
                gamepad.Poll();
                JoystickState state = gamepad.CurrentJoystickState;

                // Left stick - movement (X axis, Y axis)
                // Normalize from -1000 to 1000 to -1 to 1
                gamepadLeftX = state.X / 1000.0f;
                gamepadLeftY = state.Y / 1000.0f;

                // Right stick - camera (typically Rz for X, Z for Y on Xbox controllers)
                // Different controllers may use different axes
                gamepadRightX = state.Rz / 1000.0f;
                gamepadRightY = state.Z / 1000.0f;

                // Apply deadzone
                if (Math.Abs(gamepadLeftX) < DEADZONE) gamepadLeftX = 0;
                if (Math.Abs(gamepadLeftY) < DEADZONE) gamepadLeftY = 0;
                if (Math.Abs(gamepadRightX) < DEADZONE) gamepadRightX = 0;
                if (Math.Abs(gamepadRightY) < DEADZONE) gamepadRightY = 0;

                // Triggers - using slider or Rx/Ry axes depending on controller
                // Xbox controllers typically use sliders
                int[] sliders = state.GetSlider();
                if (sliders.Length >= 2)
                {
                    // Normalize from 0-65535 to 0-1 (or from -1000 to 1000)
                    gamepadLeftTrigger = (sliders[0] + 1000) / 2000.0f;
                    gamepadRightTrigger = (sliders[1] + 1000) / 2000.0f;
                }
                else
                {
                    // Fallback to Rx/Ry
                    gamepadLeftTrigger = (state.Rx + 1000) / 2000.0f;
                    gamepadRightTrigger = (state.Ry + 1000) / 2000.0f;
                }

                // Buttons - Xbox layout: A=0, B=1, X=2, Y=3, LB=4, RB=5, Back=6, Start=7
                byte[] buttons = state.GetButtons();

                bool currentAButton = buttons.Length > 0 && buttons[0] != 0;
                bool currentBackButton = buttons.Length > 6 && buttons[6] != 0;

                // Edge detection - only trigger on press, not hold
                if (currentAButton && !prevAButton)
                {
                    gamepadAPressed = true;
                }
                if (currentBackButton && !prevBackButton)
                {
                    gamepadBackPressed = true;
                }

                prevAButton = currentAButton;
                prevBackButton = currentBackButton;

                // D-pad handling via buttons (button indices vary by controller)
                // Common D-pad up button indices: 10, 12
                bool currentDPadUp = (buttons.Length > 10 && buttons[10] != 0) ||
                                     (buttons.Length > 12 && buttons[12] != 0);
                if (currentDPadUp && !prevDPadUp)
                {
                    gamepadDPadUpPressed = true;
                }
                prevDPadUp = currentDPadUp;

                // Apply left stick to movement
                if (gamepadLeftY != 0)
                {
                    // Y axis: up = forward (negative on most controllers), down = backward
                    float moveAmount = -gamepadLeftY; // Negate so up = forward

                    // Apply speed modifier from left trigger
                    float effectiveSpeed = speed * (1.0f + gamepadLeftTrigger * 2.0f);

                    x += i * effectiveSpeed * moveAmount;
                    y += j * effectiveSpeed * moveAmount;
                    z += k * effectiveSpeed * moveAmount;
                    Position.X = x;
                    Position.Y = y;
                    Position.Z = z;
                }

                if (gamepadLeftX != 0)
                {
                    // X axis: strafe left/right
                    float effectiveSpeed = speed * (1.0f + gamepadLeftTrigger * 2.0f);

                    // Compute strafe direction
                    float strafeH = radianh - (float)(Math.PI / 2); // 90 degrees to the right
                    float strafeI = (float)(Math.Cos(strafeH) * Math.Cos(radianv));
                    float strafeJ = (float)(Math.Sin(strafeH) * Math.Cos(radianv));

                    x += strafeI * effectiveSpeed * gamepadLeftX;
                    y += strafeJ * effectiveSpeed * gamepadLeftX;
                    Position.X = x;
                    Position.Y = y;
                }

                // Apply right stick to camera look
                if (gamepadRightX != 0 || gamepadRightY != 0)
                {
                    // Camera sensitivity
                    float sensitivity = 2.0f;

                    // Horizontal look (yaw) - right stick X
                    radianh -= DegreesToRadian(gamepadRightX * sensitivity);

                    // Vertical look (pitch) - right stick Y
                    // NOT inverted: pushing down looks down (positive Y = look down)
                    radianv -= DegreesToRadian(gamepadRightY * sensitivity);

                    ComputePosition();
                }

            }
            catch
            {
                // Gamepad disconnected or error
                gamepadConnected = false;
            }

            return speedChange;
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <remarks></remarks>
        public void Dispose()
        {
            device = null;
        }

        #endregion

        #endregion

        /*
        public void BuildViewFrustum(ref DirectX.Direct3D.Device device)
        {
            Matrix viewProjection = device.Transform.View * device.Transform.Projection;

            // Left plane
            m_frustum[0].A = viewProjection.M14 + viewProjection.M11;
            m_frustum[0].B = viewProjection.M24 + viewProjection.M21;
            m_frustum[0].C = viewProjection.M34 + viewProjection.M31;
            m_frustum[0].D = viewProjection.M44 + viewProjection.M41;

            // Right plane
            m_frustum[1].A = viewProjection.M14 - viewProjection.M11;
            m_frustum[1].B = viewProjection.M24 - viewProjection.M21;
            m_frustum[1].C = viewProjection.M34 - viewProjection.M31;
            m_frustum[1].D = viewProjection.M44 - viewProjection.M41;

            // Top plane
            m_frustum[2].A = viewProjection.M14 - viewProjection.M12;
            m_frustum[2].B = viewProjection.M24 - viewProjection.M22;
            m_frustum[2].C = viewProjection.M34 - viewProjection.M32;
            m_frustum[2].D = viewProjection.M44 - viewProjection.M42;

            // Bottom plane
            m_frustum[3].A = viewProjection.M14 + viewProjection.M12;
            m_frustum[3].B = viewProjection.M24 + viewProjection.M22;
            m_frustum[3].C = viewProjection.M34 + viewProjection.M32;
            m_frustum[3].D = viewProjection.M44 + viewProjection.M42;

            // Near plane
            m_frustum[4].A = viewProjection.M13;
            m_frustum[4].B = viewProjection.M23;
            m_frustum[4].C = viewProjection.M33;
            m_frustum[4].D = viewProjection.M43;

            // Far plane
            m_frustum[5].A = viewProjection.M14 - viewProjection.M13;
            m_frustum[5].B = viewProjection.M24 - viewProjection.M23;
            m_frustum[5].C = viewProjection.M34 - viewProjection.M33;
            m_frustum[5].D = viewProjection.M44 - viewProjection.M43;

            // Normalize planes
            for (int i = 0; i < 6; i++)
            {
                m_frustum[i] = Plane.Normalize(m_frustum[i]);
            }
        }

        public bool SphereInFrustum(Vector3 position, float radius)
        {
            Vector4 position4 = new Vector4(position.X, position.Y, position.Z, 1f);
            for (int i = 0; i < 6; i++)
            {
                if (m_frustum[i].Dot(position4) + radius < 0)
                {
                    // Outside the frustum, reject it!
                    return false;
                }
            }
            return true;
        }
        */
    }
}