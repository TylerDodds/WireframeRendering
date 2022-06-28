// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using PixelinearAccelerator.WireframeRendering.Runtime.Mesh;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using PixelinearAccelerator.WireframeRendering.Runtime.Layer;
using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// A <see cref="ScriptedImporter"/> for generating wireframe meshes with line topology from a source model.
    /// </summary>
    [ScriptedImporter(1, FileExtensionWithoutDot)]
    public class WireframeMeshScriptedImporter : ScriptedImporter
    {
        /// <summary>
        /// Distance used to weld close-by wireframe vertices together.
        /// </summary>
        [SerializeField]
        private float _weldDistance = -1;//NB This starts off negative, so that new values will have the appropriate default setting value applied in OnValidate.

        /// <summary>
        /// This method is called by the Asset pipeline to import files.
        /// </summary>
        /// <param name="ctx">The <see cref="AssetImportContext"/>.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string json = File.ReadAllText(ctx.assetPath);
            WireframeGeneratedMeshData generatedMesh = JsonUtility.FromJson<WireframeGeneratedMeshData>(json);
            WireframeRenderingSettings wireframeSettings = WireframeRenderingSettings.Settings;
            ImportFromReferenceAssetGuid(ctx, generatedMesh.ReferenceGuid, wireframeSettings.DefaultLayer, wireframeSettings.ImportObjectNormals, wireframeSettings.ImportContourEdges);
        }

        /// <summary>
        /// Generates wireframe meshes and prefab based on source model on import.
        /// </summary>
        /// <param name="ctx">The <see cref="AssetImportContext"/>.</param>
        /// <param name="guidString">The source model GUID as a string.</param>
        /// <param name="wireframeLayer">The <see cref="SingleLayer"/> used for the wireframe siblings.</param>
        /// <param name="importObjectNormals">If object-space normals should be imported.</param>
        /// <param name="importContourEdges">If contour edge information should be imported</param>
        private void ImportFromReferenceAssetGuid(AssetImportContext ctx, string guidString, SingleLayer wireframeLayer, bool importObjectNormals, bool importContourEdges)
        {
            string originalModelPath = AssetDatabase.GUIDToAssetPath(guidString);

            Object originalMainAsset = AssetDatabase.LoadMainAssetAtPath(originalModelPath);
            GameObject originalMainGO = originalMainAsset as GameObject;
            GameObject newGameObject;
            if (originalMainGO != null)
            {
                newGameObject = Instantiate(originalMainGO);

                MeshFilter[] originalMeshFilters = newGameObject.GetComponentsInChildren<MeshFilter>();
                foreach(MeshFilter originalMeshFilter in originalMeshFilters)
                {
                    Mesh newMesh = GetLineSegmentMeshFromOriginal(originalMeshFilter.sharedMesh, _weldDistance, importObjectNormals, importContourEdges);
                    ctx.AddObjectToAsset(newMesh.name, newMesh);

                    GameObject newMeshFilterGO = Instantiate(originalMeshFilter.gameObject, originalMeshFilter.transform.parent);
                    newMeshFilterGO.name = $"{originalMeshFilter.name}_Wire";
                    newMeshFilterGO.GetComponent<MeshFilter>().sharedMesh = newMesh;
                    newMeshFilterGO.layer = wireframeLayer;
                    MeshRenderer newMeshRenderer = newMeshFilterGO.GetComponent<MeshRenderer>();
                    if (newMeshRenderer != null)
                    {
                        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.pixelinearaccelerator.wireframe-rendering/Runtime/Materials/Placeholder Wireframe Material (Geometry Shader).mat");
                        if(existingMaterial != null)
                        {
                            newMeshRenderer.sharedMaterial = existingMaterial;
                        }
                        else
                        {
                            Material newMaterial = new Material(Shader.Find("Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (Using Geometry Shader)"));
                            newMaterial.name = "Placeholder Wireframe Material";
                            ctx.AddObjectToAsset(newMaterial.name, newMaterial);
                            newMeshRenderer.sharedMaterial = newMaterial;
                        }
                    }
                }

                ctx.AddObjectToAsset("GameObject", newGameObject);
                ctx.SetMainObject(newGameObject);
            }
            else
            {
                Debug.LogError($"Root GameObject of wireframe source model at {originalModelPath} not found while importing generated wireframe mesh at {ctx.assetPath}.");
            }

            ctx.DependsOnArtifact(originalModelPath);

            WireframeGeneratedMeshInfo wireframeGeneratedMeshHolder = ScriptableObject.CreateInstance<WireframeGeneratedMeshInfo>();
            wireframeGeneratedMeshHolder.name = "Wireframe Generated Mesh Holder";
            wireframeGeneratedMeshHolder.ReferenceGuid = guidString;
            ctx.AddObjectToAsset(wireframeGeneratedMeshHolder.name, wireframeGeneratedMeshHolder);

            SaveAndReimport();
        }

        /// <summary>
        /// Gets a wireframe edge mesh with <see cref="MeshTopology.Lines"/> from the <paramref name="originalMesh"/>.
        /// </summary>
        /// <param name="originalMesh">The original mesh.</param>
        /// <param name="epsilon">The distance at which positions are considered physically identical.</param>
        /// <param name="importObjectNormals">If object normals should be imported.</param>
        /// <param name="importContourEdges">If contour edge information should be imported</param>
        /// <returns>A wireframe edge mesh with <see cref="MeshTopology.Lines"/>.</returns>
        private static Mesh GetLineSegmentMeshFromOriginal(Mesh originalMesh, float epsilon, bool importObjectNormals, bool importContourEdges)
        {
            Mesh newMesh = new Mesh();
            newMesh.name = $"{originalMesh.name}_Wire";
            List<Vector3> originalVertices = new List<Vector3>();
            originalMesh.GetVertices(originalVertices);

            //Get the boundary edges, and the corresponding vertices
            MeshInformation originalMeshInformation = MeshInformation.CreateAndUpdateFromSubmeshTriangleTopology(originalMesh);
            List<DecoupledGrouping> originalMeshGroupings = TriangleGroupUtilities.GetDecoupledGroupings(originalMeshInformation, DecoupledGroupType.SharedEdges);
            List<Edge> boundaryEdges = originalMeshGroupings.SelectMany(g => g.Edges).Where(e => e.TriangleCount == 1).ToList();

            //NB Note that the implementations below do not do full, proper clustering of vertex positions, but instead use the first vertex as the anchor position for all vertices within epsilon
            bool shareVertices = !(importObjectNormals || importContourEdges);
            if (shareVertices)
            {
                Dictionary<int, int> originalVertexToFinalIndicesMapping = GetMappingOfOriginalToPhysicallyIdenticalVertexIndices(originalVertices, epsilon);

                //Combine physically-identical vertices
                List<Vector3> remappedPositions = originalVertexToFinalIndicesMapping
                    .GroupBy(pair => pair.Value)
                    .OrderBy(g => g.Key)
                    .Select(group => group.Select(originalFinalPair => originalVertices[originalFinalPair.Key]))
                    .Select(positions => GeometryUtils.GetAverageVector3(positions))
                    .ToList();

                List<Vector2Int> remappedSegments = boundaryEdges.Select(edge => new Vector2Int(originalVertexToFinalIndicesMapping[edge.FirstIndex], originalVertexToFinalIndicesMapping[edge.SecondIndex])).ToList();
                List<Vector2Int> remappedSegmentsDistinct = remappedSegments.Select(indices => new Vector2Int(Mathf.Min(indices.x, indices.y), Mathf.Max(indices.x, indices.y))).Distinct().ToList();
                List<int> remappedIndices = GetLineSegmentIndices(remappedSegmentsDistinct);

                List<int> remappedIndicesDistinct = remappedIndices.Distinct().ToList(); 
                Dictionary<int, int> finalVertexIndicesRemapped = remappedIndicesDistinct.Select((value, index) => new { value, index }).ToDictionary(p => p.value, p => p.index);

                //In this case, segments don't need to be compared for closeness, because we will not write per-segment information.
                List<Vector3> finalVertices = remappedIndicesDistinct.Select(index => remappedPositions[index]).ToList();
                List<int> finalIndices = remappedIndices.Select(index => finalVertexIndicesRemapped[index]).ToList();
                newMesh.SetVertices(finalVertices);
                newMesh.SetIndices(finalIndices, MeshTopology.Lines, 0);
            }
            else
            {
                List<Edge> edgesToUse = importContourEdges ? originalMeshGroupings.SelectMany(g => g.Edges).ToList() : boundaryEdges;
                List<SegmentAndNormals> physicallyDistinctEdges = GetPhysicallyDistinctSegments(originalVertices, edgesToUse, epsilon);
                List<Vector3> vertexPositions = physicallyDistinctEdges.SelectMany(e => new Vector3[] { e.Start, e.End }).ToList();
                List<Vector3> vertexNormals = physicallyDistinctEdges.SelectMany(e => new Vector3[] { e.Normal, e.Normal}).ToList();
                List<Vector3> faceNormals = physicallyDistinctEdges.SelectMany(e => new Vector3[] { e.FaceNormal1, e.FaceNormal2}).ToList();
                List<int> segmentIndices = Enumerable.Range(0, vertexPositions.Count).ToList();

                newMesh.SetVertices(vertexPositions);
                if (importObjectNormals)
                {
                    newMesh.SetNormals(vertexNormals);
                }
                if (importContourEdges)
                {
                    newMesh.SetUVs(0, faceNormals);
                }
                newMesh.SetIndices(segmentIndices, MeshTopology.Lines, 0);
            }
            return newMesh;
        }

        /// <summary>
        /// Gets a vertex index mapping by mapping originally-physically-identical vertices to the same vertex.
        /// </summary>
        /// <param name="originalVertices">The original vertex positions.</param>
        /// <param name="epsilon">The distance at which positions are considered physically identical.</param>
        private static Dictionary<int, int> GetMappingOfOriginalToPhysicallyIdenticalVertexIndices(List<Vector3> originalVertices, float epsilon)
        {
            int count = originalVertices.Count;
            List<HashSet<int>> physicallyCloseIndices = GetPhysicallyCloseIndices(originalVertices, epsilon);
            List<int> mappedToClose = physicallyCloseIndices.Select(indices => indices.Min()).ToList();

            List<int> distinctIndices = mappedToClose.Distinct().ToList();
            Dictionary<int, int> distinctIndicesOldNewMapping = distinctIndices.Select((vertexIndex, distinctIndex) => (vertexIndex, distinctIndex)).ToDictionary(pair => pair.vertexIndex, pair => pair.distinctIndex);
            Dictionary<int, int> originalVertexToFinalIndicesMapping = Enumerable.Range(0, count).ToDictionary(index => index, index => distinctIndicesOldNewMapping[mappedToClose[index]]);
            return originalVertexToFinalIndicesMapping;
        }

        /// <summary>
        /// Returns segments that are physically distinct, along with additional normal information obtained from the combination of originally-physically-similar edges.
        /// </summary>
        /// <param name="originalPositions">The original vertex positions.</param>
        /// <param name="originalEdges">The original segment edges.</param>
        /// <param name="epsilon">The distance at which positions are considered physically identical.</param>
        private static List<SegmentAndNormals> GetPhysicallyDistinctSegments(List<Vector3> originalPositions, List<Edge> originalEdges, float epsilon)
        {
            List<HashSet<int>> physicallyCloseIndices = GetPhysicallyCloseIndices(originalPositions, epsilon);

            Dictionary<Vector2Int, HashSet<EdgeAndDirection>> segmentsPhysicallyMapped = GetPhysicallySameSegmentsMap(originalEdges, physicallyCloseIndices);

            List<SegmentAndNormals> finalLineEdgesNormals = segmentsPhysicallyMapped.Values.Select(edges =>
            {
                IEnumerable<Vector3> allFaceNormals = edges.SelectMany(ed => ed.Edge.Select(triangle => GeometryUtils.GetTriangleNormal(triangle, originalPositions)));
                const float faceNormalEpsilon = 1e-8f;
                List<Vector3> clusteredFaceNormals = ClusterVectorsApproximately(allFaceNormals, faceNormalEpsilon, true);
                
                Vector3 firstFaceNormal, secondFaceNormal;
                bool anyEdgeIsABoundaryEdge = edges.Any(e => e.Edge.TriangleCount == 1);
                bool include;
                if(anyEdgeIsABoundaryEdge)
                {
                    include = true;
                    firstFaceNormal = secondFaceNormal = Vector3.zero;
                }
                else if(clusteredFaceNormals.Count >= 2)
                {
                    include = true;
                    firstFaceNormal = clusteredFaceNormals[0];
                    secondFaceNormal = clusteredFaceNormals[1];
                }
                else
                {
                    include = false;//NB Not a boundary or potential contour edge, so will not be imported.
                    firstFaceNormal = secondFaceNormal = Vector3.right * 0.5f;//NB Even though these segments will not be included, this choice will ensure both face normals face the same direction so that they won't trigger the contour condition
                }

                IEnumerable<Vector3> edgeAverageNormals = edges.Select(ed => GeometryUtils.GetAverageNormalSimple(ed.Edge.Select(triangle => GeometryUtils.GetTriangleNormal(triangle, originalPositions))));
                Vector3 averageNormalsOverAllEdges = GeometryUtils.GetAverageNormalSimple(edgeAverageNormals);
                
                Vector3 start = GeometryUtils.GetAverageVector3(edges.Select(e => e.Forward ? originalPositions[e.Edge.FirstIndex] : originalPositions[e.Edge.SecondIndex]));
                Vector3 end = GeometryUtils.GetAverageVector3(edges.Select(e => !e.Forward ? originalPositions[e.Edge.FirstIndex] : originalPositions[e.Edge.SecondIndex]));
                SegmentAndNormals segmentAndNormals = new SegmentAndNormals(start, end, averageNormalsOverAllEdges, firstFaceNormal, secondFaceNormal);
                return new { segmentAndNormals, include};
            })
            .Where(p => p.include)
            .Select(p => p.segmentAndNormals)
            .ToList();
            return finalLineEdgesNormals;
        }

        /// <summary>
        /// Gets a mapping from a representative segment's indices to the sets of <see cref="EdgeAndDirection"/> that are physically close to that.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <param name="physicallyCloseIndices">The set of vertex indices that are physically close to each other.</param>
        private static Dictionary<Vector2Int, HashSet<EdgeAndDirection>> GetPhysicallySameSegmentsMap(List<Edge> edges, List<HashSet<int>> physicallyCloseIndices)
        {
            Dictionary<Vector2Int, HashSet<EdgeAndDirection>> segmentsPhysicallyMapped = new Dictionary<Vector2Int, HashSet<EdgeAndDirection>>();
            foreach (Edge originalEdge in edges)
            {
                IEnumerable<Vector2Int> possiblePhysicallyCloseSegments = physicallyCloseIndices[originalEdge.FirstIndex]
                    .SelectMany(ix => physicallyCloseIndices[originalEdge.SecondIndex].Select(iy => new Vector2Int(ix, iy)));
                bool foundMatch = false;
                foreach (Vector2Int possibleSegment in possiblePhysicallyCloseSegments)
                {
                    if (segmentsPhysicallyMapped.ContainsKey(possibleSegment))
                    {
                        segmentsPhysicallyMapped[possibleSegment].Add(new EdgeAndDirection(originalEdge, false));
                        foundMatch = true;
                        break;
                    }
                    else
                    {
                        Vector2Int reversedSegment = new Vector2Int(possibleSegment.y, possibleSegment.x);
                        if (segmentsPhysicallyMapped.ContainsKey(reversedSegment))
                        {
                            segmentsPhysicallyMapped[reversedSegment].Add(new EdgeAndDirection(originalEdge, true));
                            foundMatch = true;
                            break;
                        }
                    }
                }

                if (!foundMatch)
                {
                    HashSet<EdgeAndDirection> newHashSet = new HashSet<EdgeAndDirection>();
                    newHashSet.Add(new EdgeAndDirection(originalEdge, false));
                    segmentsPhysicallyMapped[GetEdgeIndices(originalEdge)] = newHashSet;
                }
            }

            return segmentsPhysicallyMapped;
        }

        /// <summary>
        /// For each position, gets the set of indices in the original list that are within <paramref name="epsilon"/> of itself.
        /// </summary>
        /// <param name="positions">The positions.</param>
        /// <param name="epsilon">The distance threshold.</param>
        private static List<HashSet<int>> GetPhysicallyCloseIndices(List<Vector3> positions, float epsilon)
        {
            int count = positions.Count;

            float[,] squareDistances = new float[count, count];
            for (int i = 0; i < count; i++)
            {
                Vector3 pos1 = positions[i];
                for (int j = i + 1; j < count; j++)
                {
                    Vector3 difference = pos1 - positions[j];
                    float sqrMag = difference.sqrMagnitude;
                    squareDistances[i, j] = sqrMag;
                    squareDistances[j, i] = sqrMag;
                }
            }

            float epsSqr = epsilon * epsilon;
            //Get sets of physically close indices (including themselves)
            List<HashSet<int>> physicallyCloseIndices = new List<HashSet<int>>();
            for (int i = 0; i < count; i++)
            {
                HashSet<int> closeIndices = new HashSet<int>();
                for (int j = 0; j < count; j++)
                {
                    if (squareDistances[i, j] < epsSqr)
                    {
                        closeIndices.Add(j);
                    }
                }
                physicallyCloseIndices.Add(closeIndices);
            }

            return physicallyCloseIndices;
        }

        /// <summary>
        /// Clusters a set of <see cref="Vector3"/> approximately, based on adding consecutively to clusters based on closeness to average value.
        /// Ordered by count in cluster, descebding.
        /// </summary>
        /// <param name="vectors">The set of input vectors.</param>
        /// <param name="epsilon">Cutoff distance for clustering.</param>
        /// <param name="directionVectors">If vectors are direction vectors.</param>
        private static List<Vector3> ClusterVectorsApproximately(IEnumerable<Vector3> vectors, float epsilon, bool directionVectors)
        {
            List<Cluster> clusters = new List<Cluster>();
            foreach(Vector3 vector in vectors)
            {
                bool added = false;
                foreach(Cluster cluster in clusters)
                {
                    if(cluster.TryAdd(vector))
                    {
                        added = true;
                        break;
                    }
                }
                if(!added)
                {
                    clusters.Add(new Cluster(vector, epsilon, directionVectors));
                }
            }

            List<Vector3> clusterAverages = clusters.OrderByDescending(cluster => cluster.Vectors.Count).Select(cluster => cluster.Average).ToList();
            return clusterAverages;
        }

        /// <summary>
        /// Represents a cluster of <see cref="Vector3"/>.
        /// </summary>
        class Cluster
        {
            public IReadOnlyList<Vector3> Vectors => _vectors;
            public Vector3 Average => _average;

            public Cluster(Vector3 initial, float epsilon, bool directionVectors)
            {
                _vectors.Add(initial);
                _average = initial;
                _epsilon = epsilon;
                _directionVectors = directionVectors;
            }

            /// <summary>
            /// Try to add a vector to the cluster.
            /// </summary>
            /// <param name="other">The vector</param>
            /// <returns>If the vector was added</returns>
            public bool TryAdd(Vector3 other)
            {
                Vector3 diff = other - _average;
                bool isClose = diff.magnitude < _epsilon;
                if(isClose)
                {
                    _vectors.Add(other);
                    if(_directionVectors)
                    {
                        _average = GeometryUtils.GetAverageNormalSimple(_vectors);
                    }
                    else
                    {
                        _average = GeometryUtils.GetAverageVector3(_vectors);
                    }
                }
                return isClose;
            }

            private float _epsilon;
            private bool _directionVectors;
            private List<Vector3> _vectors = new List<Vector3>();
            private Vector3 _average;
        }

        /// <summary>
        /// Gets a list of indices from line segment indices, suitable for <see cref="MeshTopology.Lines"/>.
        /// </summary>
        /// <param name="segmentIndices">The pairs of line segment indices.</param>
        private static List<int> GetLineSegmentIndices(IEnumerable<Vector2Int> segmentIndices)
        {
            List<int> allIndices = new List<int>();
            foreach (Vector2Int segment in segmentIndices)
            {
                allIndices.Add(segment.x);
                allIndices.Add(segment.y);
            }

            return allIndices;
        }

        private static Vector2Int GetEdgeIndices(Edge edge) => new Vector2Int(edge.FirstIndex, edge.SecondIndex);

        /// <summary>
        /// An edge and a direction (if FirstIndex should be considered the start of the segment).
        /// </summary>
        private struct EdgeAndDirection
        {
            public Edge Edge;
            public bool Forward;

            public EdgeAndDirection(Edge edge, bool forward)
            {
                Edge = edge;
                Forward = forward;
            }
        }

        /// <summary>
        /// A line segment, a normal vector for the segment between them, and the segment's connecting face normals.
        /// </summary>
        private struct SegmentAndNormals
        {
            public Vector3 Start;
            public Vector3 End;
            public Vector3 Normal;
            public Vector3 FaceNormal1;
            public Vector3 FaceNormal2;

            public SegmentAndNormals(Vector3 start, Vector3 end, Vector3 normal, Vector3 faceNormal1, Vector3 faceNormal2)
            {
                Start = start;
                End = end;
                Normal = normal;
                FaceNormal1 = faceNormal1;
                FaceNormal2 = faceNormal2;
            }
        }

        private void OnValidate()
        {
            if(_weldDistance < 0)
            {
                _weldDistance = WireframeRenderingSettings.Settings.ImporterDefaultWeldDistance;
            }
        }

        /// <summary>
        /// The file extension used by this ScriptedImporter.
        /// </summary>
        public const string FileExtension = "." + FileExtensionWithoutDot;
        private const string FileExtensionWithoutDot = "wiremesh";
    }
}
