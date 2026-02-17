// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Gizmo.cs" company="">
//
// </copyright>
// <summary>
//   The gizmo.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace entity.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using Microsoft.DirectX;
    using Microsoft.DirectX.Direct3D;

    using Font = System.Drawing.Font;

    /// <summary>
    /// The gizmo.
    /// </summary>
    /// <remarks></remarks>
    internal class Gizmo
    {
        #region Constants and Fields

        /// <summary>
        /// The device.
        /// </summary>
        private readonly Device device;

        /// <summary>
        /// The fnt.
        /// </summary>
        private readonly Font fnt = new Font("Arial", 12);

        /// <summary>
        /// The font.
        /// </summary>
        private readonly Microsoft.DirectX.Direct3D.Font font;

        /// <summary>
        /// The gizmo mesh (used for picking).
        /// </summary>
        private Mesh gizmo;

        /// <summary>
        /// The scale.
        /// </summary>
        private float scale = 1.0f;

        /// <summary>
        /// The selected axis.
        /// </summary>
        private axis selectedAxis = axis.none;

        /// <summary>
        /// The current transform mode.
        /// </summary>
        private transform currentTransform = transform.movement;

        /// <summary>
        /// Number of mesh subsets for the current gizmo type.
        /// </summary>
        private int meshSubsetCount = 9;

        /// <summary>
        /// Whether a rotation drag is in progress.
        /// </summary>
        private bool isDragging = false;

        /// <summary>
        /// Accumulated rotation during the current drag (radians).
        /// </summary>
        private float totalRotation = 0f;

        /// <summary>
        /// The axis being dragged for rotation feedback.
        /// </summary>
        private axis dragAxis = axis.none;

        // Rotation ring constants
        private const int ringSegments = 48;
        private const float ringRadius = 10f;
        private const float ringInnerR = 8.5f;
        private const float ringOuterR = 11.5f;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Gizmo"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <remarks></remarks>
        public Gizmo(Device device)
        {
            this.device = device;
            createMovementGizmo();
            this.device.RenderState.ZBufferEnable = true;
            font = new Microsoft.DirectX.Direct3D.Font(device, fnt);
        }

        #endregion

        #region Enums

        /// <summary>
        /// The axis.
        /// </summary>
        /// <remarks></remarks>
        public enum axis
        {
            /// <summary>
            /// The none.
            /// </summary>
            none,

            /// <summary>
            /// The x.
            /// </summary>
            X,

            /// <summary>
            /// The y.
            /// </summary>
            Y,

            /// <summary>
            /// The z.
            /// </summary>
            Z,

            /// <summary>
            /// The xy.
            /// </summary>
            XY,

            /// <summary>
            /// The xz.
            /// </summary>
            XZ,

            /// <summary>
            /// The yz.
            /// </summary>
            YZ
        }

        /// <summary>
        /// The transform.
        /// </summary>
        /// <remarks></remarks>
        public enum transform
        {
            /// <summary>
            /// The movement.
            /// </summary>
            movement,

            /// <summary>
            /// The rotation.
            /// </summary>
            rotation,

            /// <summary>
            /// The scale.
            /// </summary>
            scale
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current transform mode.
        /// </summary>
        public transform CurrentTransform
        {
            get { return currentTransform; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Switches between gizmo modes (movement, rotation).
        /// </summary>
        /// <param name="tForm">The transform mode.</param>
        public void SetGizmoMode(transform tForm)
        {
            if (currentTransform == tForm)
                return;

            currentTransform = tForm;
            selectedAxis = axis.none;
            isDragging = false;
            totalRotation = 0f;

            if (gizmo != null)
            {
                gizmo.Dispose();
                gizmo = null;
            }

            switch (tForm)
            {
                case transform.movement:
                    createMovementGizmo();
                    meshSubsetCount = 9;
                    break;
                case transform.rotation:
                    createRotationGizmo();
                    meshSubsetCount = 3;
                    break;
            }
        }

        /// <summary>
        /// Begins tracking a rotation drag for visual feedback.
        /// </summary>
        /// <param name="ax">The axis being rotated.</param>
        public void BeginDrag(axis ax)
        {
            isDragging = true;
            totalRotation = 0f;
            dragAxis = ax;
        }

        /// <summary>
        /// Ends rotation drag tracking.
        /// </summary>
        public void EndDrag()
        {
            isDragging = false;
            totalRotation = 0f;
            dragAxis = axis.none;
        }

        /// <summary>
        /// Accumulates rotation amount for arc feedback display.
        /// </summary>
        /// <param name="amount">Rotation in radians.</param>
        public void AddRotation(float amount)
        {
            totalRotation += amount;
        }

        /// <summary>
        /// Checks for intersection with a given mouse point.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="mat">The world matrix.</param>
        /// <returns>The intersected axis.</returns>
        /// <remarks></remarks>
        public axis checkForIntersection(MouseEventArgs e, Matrix mat)
        {
            List<int> temp = new List<int>();
            temp = MeshPick(e.X, e.Y, this.gizmo, mat);
            if (temp.Count > 0)
            {
                if (currentTransform == transform.rotation)
                {
                    switch (temp[0])
                    {
                        case 0:
                            this.selectedAxis = axis.X;
                            break;
                        case 1:
                            this.selectedAxis = axis.Y;
                            break;
                        case 2:
                            this.selectedAxis = axis.Z;
                            break;
                        default:
                            this.selectedAxis = axis.none;
                            break;
                    }
                }
                else
                {
                    switch (temp[0])
                    {
                        case 0:
                        case 3:
                            this.selectedAxis = axis.X;
                            break;
                        case 1:
                        case 4:
                            this.selectedAxis = axis.Y;
                            break;
                        case 2:
                        case 5:
                            this.selectedAxis = axis.Z;
                            break;
                        case 6:
                            this.selectedAxis = axis.XY;
                            break;
                        case 7:
                            this.selectedAxis = axis.YZ;
                            break;
                        case 8:
                            this.selectedAxis = axis.XZ;
                            break;
                        default:
                            this.selectedAxis = axis.none;
                            break;
                    }
                }
            }
            else
            {
                this.selectedAxis = axis.none;
            }

            return this.selectedAxis;
        }

        /// <summary>
        /// Draws the gizmo. Dispatches to the appropriate drawing method based on current mode.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <remarks></remarks>
        public void draw(float scale)
        {
            switch (currentTransform)
            {
                case transform.movement:
                    drawMovement(scale);
                    break;
                case transform.rotation:
                    drawRotation(scale);
                    break;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Draws the movement gizmo (translation arrows).
        /// </summary>
        private void drawMovement(float scale)
        {
            // Store current world matrix
            Matrix mat = device.Transform.World;

            // Set the world matrix to our desires for our gizmo
            device.Transform.World = Matrix.Scaling(scale, scale, scale) * mat;

            FillMode oldFill = device.RenderState.FillMode;
            device.RenderState.FillMode = FillMode.Solid;
            Cull oldCull = device.RenderState.CullMode;
            device.RenderState.CullMode = Cull.None;
            bool oldLighting = device.RenderState.Lighting;
            device.RenderState.Lighting = false;
            bool oldZBuffer = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;

            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[18];

            Color c1 = Color.Red;
            Color c2 = Color.Red;
            Color c3 = Color.Red;
            if (this.selectedAxis == axis.X)
            {
                c1 = Color.Yellow;
            }

            if (this.selectedAxis == axis.XY)
            {
                c1 = Color.Yellow;
                c2 = Color.Yellow;
            }

            if (this.selectedAxis == axis.XZ)
            {
                c1 = Color.Yellow;
                c3 = Color.Yellow;
            }

            vertices[0].Color = c1.ToArgb();
            vertices[1].Color = c1.ToArgb();
            vertices[2].Color = c2.ToArgb();
            vertices[3].Color = c2.ToArgb();
            vertices[4].Color = c3.ToArgb();
            vertices[5].Color = c3.ToArgb();
            vertices[0].Position = new Vector3(0f, 0f, 0f);
            vertices[1].Position = new Vector3(10f, 0f, 0f);
            vertices[2].Position = new Vector3(5f, 0f, 0f);
            vertices[3].Position = new Vector3(5f, 5f, 0f);
            vertices[4].Position = new Vector3(5f, 0f, 0f);
            vertices[5].Position = new Vector3(5f, 0f, 5f);
            Vector3 pos = new Vector3(13f, 1f, 0f);
            Vector3 plot2d = Vector3.Project(
                pos, this.device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);

            // Need to render in 3D for ZBuffer removal
            font.DrawText(null, "x", new Point((int)plot2d.X, (int)plot2d.Y), c1);

            c1 = Color.Green;
            c2 = Color.Green;
            c3 = Color.Green;
            if (this.selectedAxis == axis.Y)
            {
                c1 = Color.Yellow;
            }

            if (this.selectedAxis == axis.XY)
            {
                c1 = Color.Yellow;
                c2 = Color.Yellow;
            }

            if (this.selectedAxis == axis.YZ)
            {
                c1 = Color.Yellow;
                c3 = Color.Yellow;
            }

            vertices[6].Color = c1.ToArgb();
            vertices[7].Color = c1.ToArgb();
            vertices[8].Color = c2.ToArgb();
            vertices[9].Color = c2.ToArgb();
            vertices[10].Color = c3.ToArgb();
            vertices[11].Color = c3.ToArgb();
            vertices[6].Position = new Vector3(0f, 0f, 0f);
            vertices[7].Position = new Vector3(0f, 10f, 0f);
            vertices[8].Position = new Vector3(0f, 5f, 0f);
            vertices[9].Position = new Vector3(5f, 5f, 0f);
            vertices[10].Position = new Vector3(0f, 5f, 0f);
            vertices[11].Position = new Vector3(0f, 5f, 5f);
            pos = new Vector3(1f, 13f, 0f);
            plot2d = Vector3.Project(
                pos, this.device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            font.DrawText(null, "y", new Point((int)plot2d.X, (int)plot2d.Y), c1);

            c1 = Color.Blue;
            c2 = Color.Blue;
            c3 = Color.Blue;
            if (this.selectedAxis == axis.Z)
            {
                c1 = Color.Yellow;
            }

            if (this.selectedAxis == axis.XZ)
            {
                c1 = Color.Yellow;
                c2 = Color.Yellow;
            }

            if (this.selectedAxis == axis.YZ)
            {
                c1 = Color.Yellow;
                c3 = Color.Yellow;
            }

            vertices[12].Color = c1.ToArgb();
            vertices[13].Color = c1.ToArgb();
            vertices[14].Color = c2.ToArgb();
            vertices[15].Color = c2.ToArgb();
            vertices[16].Color = c3.ToArgb();
            vertices[17].Color = c3.ToArgb();
            vertices[12].Position = new Vector3(0f, 0f, 0f);
            vertices[13].Position = new Vector3(0f, 0f, 10f);
            vertices[14].Position = new Vector3(0f, 0f, 5f);
            vertices[15].Position = new Vector3(5f, 0f, 5f);
            vertices[16].Position = new Vector3(0f, 0f, 5f);
            vertices[17].Position = new Vector3(0f, 5f, 5f);
            pos = new Vector3(1f, 0f, 13f);
            plot2d = Vector3.Project(
                pos, this.device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            font.DrawText(null, "z", new Point((int)plot2d.X, (int)plot2d.Y), c1);

            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawUserPrimitives(PrimitiveType.LineList, 9, vertices);

            #region Drawing axis cones

            this.gizmo.DrawSubset(0); // X-Axis
            this.gizmo.DrawSubset(1); // Y-Axis
            this.gizmo.DrawSubset(2); // Z-Axis

            #endregion

            this.gizmo.DrawSubset(3); // X-Shaft
            this.gizmo.DrawSubset(4); // Y-Shaft
            this.gizmo.DrawSubset(5); // Z-Shaft

            // Restore previous world matrix
            device.RenderState.FillMode = oldFill;
            device.RenderState.CullMode = oldCull;
            device.RenderState.Lighting = oldLighting;
            device.RenderState.ZBufferEnable = oldZBuffer;
            device.Transform.World = mat;
            this.scale = scale;
        }

        /// <summary>
        /// Draws the rotation gizmo (colored rings with feedback arc).
        /// </summary>
        private void drawRotation(float scale)
        {
            Matrix mat = device.Transform.World;
            device.Transform.World = Matrix.Scaling(scale, scale, scale) * mat;

            FillMode oldFill = device.RenderState.FillMode;
            device.RenderState.FillMode = FillMode.Solid;
            Cull oldCull = device.RenderState.CullMode;
            device.RenderState.CullMode = Cull.None;
            bool oldZWrite = device.RenderState.ZBufferWriteEnable;
            bool oldZBuffer = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;
            bool oldLighting = device.RenderState.Lighting;
            device.RenderState.Lighting = false;

            device.SetTexture(0, null);
            device.VertexFormat = CustomVertex.PositionColored.Format;

            int vertsPerRing = ringSegments + 1;
            CustomVertex.PositionColored[] ringVerts = new CustomVertex.PositionColored[vertsPerRing];

            // Offset for drawing 3-line-thick rings (simulates ~3px)
            float thickness = (selectedAxis != axis.none) ? 0.25f : 0.15f;

            // X ring (YZ plane) - Red
            Color xColor = (selectedAxis == axis.X) ? Color.Yellow : Color.Red;
            float xThick = (selectedAxis == axis.X) ? 0.25f : thickness;
            for (float off = -xThick; off <= xThick; off += xThick)
            {
                for (int i = 0; i <= ringSegments; i++)
                {
                    float angle = (float)(2 * Math.PI * i / ringSegments);
                    ringVerts[i].Position = new Vector3(off,
                        ringRadius * (float)Math.Cos(angle),
                        ringRadius * (float)Math.Sin(angle));
                    ringVerts[i].Color = xColor.ToArgb();
                }
                device.DrawUserPrimitives(PrimitiveType.LineStrip, ringSegments, ringVerts);
            }

            // Y ring (XZ plane) - Green
            Color yColor = (selectedAxis == axis.Y) ? Color.Yellow : Color.Green;
            float yThick = (selectedAxis == axis.Y) ? 0.25f : thickness;
            for (float off = -yThick; off <= yThick; off += yThick)
            {
                for (int i = 0; i <= ringSegments; i++)
                {
                    float angle = (float)(2 * Math.PI * i / ringSegments);
                    ringVerts[i].Position = new Vector3(
                        ringRadius * (float)Math.Cos(angle),
                        off,
                        ringRadius * (float)Math.Sin(angle));
                    ringVerts[i].Color = yColor.ToArgb();
                }
                device.DrawUserPrimitives(PrimitiveType.LineStrip, ringSegments, ringVerts);
            }

            // Z ring (XY plane) - Blue
            Color zColor = (selectedAxis == axis.Z) ? Color.Yellow : Color.Blue;
            float zThick = (selectedAxis == axis.Z) ? 0.25f : thickness;
            for (float off = -zThick; off <= zThick; off += zThick)
            {
                for (int i = 0; i <= ringSegments; i++)
                {
                    float angle = (float)(2 * Math.PI * i / ringSegments);
                    ringVerts[i].Position = new Vector3(
                        ringRadius * (float)Math.Cos(angle),
                        ringRadius * (float)Math.Sin(angle),
                        off);
                    ringVerts[i].Color = zColor.ToArgb();
                }
                device.DrawUserPrimitives(PrimitiveType.LineStrip, ringSegments, ringVerts);
            }

            // Axis labels
            Vector3 pos = new Vector3(0, ringRadius + 2, 0);
            Vector3 plot2d = Vector3.Project(
                pos, device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            font.DrawText(null, "x", new Point((int)plot2d.X, (int)plot2d.Y), xColor);

            pos = new Vector3(ringRadius + 2, 0, 0);
            plot2d = Vector3.Project(
                pos, device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            font.DrawText(null, "y", new Point((int)plot2d.X, (int)plot2d.Y), yColor);

            pos = new Vector3(0, 0, ringRadius + 2);
            plot2d = Vector3.Project(
                pos, device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            font.DrawText(null, "z", new Point((int)plot2d.X, (int)plot2d.Y), zColor);

            // Draw rotation feedback arc when dragging
            if (isDragging && Math.Abs(totalRotation) > 0.01f)
            {
                drawRotationArc();
            }

            device.RenderState.FillMode = oldFill;
            device.RenderState.CullMode = oldCull;
            device.RenderState.ZBufferWriteEnable = oldZWrite;
            device.RenderState.ZBufferEnable = oldZBuffer;
            device.RenderState.Lighting = oldLighting;
            device.Transform.World = mat;
            this.scale = scale;
        }

        /// <summary>
        /// Draws the yellow arc/wedge showing rotation amount during a drag.
        /// Also draws reference lines from center to start and current positions.
        /// </summary>
        private void drawRotationArc()
        {
            // Calculate number of arc segments based on rotation amount
            int arcSegments = Math.Max(1, (int)(Math.Abs(totalRotation) / (2 * Math.PI) * ringSegments));
            if (arcSegments > ringSegments * 2) arcSegments = ringSegments * 2;

            // Enable alpha blending for semi-transparent arc fill
            bool oldAlpha = device.RenderState.AlphaBlendEnable;
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.SourceBlend = Blend.SourceAlpha;
            device.RenderState.DestinationBlend = Blend.InvSourceAlpha;

            int arcColor = Color.FromArgb(100, 255, 255, 0).ToArgb(); // Semi-transparent yellow

            // Draw filled wedge using TriangleFan: center vertex + arc edge vertices
            CustomVertex.PositionColored[] arcVerts = new CustomVertex.PositionColored[arcSegments + 2];
            arcVerts[0].Position = new Vector3(0, 0, 0);
            arcVerts[0].Color = arcColor;

            float step = totalRotation / arcSegments;

            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = step * i;
                switch (dragAxis)
                {
                    case axis.X: // YZ plane
                        arcVerts[i + 1].Position = new Vector3(0,
                            ringRadius * (float)Math.Cos(angle),
                            ringRadius * (float)Math.Sin(angle));
                        break;
                    case axis.Y: // XZ plane
                        arcVerts[i + 1].Position = new Vector3(
                            ringRadius * (float)Math.Cos(angle),
                            0,
                            ringRadius * (float)Math.Sin(angle));
                        break;
                    case axis.Z: // XY plane
                        arcVerts[i + 1].Position = new Vector3(
                            ringRadius * (float)Math.Cos(angle),
                            ringRadius * (float)Math.Sin(angle),
                            0);
                        break;
                    default:
                        arcVerts[i + 1].Position = new Vector3(0, 0, 0);
                        break;
                }
                arcVerts[i + 1].Color = arcColor;
            }

            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawUserPrimitives(PrimitiveType.TriangleFan, arcSegments, arcVerts);

            // Draw reference lines from center to start and current positions on the ring
            int lineColor = Color.FromArgb(255, 255, 255, 0).ToArgb(); // Solid yellow
            CustomVertex.PositionColored[] lineVerts = new CustomVertex.PositionColored[4];

            // Line from center to start position (angle = 0)
            lineVerts[0].Position = new Vector3(0, 0, 0);
            lineVerts[0].Color = lineColor;
            // Line from center to current position (angle = totalRotation)
            lineVerts[2].Position = new Vector3(0, 0, 0);
            lineVerts[2].Color = lineColor;

            switch (dragAxis)
            {
                case axis.X:
                    lineVerts[1].Position = new Vector3(0, ringRadius, 0);
                    lineVerts[3].Position = new Vector3(0,
                        ringRadius * (float)Math.Cos(totalRotation),
                        ringRadius * (float)Math.Sin(totalRotation));
                    break;
                case axis.Y:
                    lineVerts[1].Position = new Vector3(ringRadius, 0, 0);
                    lineVerts[3].Position = new Vector3(
                        ringRadius * (float)Math.Cos(totalRotation),
                        0,
                        ringRadius * (float)Math.Sin(totalRotation));
                    break;
                case axis.Z:
                    lineVerts[1].Position = new Vector3(ringRadius, 0, 0);
                    lineVerts[3].Position = new Vector3(
                        ringRadius * (float)Math.Cos(totalRotation),
                        ringRadius * (float)Math.Sin(totalRotation),
                        0);
                    break;
            }
            lineVerts[1].Color = lineColor;
            lineVerts[3].Color = lineColor;

            device.DrawUserPrimitives(PrimitiveType.LineList, 2, lineVerts);

            // Draw the arc edge line (solid yellow outline along the wedge edge)
            int edgeSegments = arcSegments;
            CustomVertex.PositionColored[] edgeVerts = new CustomVertex.PositionColored[edgeSegments + 1];
            int edgeColor = Color.Yellow.ToArgb();

            for (int i = 0; i <= edgeSegments; i++)
            {
                float angle = step * i;
                switch (dragAxis)
                {
                    case axis.X:
                        edgeVerts[i].Position = new Vector3(0,
                            ringRadius * (float)Math.Cos(angle),
                            ringRadius * (float)Math.Sin(angle));
                        break;
                    case axis.Y:
                        edgeVerts[i].Position = new Vector3(
                            ringRadius * (float)Math.Cos(angle),
                            0,
                            ringRadius * (float)Math.Sin(angle));
                        break;
                    case axis.Z:
                        edgeVerts[i].Position = new Vector3(
                            ringRadius * (float)Math.Cos(angle),
                            ringRadius * (float)Math.Sin(angle),
                            0);
                        break;
                }
                edgeVerts[i].Color = edgeColor;
            }
            device.DrawUserPrimitives(PrimitiveType.LineStrip, edgeSegments, edgeVerts);

            device.RenderState.AlphaBlendEnable = oldAlpha;
        }

        /// <summary>
        /// The mesh pick.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="mat">The mat.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private List<int> MeshPick(float x, float y, Mesh mesh, Matrix mat)
        {
            Vector3 s = Vector3.Unproject(
                new Vector3(x, y, 0),
                device.Viewport,
                device.Transform.Projection,
                device.Transform.View,
                Matrix.Scaling(scale, scale, scale) * mat);

            Vector3 d = Vector3.Unproject(
                new Vector3(x, y, 1),
                device.Viewport,
                device.Transform.Projection,
                device.Transform.View,
                Matrix.Scaling(scale, scale, scale) * mat);

            Vector3 rPosition = s;
            Vector3 rDirection = Vector3.Normalize(d - s);

            // Collect all intersected subsets with their closest hit distance
            List<KeyValuePair<int, float>> hits = new List<KeyValuePair<int, float>>();
            for (int i = 0; i < meshSubsetCount; i++)
            {
                IntersectInformation closestHit;
                IntersectInformation[] allHits;
                if (mesh.IntersectSubset(i, rPosition, rDirection, out closestHit, out allHits))
                {
                    hits.Add(new KeyValuePair<int, float>(i, closestHit.Dist));
                }
            }

            // Sort by distance so the closest subset is first
            hits.Sort(delegate(KeyValuePair<int, float> a, KeyValuePair<int, float> b)
            {
                return a.Value.CompareTo(b.Value);
            });

            List<int> temp = new List<int>();
            for (int i = 0; i < hits.Count; i++)
            {
                temp.Add(hits[i].Key);
            }

            return temp;
        }

        /// <summary>
        /// The create movement gizmo.
        /// </summary>
        /// <remarks></remarks>
        private void createMovementGizmo()
        {
            int eachVLength = 17; // 9
            int eachILength = 24;
            Mesh m = new Mesh(
                (6 * eachILength + 18) / 3,
                3 * eachVLength + 7,
                MeshFlags.Managed,
                CustomVertex.PositionColored.Format,
                this.device);

            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[eachVLength * 3 + 7];
            for (int i = 0; i < 3; i++)
            {
                Color tempColor = i == 0 ? Color.DarkRed : i == 1 ? Color.DarkGreen : Color.DarkBlue;
                for (int ii = 1; ii < eachVLength; ii++)
                {
                    vertices[i * eachVLength + ii].Color = tempColor.ToArgb();
                }

                vertices[i * eachVLength + 0].Color = tempColor.ToArgb();
            }

            // X Cone (Red)
            vertices[0 * eachVLength + 0].Position = new Vector3(13f, 0f, 0f);
            vertices[0 * eachVLength + 1].Position = new Vector3(10f, 0f, 1f);
            vertices[0 * eachVLength + 2].Position = new Vector3(10f, 0.7f, 0.7f);
            vertices[0 * eachVLength + 3].Position = new Vector3(10f, 1.0f, 0f);
            vertices[0 * eachVLength + 4].Position = new Vector3(10f, 0.7f, -0.7f);
            vertices[0 * eachVLength + 5].Position = new Vector3(10f, 0f, -1.0f);
            vertices[0 * eachVLength + 6].Position = new Vector3(10f, -0.7f, -0.7f);
            vertices[0 * eachVLength + 7].Position = new Vector3(10f, -1.0f, 0f);
            vertices[0 * eachVLength + 8].Position = new Vector3(10f, -0.7f, 0.7f);

            // X Top Shaft (Not Visible, for mouse intersect only!)
            vertices[0 * eachVLength + 9].Position = new Vector3(5f, -0.25f, 0.25f);
            vertices[0 * eachVLength + 10].Position = new Vector3(5f, 0.25f, 0.25f);
            vertices[0 * eachVLength + 11].Position = new Vector3(5f, 0.25f, -0.25f);
            vertices[0 * eachVLength + 12].Position = new Vector3(5f, -0.25f, -0.25f);
            vertices[0 * eachVLength + 13].Position = new Vector3(10f, -0.25f, 0.25f);
            vertices[0 * eachVLength + 14].Position = new Vector3(10f, 0.25f, 0.25f);
            vertices[0 * eachVLength + 15].Position = new Vector3(10f, 0.25f, -0.25f);
            vertices[0 * eachVLength + 16].Position = new Vector3(10f, -0.25f, -0.25f);

            // Y Cone (Green)
            vertices[1 * eachVLength + 0].Position = new Vector3(0f, 13f, 0f);
            vertices[1 * eachVLength + 1].Position = new Vector3(0f, 10f, 1f);
            vertices[1 * eachVLength + 2].Position = new Vector3(0.7f, 10f, 0.7f);
            vertices[1 * eachVLength + 3].Position = new Vector3(1.0f, 10f, 0f);
            vertices[1 * eachVLength + 4].Position = new Vector3(0.7f, 10f, -0.7f);
            vertices[1 * eachVLength + 5].Position = new Vector3(0f, 10f, -1.0f);
            vertices[1 * eachVLength + 6].Position = new Vector3(-0.7f, 10f, -0.7f);
            vertices[1 * eachVLength + 7].Position = new Vector3(-1.0f, 10f, 0f);
            vertices[1 * eachVLength + 8].Position = new Vector3(-0.7f, 10f, 0.7f);

            // Y Top Shaft (Not Visible)
            vertices[1 * eachVLength + 9].Position = new Vector3(-0.25f, 5f, 0.25f);
            vertices[1 * eachVLength + 10].Position = new Vector3(0.25f, 5f, 0.25f);
            vertices[1 * eachVLength + 11].Position = new Vector3(0.25f, 5f, -0.25f);
            vertices[1 * eachVLength + 12].Position = new Vector3(-0.25f, 5f, -0.25f);
            vertices[1 * eachVLength + 13].Position = new Vector3(-0.25f, 10f, 0.25f);
            vertices[1 * eachVLength + 14].Position = new Vector3(0.25f, 10f, 0.25f);
            vertices[1 * eachVLength + 15].Position = new Vector3(0.25f, 10f, -0.25f);
            vertices[1 * eachVLength + 16].Position = new Vector3(-0.25f, 10f, -0.25f);

            // Z Cone (Blue)
            vertices[2 * eachVLength + 0].Position = new Vector3(0f, 0f, 13f);
            vertices[2 * eachVLength + 1].Position = new Vector3(0f, 1.0f, 10f);
            vertices[2 * eachVLength + 2].Position = new Vector3(0.7f, 0.7f, 10f);
            vertices[2 * eachVLength + 3].Position = new Vector3(1.0f, 0.0f, 10f);
            vertices[2 * eachVLength + 4].Position = new Vector3(0.7f, -0.7f, 10f);
            vertices[2 * eachVLength + 5].Position = new Vector3(0f, -1.0f, 10f);
            vertices[2 * eachVLength + 6].Position = new Vector3(-0.7f, -0.7f, 10f);
            vertices[2 * eachVLength + 7].Position = new Vector3(-1.0f, 0.0f, 10f);
            vertices[2 * eachVLength + 8].Position = new Vector3(-0.7f, 0.7f, 10f);

            // Z Top Shaft (Not Visible)
            vertices[2 * eachVLength + 9].Position = new Vector3(-0.25f, 0.25f, 5f);
            vertices[2 * eachVLength + 10].Position = new Vector3(0.25f, 0.25f, 5f);
            vertices[2 * eachVLength + 11].Position = new Vector3(0.25f, -0.25f, 5f);
            vertices[2 * eachVLength + 12].Position = new Vector3(-0.25f, -0.25f, 5f);
            vertices[2 * eachVLength + 13].Position = new Vector3(-0.25f, 0.25f, 10f);
            vertices[2 * eachVLength + 14].Position = new Vector3(0.25f, 0.25f, 10f);
            vertices[2 * eachVLength + 15].Position = new Vector3(0.25f, -0.25f, 10f);
            vertices[2 * eachVLength + 16].Position = new Vector3(-0.25f, -0.25f, 10f);

            // X Bottom Square Basic
            vertices[3 * eachVLength + 0].Position = new Vector3(0f, 0f, 0f);
            vertices[3 * eachVLength + 1].Position = new Vector3(5f, 0f, 0f);
            vertices[3 * eachVLength + 2].Position = new Vector3(0f, 5f, 0f);
            vertices[3 * eachVLength + 3].Position = new Vector3(0f, 0f, 5f);
            vertices[3 * eachVLength + 4].Position = new Vector3(5f, 5f, 0f);
            vertices[3 * eachVLength + 5].Position = new Vector3(5f, 0f, 5f);
            vertices[3 * eachVLength + 6].Position = new Vector3(0f, 5f, 5f);

            m.SetVertexBufferData(vertices, LockFlags.None);

            #region Indices Declaration

            short[] indices = new short[eachILength * 6 + 18];
            for (int i = 0; i < 3; i++)
            {
                // Cone
                indices[i * eachILength + 0] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 1] = (short)(i * eachVLength + 1);
                indices[i * eachILength + 2] = (short)(i * eachVLength + 2);

                indices[i * eachILength + 3] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 4] = (short)(i * eachVLength + 2);
                indices[i * eachILength + 5] = (short)(i * eachVLength + 3);

                indices[i * eachILength + 6] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 7] = (short)(i * eachVLength + 3);
                indices[i * eachILength + 8] = (short)(i * eachVLength + 4);

                indices[i * eachILength + 9] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 10] = (short)(i * eachVLength + 4);
                indices[i * eachILength + 11] = (short)(i * eachVLength + 5);

                indices[i * eachILength + 12] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 13] = (short)(i * eachVLength + 5);
                indices[i * eachILength + 14] = (short)(i * eachVLength + 6);

                indices[i * eachILength + 15] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 16] = (short)(i * eachVLength + 6);
                indices[i * eachILength + 17] = (short)(i * eachVLength + 7);

                indices[i * eachILength + 18] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 19] = (short)(i * eachVLength + 7);
                indices[i * eachILength + 20] = (short)(i * eachVLength + 8);

                indices[i * eachILength + 21] = (short)(i * eachVLength + 0);
                indices[i * eachILength + 22] = (short)(i * eachVLength + 8);
                indices[i * eachILength + 23] = (short)(i * eachVLength + 1);
            }

            for (int i = 3; i < 6; i++)
            {
                // Shaft
                indices[i * eachILength + 0] = (short)((i - 3) * eachVLength + 9);
                indices[i * eachILength + 1] = (short)((i - 3) * eachVLength + 13);
                indices[i * eachILength + 2] = (short)((i - 3) * eachVLength + 10);
                indices[i * eachILength + 3] = (short)((i - 3) * eachVLength + 10);
                indices[i * eachILength + 4] = (short)((i - 3) * eachVLength + 13);
                indices[i * eachILength + 5] = (short)((i - 3) * eachVLength + 14);

                indices[i * eachILength + 6] = (short)((i - 3) * eachVLength + 10);
                indices[i * eachILength + 7] = (short)((i - 3) * eachVLength + 14);
                indices[i * eachILength + 8] = (short)((i - 3) * eachVLength + 11);
                indices[i * eachILength + 9] = (short)((i - 3) * eachVLength + 11);
                indices[i * eachILength + 10] = (short)((i - 3) * eachVLength + 14);
                indices[i * eachILength + 11] = (short)((i - 3) * eachVLength + 15);

                indices[i * eachILength + 12] = (short)((i - 3) * eachVLength + 11);
                indices[i * eachILength + 13] = (short)((i - 3) * eachVLength + 15);
                indices[i * eachILength + 14] = (short)((i - 3) * eachVLength + 12);
                indices[i * eachILength + 15] = (short)((i - 3) * eachVLength + 12);
                indices[i * eachILength + 16] = (short)((i - 3) * eachVLength + 15);
                indices[i * eachILength + 17] = (short)((i - 3) * eachVLength + 16);

                indices[i * eachILength + 18] = (short)((i - 3) * eachVLength + 12);
                indices[i * eachILength + 19] = (short)((i - 3) * eachVLength + 16);
                indices[i * eachILength + 20] = (short)((i - 3) * eachVLength + 13);
                indices[i * eachILength + 21] = (short)((i - 3) * eachVLength + 12);
                indices[i * eachILength + 22] = (short)((i - 3) * eachVLength + 13);
                indices[i * eachILength + 23] = (short)((i - 3) * eachVLength + 9);
            }

            // Squares
            indices[6 * eachILength + 0] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 1] = (short)(3 * eachVLength + 1);
            indices[6 * eachILength + 2] = (short)(3 * eachVLength + 4);
            indices[6 * eachILength + 3] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 4] = (short)(3 * eachVLength + 2);
            indices[6 * eachILength + 5] = (short)(3 * eachVLength + 4);

            indices[6 * eachILength + 6] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 7] = (short)(3 * eachVLength + 2);
            indices[6 * eachILength + 8] = (short)(3 * eachVLength + 6);
            indices[6 * eachILength + 9] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 10] = (short)(3 * eachVLength + 3);
            indices[6 * eachILength + 11] = (short)(3 * eachVLength + 6);

            indices[6 * eachILength + 12] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 13] = (short)(3 * eachVLength + 3);
            indices[6 * eachILength + 14] = (short)(3 * eachVLength + 5);
            indices[6 * eachILength + 15] = (short)(3 * eachVLength + 0);
            indices[6 * eachILength + 16] = (short)(3 * eachVLength + 1);
            indices[6 * eachILength + 17] = (short)(3 * eachVLength + 5);

            m.SetIndexBufferData(indices, LockFlags.None);

            #endregion

            int[] attr = m.LockAttributeBufferArray(LockFlags.Discard);
            int step = eachILength / 3; // 1 face = 3 indices
            for (int i = 0; i < step; i++)
            {
                attr[step * 0 + i] = 0;
                attr[step * 1 + i] = 1;
                attr[step * 2 + i] = 2;
                attr[step * 3 + i] = 3;
                attr[step * 4 + i] = 4;
                attr[step * 5 + i] = 5;
            }

            for (int i = step * 6; i < attr.Length; i++)
            {
                attr[i] = 6 + (i - step * 6) / 2;
            }

            m.UnlockAttributeBuffer(attr);
            int[] adj = new int[m.NumberFaces * 3];
            m.GenerateAdjacency(0.001f, adj);
            m.OptimizeInPlace(MeshFlags.OptimizeVertexCache, adj);
            this.gizmo = m;
        }

        /// <summary>
        /// Creates the rotation gizmo mesh (flat ring annuli for ray picking).
        /// 3 subsets: 0=X (YZ plane), 1=Y (XZ plane), 2=Z (XY plane).
        /// </summary>
        private void createRotationGizmo()
        {
            int vertsPerRing = ringSegments * 2;
            int trisPerRing = ringSegments * 2;
            int totalVerts = vertsPerRing * 3;
            int totalTris = trisPerRing * 3;

            Mesh m = new Mesh(
                totalTris,
                totalVerts,
                MeshFlags.Managed,
                CustomVertex.PositionColored.Format,
                this.device);

            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[totalVerts];
            short[] indices = new short[totalTris * 3];

            for (int ring = 0; ring < 3; ring++)
            {
                Color c = ring == 0 ? Color.DarkRed : ring == 1 ? Color.DarkGreen : Color.DarkBlue;
                int vBase = ring * vertsPerRing;
                int iBase = ring * trisPerRing * 3;

                for (int seg = 0; seg < ringSegments; seg++)
                {
                    float angle = (float)(2 * Math.PI * seg / ringSegments);
                    float cos = (float)Math.Cos(angle);
                    float sin = (float)Math.Sin(angle);

                    int innerIdx = vBase + seg * 2;
                    int outerIdx = vBase + seg * 2 + 1;

                    switch (ring)
                    {
                        case 0: // X ring in YZ plane
                            vertices[innerIdx].Position = new Vector3(0, ringInnerR * cos, ringInnerR * sin);
                            vertices[outerIdx].Position = new Vector3(0, ringOuterR * cos, ringOuterR * sin);
                            break;
                        case 1: // Y ring in XZ plane
                            vertices[innerIdx].Position = new Vector3(ringInnerR * cos, 0, ringInnerR * sin);
                            vertices[outerIdx].Position = new Vector3(ringOuterR * cos, 0, ringOuterR * sin);
                            break;
                        case 2: // Z ring in XY plane
                            vertices[innerIdx].Position = new Vector3(ringInnerR * cos, ringInnerR * sin, 0);
                            vertices[outerIdx].Position = new Vector3(ringOuterR * cos, ringOuterR * sin, 0);
                            break;
                    }

                    vertices[innerIdx].Color = c.ToArgb();
                    vertices[outerIdx].Color = c.ToArgb();

                    int nextSeg = (seg + 1) % ringSegments;
                    int nextInnerIdx = vBase + nextSeg * 2;
                    int nextOuterIdx = vBase + nextSeg * 2 + 1;

                    int triIdx = iBase + seg * 6;
                    indices[triIdx + 0] = (short)innerIdx;
                    indices[triIdx + 1] = (short)outerIdx;
                    indices[triIdx + 2] = (short)nextInnerIdx;
                    indices[triIdx + 3] = (short)nextInnerIdx;
                    indices[triIdx + 4] = (short)outerIdx;
                    indices[triIdx + 5] = (short)nextOuterIdx;
                }
            }

            m.SetVertexBufferData(vertices, LockFlags.None);
            m.SetIndexBufferData(indices, LockFlags.None);

            int[] attr = m.LockAttributeBufferArray(LockFlags.Discard);
            for (int ring = 0; ring < 3; ring++)
            {
                for (int tri = 0; tri < trisPerRing; tri++)
                {
                    attr[ring * trisPerRing + tri] = ring;
                }
            }
            m.UnlockAttributeBuffer(attr);

            int[] adj = new int[m.NumberFaces * 3];
            m.GenerateAdjacency(0.001f, adj);
            m.OptimizeInPlace(MeshFlags.OptimizeVertexCache, adj);
            this.gizmo = m;
        }

        #endregion
    }
}
