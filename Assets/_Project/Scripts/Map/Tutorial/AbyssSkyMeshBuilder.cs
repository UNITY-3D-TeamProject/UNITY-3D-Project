using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map.Tutorial
{
    /// <summary>
    /// 시스템 심연 3D 하늘의 메시를 만든다. 고리 한 겹(SAbyssRing)마다 모듈 블록 / 빛 테두리 / 빛 기둥 3개.
    /// 같은 System.Random 시드면 같은 모양이 나온다. 정점 데이터 규칙은 Project/AbyssModule·AbyssLight 셰이더 머리말 참고.
    /// </summary>
    public static class AbyssSkyMeshBuilder
    {
        #region Constants
        private const int LINE_SEGMENTS = 360;
        private const float EDGE_LINE_WIDTH = 0.9f;
        private const float TWO_PI = Mathf.PI * 2.0f;
        #endregion

        #region Public Methods
        /// <summary>
        /// 고리 띠를 따라 어두운 모듈 블록을 놓는다. 칸마다 반지름 방향으로 1~3조각, 가끔 빠지고 가끔 위에 하나 더 얹힌다.
        /// </summary>
        public static Mesh BuildModules(SAbyssRing ring, System.Random random)
        {
            var data = new MeshData();
            int count = Mathf.Max(12, Mathf.RoundToInt((TWO_PI * ring.Radius) / ring.BlockLength));
            float step = TWO_PI / count;
            float inner = ring.Radius - (ring.Width * 0.5f);

            for (int i = 0; i < count; i++)
            {
                if (Next(random) < ring.GapChance)
                {
                    continue;
                }

                float cursor = 0.0f;
                int pieces = 1 + random.Next(3);

                for (int k = 0; k < pieces; k++)
                {
                    float depth = Mathf.Min(ring.Width * Mathf.Lerp(0.3f, 0.7f, Next(random)), ring.Width - cursor);
                    if (depth < 1.5f)
                    {
                        break;
                    }

                    float arcFill = Mathf.Lerp(0.8f, 0.98f, Next(random));
                    float angle = (i + 0.5f + ((Next(random) - 0.5f) * (1.0f - arcFill))) * step;
                    float midRadius = inner + cursor + (depth * 0.5f);
                    float thickness = Mathf.Lerp(ring.Thickness.x, ring.Thickness.y, Next(random));
                    float height = (Next(random) - 0.5f) * ring.Thickness.y * 0.8f;
                    var radial = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                    var tangent = new Vector3(-radial.z, 0.0f, radial.x);
                    Vector3 center = (radial * midRadius) + (Vector3.up * height);
                    Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
                    var size = new Vector3(depth, thickness, midRadius * step * arcFill);
                    var color = new Color(Next(random), 0.0f, angle / TWO_PI, 1.0f);

                    data.AddBox(center, rotation, size, color);

                    if (Next(random) < 0.2f)
                    {
                        float topThickness = thickness * Mathf.Lerp(0.4f, 0.9f, Next(random));
                        Vector3 topCenter = center + (Vector3.up * ((thickness + topThickness) * 0.5f));
                        data.AddBox(topCenter, rotation, new Vector3(depth * 0.6f, topThickness, size.z * 0.5f), color);
                    }

                    cursor += depth + Mathf.Lerp(0.4f, 3.0f, Next(random));
                }
            }

            return data.ToMesh("AbyssModules", true);
        }

        /// <summary>
        /// 고리 안쪽·바깥쪽 가장자리에 빛 테두리를 두른다. 어느 각도에서도 보이게 가로·세로 띠를 겹친다.
        /// </summary>
        public static Mesh BuildEdges(SAbyssRing ring, System.Random random)
        {
            var data = new MeshData();
            float half = ring.Width * 0.5f;

            AddCircleRibbon(data, ring.Radius - half, 1.0f, Next(random));
            AddCircleRibbon(data, ring.Radius + half, 0.7f, Next(random));
            return data.ToMesh("AbyssEdges", false);
        }

        /// <summary>
        /// 고리에서 아래로 늘어지는 빛 기둥 다발. 기둥마다 1~4가닥, 아래로 갈수록 사라진다.
        /// </summary>
        public static Mesh BuildPillars(SAbyssRing ring, System.Random random)
        {
            var data = new MeshData();

            for (int p = 0; p < ring.PillarCount; p++)
            {
                float angle = ((p + (Next(random) * 0.6f)) / ring.PillarCount) * TWO_PI;
                float radius = ring.Radius + ((Next(random) - 0.5f) * ring.Width * 0.8f);
                var radial = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                var tangent = new Vector3(-radial.z, 0.0f, radial.x);
                int strands = 1 + random.Next(4);
                float spacing = Mathf.Lerp(1.5f, 4.0f, Next(random));

                for (int s = 0; s < strands; s++)
                {
                    float offset = (s - ((strands - 1) * 0.5f)) * spacing;
                    Vector3 top = (radial * radius) + (tangent * offset) + (Vector3.up * Mathf.Lerp(-1.0f, 4.0f, Next(random)));
                    float length = ring.PillarLength * Mathf.Lerp(0.45f, 1.0f, Next(random));
                    float width = Mathf.Lerp(0.5f, 1.4f, Next(random));
                    float seed = Next(random);

                    AddPillarQuad(data, top, Vector3.right, length, width, seed);
                    AddPillarQuad(data, top, Vector3.forward, length, width, seed);
                }
            }

            return data.ToMesh("AbyssPillars", false);
        }
        #endregion

        #region Private Methods
        private static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static void AddCircleRibbon(MeshData data, float radius, float brightness, float seed)
        {
            float circumference = TWO_PI * radius;
            float half = EDGE_LINE_WIDTH * 0.5f;
            var color = new Color(seed, 0.0f, 0.0f, brightness);

            // 가로 띠 → 세로 띠 순서로 같은 원을 두 번 두른다
            for (int pass = 0; pass < 2; pass++)
            {
                int start = data.Vertices.Count;
                for (int s = 0; s <= LINE_SEGMENTS; s++)
                {
                    float t = s / (float)LINE_SEGMENTS;
                    float angle = t * TWO_PI;
                    var radial = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                    Vector3 across = (pass == 0) ? radial : Vector3.up;
                    Vector3 center = radial * radius;

                    data.AddVertex(center - (across * half), Vector3.up, new Vector2(0.0f, t * circumference), color);
                    data.AddVertex(center + (across * half), Vector3.up, new Vector2(1.0f, t * circumference), color);
                }

                for (int s = 0; s < LINE_SEGMENTS; s++)
                {
                    int a = start + (s * 2);
                    data.AddQuad(a, a + 1, a + 3, a + 2);
                }
            }
        }

        private static void AddPillarQuad(MeshData data, Vector3 top, Vector3 across, float length, float width, float seed)
        {
            // 위 → 35% 지점 → 끝, 밝기 1 → 0.5 → 0
            float[] rows = { 0.0f, 0.35f, 1.0f };
            float[] alphas = { 1.0f, 0.5f, 0.0f };
            Vector3 half = across * (width * 0.5f);
            int start = data.Vertices.Count;

            for (int r = 0; r < rows.Length; r++)
            {
                float distance = rows[r] * length;
                Vector3 center = top + (Vector3.down * distance);
                var color = new Color(seed, 0.0f, 0.0f, alphas[r]);
                data.AddVertex(center - half, Vector3.up, new Vector2(0.0f, distance), color);
                data.AddVertex(center + half, Vector3.up, new Vector2(1.0f, distance), color);
            }

            for (int r = 0; r < rows.Length - 1; r++)
            {
                int a = start + (r * 2);
                data.AddQuad(a, a + 1, a + 3, a + 2);
            }
        }
        #endregion

        #region Nested Types
        private class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<Vector2> Sizes = new List<Vector2>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<int> Triangles = new List<int>();

            public void AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Color color)
            {
                Vertices.Add(position);
                Normals.Add(normal);
                Uvs.Add(uv);
                Sizes.Add(Vector2.zero);
                Colors.Add(color);
            }

            public void AddQuad(int a, int b, int c, int d)
            {
                Triangles.Add(a);
                Triangles.Add(b);
                Triangles.Add(c);
                Triangles.Add(a);
                Triangles.Add(c);
                Triangles.Add(d);
            }

            /// <summary>면마다 uv = 면 안 좌표(m), uv1 = 면 크기(m).</summary>
            public void AddBox(Vector3 center, Quaternion rotation, Vector3 size, Color color)
            {
                AddFace(center, rotation, Vector3.right, Vector3.forward, Vector3.up, size.x, size.z, size.y, color);
                AddFace(center, rotation, Vector3.left, Vector3.forward, Vector3.up, size.x, size.z, size.y, color);
                AddFace(center, rotation, Vector3.up, Vector3.right, Vector3.forward, size.y, size.x, size.z, color);
                AddFace(center, rotation, Vector3.down, Vector3.right, Vector3.forward, size.y, size.x, size.z, color);
                AddFace(center, rotation, Vector3.forward, Vector3.right, Vector3.up, size.z, size.x, size.y, color);
                AddFace(center, rotation, Vector3.back, Vector3.right, Vector3.up, size.z, size.x, size.y, color);
            }

            public Mesh ToMesh(string meshName, bool hasSizes)
            {
                var mesh = new Mesh { name = meshName };
                mesh.indexFormat = (Vertices.Count > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(Vertices);
                mesh.SetNormals(Normals);
                mesh.SetUVs(0, Uvs);
                if (hasSizes)
                {
                    mesh.SetUVs(1, Sizes);
                }
                mesh.SetColors(Colors);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }

            private void AddFace(
                Vector3 center,
                Quaternion rotation,
                Vector3 normal,
                Vector3 axisU,
                Vector3 axisV,
                float depth,
                float sizeU,
                float sizeV,
                Color color)
            {
                Vector3 n = rotation * normal;
                Vector3 u = rotation * axisU;
                Vector3 v = rotation * axisV;
                Vector3 faceCenter = center + (n * (depth * 0.5f));
                Vector3 halfU = u * (sizeU * 0.5f);
                Vector3 halfV = v * (sizeV * 0.5f);
                var faceSize = new Vector2(sizeU, sizeV);
                int start = Vertices.Count;

                AddFaceVertex(faceCenter - halfU - halfV, n, new Vector2(0.0f, 0.0f), faceSize, color);
                AddFaceVertex(faceCenter + halfU - halfV, n, new Vector2(sizeU, 0.0f), faceSize, color);
                AddFaceVertex(faceCenter + halfU + halfV, n, new Vector2(sizeU, sizeV), faceSize, color);
                AddFaceVertex(faceCenter - halfU + halfV, n, new Vector2(0.0f, sizeV), faceSize, color);
                AddQuad(start, start + 1, start + 2, start + 3);
            }

            private void AddFaceVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector2 size, Color color)
            {
                Vertices.Add(position);
                Normals.Add(normal);
                Uvs.Add(uv);
                Sizes.Add(size);
                Colors.Add(color);
            }
        }
        #endregion
    }
}
