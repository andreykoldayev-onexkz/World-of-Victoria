using UnityEngine;

namespace WorldOfVictoria.Core
{
    public static class BlockRaycaster
    {
        public static bool RaycastDda(Vector3 origin, Vector3 direction, float maxDistance, WorldData worldData, out HitResult hitResult)
        {
            hitResult = default;

            if (worldData == null || maxDistance <= 0f || direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction.Normalize();

            var x = Mathf.FloorToInt(origin.x);
            var y = Mathf.FloorToInt(origin.y);
            var z = Mathf.FloorToInt(origin.z);

            var stepX = direction.x >= 0f ? 1 : -1;
            var stepY = direction.y >= 0f ? 1 : -1;
            var stepZ = direction.z >= 0f ? 1 : -1;

            var tMaxX = IntBound(origin.x, direction.x);
            var tMaxY = IntBound(origin.y, direction.y);
            var tMaxZ = IntBound(origin.z, direction.z);

            var tDeltaX = Mathf.Approximately(direction.x, 0f) ? float.PositiveInfinity : Mathf.Abs(1f / direction.x);
            var tDeltaY = Mathf.Approximately(direction.y, 0f) ? float.PositiveInfinity : Mathf.Abs(1f / direction.y);
            var tDeltaZ = Mathf.Approximately(direction.z, 0f) ? float.PositiveInfinity : Mathf.Abs(1f / direction.z);

            var face = -1;

            while (true)
            {
                if (worldData.InBounds(x, y, z) && worldData.IsSolidBlock(x, y, z))
                {
                    hitResult = new HitResult(x, y, z, face < 0 ? 3 : face);
                    return true;
                }

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        if (tMaxX > maxDistance)
                        {
                            break;
                        }

                        x += stepX;
                        face = stepX > 0 ? 0 : 1;
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        if (tMaxZ > maxDistance)
                        {
                            break;
                        }

                        z += stepZ;
                        face = stepZ > 0 ? 4 : 5;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        if (tMaxY > maxDistance)
                        {
                            break;
                        }

                        y += stepY;
                        face = stepY > 0 ? 2 : 3;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        if (tMaxZ > maxDistance)
                        {
                            break;
                        }

                        z += stepZ;
                        face = stepZ > 0 ? 4 : 5;
                        tMaxZ += tDeltaZ;
                    }
                }
            }

            return false;
        }

        private static float IntBound(float s, float ds)
        {
            if (Mathf.Approximately(ds, 0f))
            {
                return float.PositiveInfinity;
            }

            if (ds < 0f)
            {
                return IntBound(-s, -ds);
            }

            s = Mod(s, 1f);
            return (1f - s) / ds;
        }

        private static float Mod(float value, float modulus)
        {
            return (value % modulus + modulus) % modulus;
        }
    }
}
