using Bonobo.Bepuphysics2;
using Bonobo.Bepuphysics2.Collidables;
using Bonobo.Bepuphysics2.CollisionDetection;
using Bonobo.Bepuphysics2.Constraints;
using Bonobo.BepuUtilities;
using Bonobo.BepuUtilities.Memory;
using System;
using System.Numerics;

namespace Bonobo.Bepu.AotProbe
{
    internal struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation) { }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = 1f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1);
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

        public void Dispose() { }
    }

    internal struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;
        public float LinearDamping;
        public float AngularDamping;
        Vector3Wide gravityWideDt;
        Vector<float> linearDampingDt;
        Vector<float> angularDampingDt;

        public PoseIntegratorCallbacks(Vector3 gravity, float linearDamping = 0.03f, float angularDamping = 0.03f) : this()
        {
            Gravity = gravity;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }

        public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public readonly bool AllowSubstepsForUnconstrainedBodies => false;
        public readonly bool IntegrateVelocityForKinematics => false;

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt)
        {
            linearDampingDt = new Vector<float>(MathF.Pow(MathF.Max(0f, 1f - LinearDamping), dt));
            angularDampingDt = new Vector<float>(MathF.Pow(MathF.Max(0f, 1f - AngularDamping), dt));
            Vector3Wide.Broadcast(Gravity * dt, out gravityWideDt);
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia,
            Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            // Deterministic single-threaded path: no dispatcher, no worker indexing.
            velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
            velocity.Angular = velocity.Angular * angularDampingDt;
        }
    }

    public static class Program
    {
        public static int Main()
        {
            var bufferPool = new BufferPool();
            var simulation = Simulation.Create(
                bufferPool,
                new NarrowPhaseCallbacks(),
                new PoseIntegratorCallbacks(new Vector3(0, -10, 0)),
                new SolveDescription(8, 1));

            var boxShape = new Box(1, 1, 1);
            var boxInertia = boxShape.ComputeInertia(1);
            var boxIndex = simulation.Shapes.Add(boxShape);
            simulation.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose(new Vector3(0, 5, 0)),
                boxInertia,
                new CollidableDescription(boxIndex, 0.1f),
                new BodyActivityDescription(-1f, 255)));

            var groundShape = new Box(100, 1, 100);
            var groundIndex = simulation.Shapes.Add(groundShape);
            simulation.Statics.Add(new StaticDescription(new Vector3(0, -0.5f, 0), Quaternion.Identity, groundIndex));

            // Warm up lazy initialization and pool growth paths (body lands, contact constraints are allocated)
            // before measuring steady-state allocation. Negative sleep threshold keeps the body awake.
            for (int i = 0; i < 300; ++i)
            {
                simulation.Timestep(1f / 60f);
            }

            var bodyHandle = new BodyHandle(0);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 256; ++i)
            {
                simulation.Timestep(1f / 60f, null);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            var pose = simulation.Bodies[bodyHandle].Pose;
            Console.WriteLine($"Bonobo.Bepuphysics2 AOT probe");
            Console.WriteLine($"version: {typeof(Simulation).Assembly.GetName().Version}");
            Console.WriteLine($"body position: ({pose.Position.X:F4}, {pose.Position.Y:F4}, {pose.Position.Z:F4})");
            Console.WriteLine($"allocated bytes over 256 null-dispatcher steps: {allocated}");

            simulation.Dispose();
            bufferPool.Clear();

            var ok = allocated == 0;
            Console.WriteLine(ok ? "PASS: zero steady-state allocation" : "FAIL: steady-state allocation detected");
            return ok ? 0 : 1;
        }
    }
}
