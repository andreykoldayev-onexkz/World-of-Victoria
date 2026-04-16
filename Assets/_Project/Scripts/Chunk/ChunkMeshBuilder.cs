using UnityEngine;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkMeshBuilder
    {
        private static readonly Vector3[] BottomFace =
        {
            new(0f, 0f, 1f),
            new(0f, 0f, 0f),
            new(1f, 0f, 0f),
            new(1f, 0f, 1f)
        };

        private static readonly Vector3[] TopFace =
        {
            new(1f, 1f, 1f),
            new(1f, 1f, 0f),
            new(0f, 1f, 0f),
            new(0f, 1f, 1f)
        };

        private static readonly Vector3[] NorthFace =
        {
            new(0f, 1f, 0f),
            new(1f, 1f, 0f),
            new(1f, 0f, 0f),
            new(0f, 0f, 0f)
        };

        private static readonly Vector3[] SouthFace =
        {
            new(0f, 1f, 1f),
            new(0f, 0f, 1f),
            new(1f, 0f, 1f),
            new(1f, 1f, 1f)
        };

        private static readonly Vector3[] WestFace =
        {
            new(0f, 1f, 1f),
            new(0f, 1f, 0f),
            new(0f, 0f, 0f),
            new(0f, 0f, 1f)
        };

        private static readonly Vector3[] EastFace =
        {
            new(1f, 0f, 1f),
            new(1f, 0f, 0f),
            new(1f, 1f, 0f),
            new(1f, 1f, 1f)
        };

        private static readonly Vector2[] QuadUVs =
        {
            new(0f, 1f),
            new(0f, 0f),
            new(1f, 0f),
            new(1f, 1f)
        };

        private static readonly Vector2[] NorthFaceUVs =
        {
            new(0f, 1f),
            new(1f, 1f),
            new(1f, 0f),
            new(0f, 0f)
        };

        private static readonly Vector2[] SouthFaceUVs =
        {
            new(0f, 1f),
            new(0f, 0f),
            new(1f, 0f),
            new(1f, 1f)
        };

        private static readonly Vector2[] WestFaceUVs =
        {
            new(0f, 1f),
            new(1f, 1f),
            new(1f, 0f),
            new(0f, 0f)
        };

        private static readonly Vector2[] EastFaceUVs =
        {
            new(0f, 0f),
            new(1f, 0f),
            new(1f, 1f),
            new(0f, 1f)
        };

        private static readonly Vector3 BottomNormal = Vector3.down;
        private static readonly Vector3 TopNormal = Vector3.up;
        private static readonly Vector3 NorthNormal = Vector3.back;
        private static readonly Vector3 SouthNormal = Vector3.forward;
        private static readonly Vector3 WestNormal = Vector3.left;
        private static readonly Vector3 EastNormal = Vector3.right;

        private static readonly Vector4 HorizontalTangent = new(1f, 0f, 0f, 1f);
        private static readonly Vector4 VerticalTangent = new(0f, 0f, 1f, 1f);

        public ChunkMeshData Build(ChunkData chunkData, WorldData worldData)
        {
            var meshData = new ChunkMeshData();
            Fill(meshData, chunkData, worldData);
            return meshData;
        }

        public void Fill(ChunkMeshData meshData, ChunkData chunkData, WorldData worldData)
        {
            meshData.Clear();

            var bounds = chunkData.BlockBounds;
            for (var x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (var y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (var z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        if (!worldData.IsSolidBlock(x, y, z))
                        {
                            continue;
                        }

                        var blockType = ResolveBlockType(worldData, x, y, z);
                        var tileId = ResolveTileId(blockType);
                        AppendVisibleFaces(meshData, worldData, x, y, z, blockType, tileId);
                    }
                }
            }
        }

        private static byte ResolveBlockType(WorldData worldData, int x, int y, int z)
        {
            var topExposed = !worldData.IsSolidBlock(x, y + 1, z);
            var nearSurface = y >= worldData.Depth - 8;
            var skylit = worldData.GetSkyLightLevel(x, y + 1, z) >= 13;
            return (byte)(topExposed && nearSurface && skylit ? 1 : 0);
        }

        private static int ResolveTileId(byte blockType)
        {
            return blockType == 1 ? 0 : 1;
        }

        private static void AppendVisibleFaces(ChunkMeshData meshData, WorldData worldData, int x, int y, int z, byte blockType, int tileId)
        {
            if (!worldData.IsSolidBlock(x, y - 1, z))
            {
                AppendFace(meshData, worldData, BottomFace, QuadUVs, BottomNormal, HorizontalTangent, x, y, z, blockType, 0, tileId, worldData.GetBrightness(x, y - 1, z) * 1.0f, 1.0f);
            }

            if (!worldData.IsSolidBlock(x, y + 1, z))
            {
                AppendFace(meshData, worldData, TopFace, QuadUVs, TopNormal, HorizontalTangent, x, y, z, blockType, 1, tileId, worldData.GetBrightness(x, y + 1, z) * 1.0f, 1.0f);
            }

            if (!worldData.IsSolidBlock(x, y, z - 1))
            {
                AppendFace(meshData, worldData, NorthFace, NorthFaceUVs, NorthNormal, HorizontalTangent, x, y, z, blockType, 2, tileId, worldData.GetBrightness(x, y, z - 1) * 0.85f, 0.85f);
            }

            if (!worldData.IsSolidBlock(x, y, z + 1))
            {
                AppendFace(meshData, worldData, SouthFace, SouthFaceUVs, SouthNormal, HorizontalTangent, x, y, z, blockType, 3, tileId, worldData.GetBrightness(x, y, z + 1) * 0.85f, 0.85f);
            }

            if (!worldData.IsSolidBlock(x - 1, y, z))
            {
                AppendFace(meshData, worldData, WestFace, WestFaceUVs, WestNormal, VerticalTangent, x, y, z, blockType, 4, tileId, worldData.GetBrightness(x - 1, y, z) * 0.75f, 0.75f);
            }

            if (!worldData.IsSolidBlock(x + 1, y, z))
            {
                AppendFace(meshData, worldData, EastFace, EastFaceUVs, EastNormal, VerticalTangent, x, y, z, blockType, 5, tileId, worldData.GetBrightness(x + 1, y, z) * 0.75f, 0.75f);
            }
        }

        private static void AppendFace(
            ChunkMeshData meshData,
            WorldData worldData,
            Vector3[] faceVertices,
            Vector2[] faceUvs,
            Vector3 normal,
            Vector4 tangent,
            int x,
            int y,
            int z,
            byte blockType,
            int faceId,
            int tileId,
            float brightness,
            float fullyLitBrightness)
        {
            var vertexStart = meshData.Vertices.Count;
            var vertexBrightness = new float[4];
            var vertexOcclusion = new float[4];
            var combinedShading = new float[4];
            var faceBrightness = 0f;

            for (var i = 0; i < 4; i++)
            {
                vertexBrightness[i] = SampleVertexBrightness(worldData, x, y, z, faceId, faceVertices[i], brightness);
                vertexOcclusion[i] = SampleVertexAmbientOcclusion(worldData, x, y, z, faceId, faceVertices[i]);
                combinedShading[i] = vertexBrightness[i] * vertexOcclusion[i];
                faceBrightness += vertexBrightness[i];
            }

            faceBrightness *= 0.25f;
            var metadata = new Vector4(blockType, faceId, faceBrightness, tileId);

            for (var i = 0; i < 4; i++)
            {
                meshData.Vertices.Add(new Vector3(x, y, z) + faceVertices[i]);
                meshData.UVs.Add(faceUvs[i]);
                meshData.Metadata.Add(metadata);
                meshData.Colors.Add((Color32)new Color(vertexBrightness[i], vertexBrightness[i], vertexBrightness[i], vertexOcclusion[i]));
                meshData.Normals.Add(normal);
                meshData.Tangents.Add(tangent);
            }

            if (combinedShading[0] + combinedShading[2] > combinedShading[1] + combinedShading[3])
            {
                AddTriangle(meshData, vertexStart + 0, vertexStart + 1, vertexStart + 3);
                AddTriangle(meshData, vertexStart + 1, vertexStart + 2, vertexStart + 3);
            }
            else
            {
                AddTriangle(meshData, vertexStart + 0, vertexStart + 1, vertexStart + 2);
                AddTriangle(meshData, vertexStart + 0, vertexStart + 2, vertexStart + 3);
            }
        }

        private static float SampleVertexBrightness(WorldData worldData, int x, int y, int z, int faceId, Vector3 vertex, float fallbackBrightness)
        {
            if (worldData == null)
            {
                return fallbackBrightness;
            }

            var sampleSum = 0f;
            var sampleCount = 0;

            switch (faceId)
            {
                case 0:
                case 1:
                {
                    var sampleY = faceId == 1 ? y + 1 : y - 1;
                    var edgeX = x + (vertex.x > 0.5f ? 1 : 0);
                    var edgeZ = z + (vertex.z > 0.5f ? 1 : 0);
                    AddBrightnessSample(worldData, edgeX, sampleY, edgeZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX - 1, sampleY, edgeZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX, sampleY, edgeZ - 1, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX - 1, sampleY, edgeZ - 1, ref sampleSum, ref sampleCount);
                    break;
                }
                case 2:
                case 3:
                {
                    var sampleZ = faceId == 3 ? z + 1 : z - 1;
                    var edgeX = x + (vertex.x > 0.5f ? 1 : 0);
                    var edgeY = y + (vertex.y > 0.5f ? 1 : 0);
                    AddBrightnessSample(worldData, edgeX, edgeY, sampleZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX - 1, edgeY, sampleZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX, edgeY - 1, sampleZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, edgeX - 1, edgeY - 1, sampleZ, ref sampleSum, ref sampleCount);
                    break;
                }
                case 4:
                case 5:
                {
                    var sampleX = faceId == 5 ? x + 1 : x - 1;
                    var edgeY = y + (vertex.y > 0.5f ? 1 : 0);
                    var edgeZ = z + (vertex.z > 0.5f ? 1 : 0);
                    AddBrightnessSample(worldData, sampleX, edgeY, edgeZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, sampleX, edgeY - 1, edgeZ, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, sampleX, edgeY, edgeZ - 1, ref sampleSum, ref sampleCount);
                    AddBrightnessSample(worldData, sampleX, edgeY - 1, edgeZ - 1, ref sampleSum, ref sampleCount);
                    break;
                }
            }

            if (sampleCount == 0)
            {
                return fallbackBrightness;
            }

            return Mathf.Lerp(fallbackBrightness, sampleSum / sampleCount, 0.8f);
        }

        private static void AddBrightnessSample(WorldData worldData, int x, int y, int z, ref float sampleSum, ref int sampleCount)
        {
            sampleSum += worldData.GetBrightness(x, y, z);
            sampleCount++;
        }

        private static float SampleVertexAmbientOcclusion(WorldData worldData, int x, int y, int z, int faceId, Vector3 vertex)
        {
            var side1 = false;
            var side2 = false;
            var corner = false;

            switch (faceId)
            {
                case 0:
                case 1:
                {
                    var dx = vertex.x > 0.5f ? 1 : -1;
                    var dz = vertex.z > 0.5f ? 1 : -1;
                    side1 = worldData.IsSolidBlock(x + dx, y, z);
                    side2 = worldData.IsSolidBlock(x, y, z + dz);
                    corner = worldData.IsSolidBlock(x + dx, y, z + dz);
                    break;
                }
                case 2:
                case 3:
                {
                    var dx = vertex.x > 0.5f ? 1 : -1;
                    var dy = vertex.y > 0.5f ? 1 : -1;
                    side1 = worldData.IsSolidBlock(x + dx, y, z);
                    side2 = worldData.IsSolidBlock(x, y + dy, z);
                    corner = worldData.IsSolidBlock(x + dx, y + dy, z);
                    break;
                }
                case 4:
                case 5:
                {
                    var dz = vertex.z > 0.5f ? 1 : -1;
                    var dy = vertex.y > 0.5f ? 1 : -1;
                    side1 = worldData.IsSolidBlock(x, y, z + dz);
                    side2 = worldData.IsSolidBlock(x, y + dy, z);
                    corner = worldData.IsSolidBlock(x, y + dy, z + dz);
                    break;
                }
            }

            var occlusionLevel = side1 && side2 ? 0 : 3 - ((side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0));
            return occlusionLevel switch
            {
                0 => 0.78f,
                1 => 0.86f,
                2 => 0.93f,
                _ => 1f
            };
        }

        private static void AddTriangle(ChunkMeshData meshData, int a, int b, int c)
        {
            meshData.AllTriangles.Add(a);
            meshData.AllTriangles.Add(b);
            meshData.AllTriangles.Add(c);
            meshData.BrightTriangles.Add(a);
            meshData.BrightTriangles.Add(b);
            meshData.BrightTriangles.Add(c);
        }
    }
}
