using UnityEngine;

namespace WorldOfVictoria.Rendering.Character
{
    public sealed class ZombieModel
    {
        private readonly Transform visualRoot;
        private readonly Transform headPivot;
        private readonly Transform leftArmPivot;
        private readonly Transform rightArmPivot;
        private readonly Transform leftLegPivot;
        private readonly Transform rightLegPivot;

        private readonly ModelCube head;
        private readonly ModelCube body;
        private readonly ModelCube leftArm;
        private readonly ModelCube rightArm;
        private readonly ModelCube leftLeg;
        private readonly ModelCube rightLeg;

        public ZombieModel(Transform parent, Material material)
        {
            visualRoot = new GameObject("ZombieVisualRoot").transform;
            visualRoot.SetParent(parent, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            body = new ModelCube(
                "Body",
                visualRoot,
                new Vector3(0f, 1.125f, 0f),
                new Vector3(8f, 12f, 4f),
                material,
                new ModelCubeUvLayout(
                    new RectInt(20, 16, 8, 4),
                    new RectInt(28, 16, 8, 4),
                    new RectInt(16, 20, 4, 12),
                    new RectInt(20, 20, 8, 12),
                    new RectInt(28, 20, 4, 12),
                    new RectInt(32, 20, 8, 12)));

            headPivot = CreatePivot("HeadPivot", visualRoot, new Vector3(0f, 1.5f, 0f));
            head = new ModelCube(
                "Head",
                headPivot,
                Vector3.zero,
                new Vector3(8f, 8f, 8f),
                material,
                new ModelCubeUvLayout(
                    new RectInt(8, 0, 8, 8),
                    new RectInt(16, 0, 8, 8),
                    new RectInt(0, 8, 8, 8),
                    new RectInt(8, 8, 8, 8),
                    new RectInt(16, 8, 8, 8),
                    new RectInt(24, 8, 8, 8)));

            leftArmPivot = CreatePivot("LeftArmPivot", visualRoot, new Vector3(-0.375f, 1.375f, 0f));
            leftArm = CreateArm("LeftArm", leftArmPivot, material);

            rightArmPivot = CreatePivot("RightArmPivot", visualRoot, new Vector3(0.375f, 1.375f, 0f));
            rightArm = CreateArm("RightArm", rightArmPivot, material);

            leftLegPivot = CreatePivot("LeftLegPivot", visualRoot, new Vector3(-0.125f, 0.75f, 0f));
            leftLeg = CreateLeg("LeftLeg", leftLegPivot, material);

            rightLegPivot = CreatePivot("RightLegPivot", visualRoot, new Vector3(0.125f, 0.75f, 0f));
            rightLeg = CreateLeg("RightLeg", rightLegPivot, material);
        }

        public Transform VisualRoot => visualRoot;

        public void SetMaterial(Material material)
        {
            head.SetMaterial(material);
            body.SetMaterial(material);
            leftArm.SetMaterial(material);
            rightArm.SetMaterial(material);
            leftLeg.SetMaterial(material);
            rightLeg.SetMaterial(material);
        }

        public void ApplyPose(float walkPhase, float moveAmount, float idleTime, float yawDegrees, float headPitchDegrees)
        {
            visualRoot.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var armSwing = Mathf.Sin(walkPhase) * 30f * Mathf.Clamp01(moveAmount);
            var legSwing = Mathf.Sin(walkPhase) * 45f * Mathf.Clamp01(moveAmount);
            var idleBob = Mathf.Sin(idleTime * 1.5f) * 0.02f;

            headPivot.localPosition = new Vector3(0f, 1.5f + idleBob, 0f);
            headPivot.localRotation = Quaternion.Euler(headPitchDegrees + idleBob * 35f, 0f, 0f);

            leftArmPivot.localRotation = Quaternion.Euler(armSwing, 0f, 0f);
            rightArmPivot.localRotation = Quaternion.Euler(-armSwing, 0f, 0f);
            leftLegPivot.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            pivot.localRotation = Quaternion.identity;
            return pivot;
        }

        private static ModelCube CreateArm(string name, Transform pivot, Material material)
        {
            return new ModelCube(
                name,
                pivot,
                new Vector3(0f, -0.375f, 0f),
                new Vector3(4f, 12f, 4f),
                material,
                new ModelCubeUvLayout(
                    new RectInt(44, 16, 4, 4),
                    new RectInt(48, 16, 4, 4),
                    new RectInt(40, 20, 4, 12),
                    new RectInt(44, 20, 4, 12),
                    new RectInt(48, 20, 4, 12),
                    new RectInt(52, 20, 4, 12)));
        }

        private static ModelCube CreateLeg(string name, Transform pivot, Material material)
        {
            return new ModelCube(
                name,
                pivot,
                new Vector3(0f, -0.375f, 0f),
                new Vector3(4f, 12f, 4f),
                material,
                new ModelCubeUvLayout(
                    new RectInt(4, 16, 4, 4),
                    new RectInt(8, 16, 4, 4),
                    new RectInt(0, 20, 4, 12),
                    new RectInt(4, 20, 4, 12),
                    new RectInt(8, 20, 4, 12),
                    new RectInt(12, 20, 4, 12)));
        }
    }
}
