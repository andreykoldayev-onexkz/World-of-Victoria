using UnityEngine;

namespace WorldOfVictoria.Utilities
{
    public struct VoxelAabb
    {
        public VoxelAabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Center => (Min + Max) * 0.5f;

        public VoxelAabb Expand(Vector3 delta)
        {
            var min = Min;
            var max = Max;

            if (delta.x < 0f) min.x += delta.x; else max.x += delta.x;
            if (delta.y < 0f) min.y += delta.y; else max.y += delta.y;
            if (delta.z < 0f) min.z += delta.z; else max.z += delta.z;

            return new VoxelAabb(min, max);
        }

        public void Move(Vector3 delta)
        {
            Min += delta;
            Max += delta;
        }

        public float ClipXCollide(VoxelAabb other, float deltaX)
        {
            if (other.Max.y <= Min.y || other.Min.y >= Max.y) return deltaX;
            if (other.Max.z <= Min.z || other.Min.z >= Max.z) return deltaX;

            if (deltaX > 0f && other.Max.x <= Min.x)
            {
                var max = Min.x - other.Max.x;
                if (max < deltaX) deltaX = max;
            }

            if (deltaX < 0f && other.Min.x >= Max.x)
            {
                var max = Max.x - other.Min.x;
                if (max > deltaX) deltaX = max;
            }

            return deltaX;
        }

        public float ClipYCollide(VoxelAabb other, float deltaY)
        {
            if (other.Max.x <= Min.x || other.Min.x >= Max.x) return deltaY;
            if (other.Max.z <= Min.z || other.Min.z >= Max.z) return deltaY;

            if (deltaY > 0f && other.Max.y <= Min.y)
            {
                var max = Min.y - other.Max.y;
                if (max < deltaY) deltaY = max;
            }

            if (deltaY < 0f && other.Min.y >= Max.y)
            {
                var max = Max.y - other.Min.y;
                if (max > deltaY) deltaY = max;
            }

            return deltaY;
        }

        public float ClipZCollide(VoxelAabb other, float deltaZ)
        {
            if (other.Max.x <= Min.x || other.Min.x >= Max.x) return deltaZ;
            if (other.Max.y <= Min.y || other.Min.y >= Max.y) return deltaZ;

            if (deltaZ > 0f && other.Max.z <= Min.z)
            {
                var max = Min.z - other.Max.z;
                if (max < deltaZ) deltaZ = max;
            }

            if (deltaZ < 0f && other.Min.z >= Max.z)
            {
                var max = Max.z - other.Min.z;
                if (max > deltaZ) deltaZ = max;
            }

            return deltaZ;
        }

        public bool Intersects(VoxelAabb other)
        {
            return Max.x > other.Min.x && Min.x < other.Max.x
                && Max.y > other.Min.y && Min.y < other.Max.y
                && Max.z > other.Min.z && Min.z < other.Max.z;
        }
    }
}
