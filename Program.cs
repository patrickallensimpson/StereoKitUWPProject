using StereoKit;
using System;

namespace StereoKitUWPProject
{
    struct Entity
    {
        public Pose pose;
        public Model model;
        public Color color;
    }
    class Program
    {
        static Entity[] displayUI(Entity[] entities, ref Pose windowPose, ref Pose diskPose, ref bool rulerActive, ref float slider, Random rand)
        {
            Entity[] result = entities;
            UI.WindowBegin("Window", ref windowPose, new Vec2(20f, 0f) * U.cm, UIWin.Normal);

            UI.Label("Slide");
            UI.SameLine();
            UI.HSlider("slider", ref slider, 0f, 1f, 0.2f, 72f * U.mm);

            if (UI.Button("Add new")) {
                var depth = -(0.3f + (((float)rand.NextDouble()) * 0.4f));
                var vertical = 0.2f - (((float)rand.NextDouble()) * 0.4f);
                var horizontal = 0.2f - (((float)rand.NextDouble()) * 0.4f);
                var color = Color.HSV(slider, 0.5f, 0.8f).ToLinear();
                var newCube = new Entity() {
                    pose = new Pose(horizontal, vertical, depth, Quat.Identity),
                    model = Model.FromMesh(
                        Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                        Default.MaterialUI
                    ),
                    color = color
                };
                result = new Entity[result.Length+1];
                System.Array.Copy(entities, result, entities.Length);
                result[entities.Length] = newCube;
            }

            Model disk = Model.FromFile("test.glb");
            UI.HandleBegin("Clip", ref diskPose, disk.Bounds);
            Renderer.Add(disk, Matrix.Identity);
            UI.HandleEnd();

            UI.Toggle("Active Ruler", ref rulerActive);

            if (UI.Button("Exit")) {
                SK.Quit();
            }

            UI.WindowEnd();

            return result;
        }
        static void Main(string[] args)
        {
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "StereoKitUWPProject",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            var rulerColor = Color.HSV(0.58f, 0.9f, 0.9f);

            Pose windowPose = new Pose(0f, 0f, -0.50f, Quat.LookDir(1f, 0f, 1f));
            Pose diskPose = new Pose(0f, 0.3f, -0.50f, Quat.LookDir(1f, 0f, 1f));
            bool showHeader = true;
            float slider = 0.5f;

            Random rand = new System.Random();


            // Create assets used by the app
            var cube = new Entity()
                {
                    pose = new Pose(0, 0, -0.5f, Quat.Identity),
                    model = Model.FromMesh(
                    Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                    Default.MaterialUI),
                    color = Color.White
                };

            var proxy = new Entity()
            {
                pose = new Pose(0f, -0.4f, 0.5f, Quat.Identity),
                model = Model.FromMesh(
                                Mesh.GenerateSphere(0.1f, 4),
                                Default.MaterialUI
                            ),
                color = new Color(0,0,1)
            };

            var entities = new Entity[] { cube };

            Matrix floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
            Material floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            var rulerActive = false;
            var rulerStart = Vec3.Zero;
            var rulerEnd = Vec3.Zero;


            // Core application loop
            while (SK.Step(() =>
            {
                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);

                for (int i = 0; i < entities.Length; i++)
                {
                    UI.Handle("Cube"+i, ref entities[i].pose, entities[i].model.Bounds);

                    // if the ruler is Active then allow pinch to activate a ruler
                    if (rulerActive)
                    {
                        var leftHand = Input.Hand(Handed.Left);
                        var rightHand = Input.Hand(Handed.Right);
                        if (leftHand.IsPinched && rightHand.IsPinched)
                        {
                            rulerStart = leftHand.Get(FingerId.Index, JointId.Tip).position;
                        }
                        
                        if (rightHand.IsJustPinched && !leftHand.IsPinched)
                        {
                            // we just pinched the right hand so record the position of the ruler
                            rulerStart = rightHand.Get(FingerId.Index, JointId.Tip).position;
                        }
                        if (rightHand.IsPinched)
                        {
                            rulerEnd = rightHand.Get(FingerId.Index, JointId.Tip).position;
                            //var length = v.Length;
                            //var midpoint = rulerStart + (v / 2.0);
                            //v.
                            //var v
                            //Text.Add()
                            Lines.Add(rulerStart, rulerEnd, rulerColor, 0.1f * U.cm);
                            var v = (rulerEnd - rulerStart);
                            var midpoint = rulerStart + (v / 2f);
                            var rot = Quat.LookAt(midpoint, Input.Head.position);
                            var style = TextStyle.Default;
                            style.Material.SetColor("color", rulerColor);
                            Text.Add("Length: " + (v.Length * 0.39370079f / U.cm) + "in", Matrix.TRS(midpoint, rot, 1),style);
                        }
                    }

                    entities[i].model.Draw(entities[i].pose.ToMatrix(), entities[i].color);
                }

                entities = displayUI(entities, ref windowPose, ref diskPose, ref rulerActive, ref slider, rand);
            })) ;
            SK.Shutdown();
        }
    }
}
