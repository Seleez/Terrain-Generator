using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public ComputeShader MarchingShader;

    public MeshFilter MeshFilter;
    public MeshCollider MeshCollider;

    ComputeBuffer _trianglesBuffer;
    ComputeBuffer _trianglesCountBuffer;
    ComputeBuffer _weightsBuffer;

    public NoiseGenerator NoiseGenerator;

    [Range(0, 4)]
    public int LOD;

    struct Triangle {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public static int SizeOf => sizeof(float) * 3 * 3;
    }

    float[] _weights;

    private void Start() {
        Create();
    }

    private void OnValidate() {
        if (Application.isPlaying) {
            Create();
        }
    }

    void Create() {
        CreateBuffers();
        if (_weights == null) {
            _weights = NoiseGenerator.GetNoise(GridMetrics.LastLod);
        }

        UpdateMesh();
        ReleaseBuffers();
    }

    void UpdateMesh() {
        Mesh mesh = ConstructMesh();
        MeshFilter.sharedMesh = mesh;
        MeshCollider.sharedMesh = mesh;
    }

    public void EditWeights(Vector3 hitPosition, float brushSize, bool add) {
        CreateBuffers();
        int kernel = MarchingShader.FindKernel("UpdateWeights");

        _weightsBuffer.SetData(_weights);
        MarchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(GridMetrics.LastLod));
        MarchingShader.SetVector("_HitPosition", hitPosition);
        MarchingShader.SetFloat("_BrushSize", brushSize);
        MarchingShader.SetFloat("_TerraformStrength", add ? 1f : -1f);
        MarchingShader.SetInt("_Scale", GridMetrics.Scale);

        MarchingShader.Dispatch(kernel, GridMetrics.ThreadGroups(GridMetrics.LastLod), GridMetrics.ThreadGroups(GridMetrics.LastLod), GridMetrics.ThreadGroups(GridMetrics.LastLod));

        _weightsBuffer.GetData(_weights);

        UpdateMesh();
        ReleaseBuffers();
    }

    Mesh ConstructMesh() {
        int kernel = MarchingShader.FindKernel("March");

        MarchingShader.SetBuffer(kernel, "_Triangles", _trianglesBuffer);
        MarchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);

        float lodScaleFactor = GridMetrics.PointsPerChunk(GridMetrics.LastLod) / (float)GridMetrics.PointsPerChunk(LOD);

        MarchingShader.SetFloat("_LodScaleFactor", lodScaleFactor);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk(GridMetrics.LastLod));
        MarchingShader.SetInt("_LODSize", GridMetrics.PointsPerChunk(LOD));
        MarchingShader.SetFloat("_IsoLevel", .5f);
        MarchingShader.SetInt("_Scale", GridMetrics.Scale);

        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);


        MarchingShader.Dispatch(kernel, GridMetrics.ThreadGroups(LOD), GridMetrics.ThreadGroups(LOD), GridMetrics.ThreadGroups(LOD));

        Triangle[] triangles = new Triangle[ReadTriangleCount()];
        _trianglesBuffer.GetData(triangles);

        return CreateMeshFromTriangles(triangles);
    }

    int ReadTriangleCount() {
        int[] triCount = { 0 };
        ComputeBuffer.CopyCount(_trianglesBuffer, _trianglesCountBuffer, 0);
        _trianglesCountBuffer.GetData(triCount);
        return triCount[0];
    }

    Mesh CreateMeshFromTriangles(Triangle[] triangles) {
        Vector3[] verts = new Vector3[triangles.Length * 3];
        int[] tris = new int[triangles.Length * 3];

        for (int i = 0; i < triangles.Length; i++) {
            int startIndex = i * 3;

            verts[startIndex] = triangles[i].a;
            verts[startIndex + 1] = triangles[i].b;
            verts[startIndex + 2] = triangles[i].c;

            tris[startIndex] = startIndex;
            tris[startIndex + 1] = startIndex + 1;
            tris[startIndex + 2] = startIndex + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    void CreateBuffers() {
        _trianglesBuffer = new ComputeBuffer(5 * (GridMetrics.PointsPerChunk(LOD) * GridMetrics.PointsPerChunk(LOD) * GridMetrics.PointsPerChunk(LOD)), Triangle.SizeOf, ComputeBufferType.Append);
        _trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        _weightsBuffer = new ComputeBuffer(GridMetrics.PointsPerChunk(GridMetrics.LastLod) * GridMetrics.PointsPerChunk(GridMetrics.LastLod) * GridMetrics.PointsPerChunk(GridMetrics.LastLod), sizeof(float));
    }

    void ReleaseBuffers() {
        _trianglesBuffer.Release();
        _trianglesCountBuffer.Release();
        _weightsBuffer.Release();
    }
}
