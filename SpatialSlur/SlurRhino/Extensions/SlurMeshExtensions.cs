﻿#if USING_RHINO

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using SpatialSlur.SlurCore;
using SpatialSlur.SlurMesh;
using Rhino.Geometry;

/*
 * Notes
 */

namespace SpatialSlur.SlurRhino
{
    /// <summary>
    /// Extension methods for classes in the SpatialSlur.SlurMesh namespace
    /// </summary>
    public static class SlurMeshExtensions
    {
        #region IHeElement

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="hedge"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Line ToLine<V, E>(this IHalfedge<V, E> hedge, Func<V, Vec3d> getPosition)
            where V : IHeVertex<V, E>
            where E : IHalfedge<V, E>
        {
            Vec3d p0 = getPosition(hedge.Start);
            Vec3d p1 = getPosition(hedge.End);
            return new Line(p0.X, p0.Y, p0.Z, p1.X, p1.Y, p1.Z);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="face"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Polyline ToPolyline<V, E, F>(this IHeFace<V, E, F> face, Func<V, Vec3d> getPosition)
            where V : IHeVertex<V, E, F>
            where E : IHalfedge<V, E, F>
            where F : IHeFace<V, E, F>
        {
            Polyline result = new Polyline();

            foreach (var v in face.Vertices)
            {
                var p = getPosition(v);
                result.Add(p.X, p.Y, p.Z);
            }

            result.Add(result.First);
            return result;
        }


        /// <summary>
        /// Returns the circumcircle of a triangular face.
        /// Assumes the face is triangular.
        /// http://mathworld.wolfram.com/Incenter.html
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="face"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Circle GetCircumcircle<V, E, F>(this IHeFace<V, E, F> face, Func<V, Vec3d> getPosition)
            where V : IHeVertex<V, E, F>
            where E : IHalfedge<V, E, F>
            where F : IHeFace<V, E, F>
        {
            var he = face.First;
            Point3d p0 = getPosition(he.PreviousInFace.Start);
            Point3d p1 = getPosition(he.PreviousInFace.Start);
            Point3d p2 = getPosition(he.NextInFace.Start);
            return new Circle(p0,p1,p2);
        }


        /*
        /// <summary>
        /// Returns the incircle of a triangular face.
        /// Assumes face is triangular.
        /// http://mathworld.wolfram.com/Incenter.html
        /// </summary>
        /// <returns></returns>
        public static Circle GetIncircle(this HeFace face)
        {
            // TODO
            Vec3d p0 = _first.Previous.Start.Position;
            Vec3d p1 = _first.Start.Position;
            Vec3d p2 = _first.Next.Start.Position;

            double d01 = p0.DistanceTo(p1);
            double d12 = p1.DistanceTo(p2);
            double d20 = p2.DistanceTo(p0);

            double p = (d01 + d12 + d20) * 0.5; // semiperimeter
            double pInv = 1.0 / p; // inverse semiperimeter
            radius = Math.Sqrt(p * (p - d01) * (p - d12) * (p - d20)) * pInv; // triangle area (Heron's formula) / semiperimeter

            pInv *= 0.5; // inverse perimeter
            return p0 * (d12 * pInv) + p1 * (d20 * pInv) + p2 * (d01 * pInv);
        }
        */

        #endregion


        #region HeElementList

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="structure"></param>
        /// <param name="getPosition"></param>
        /// <param name="setResult"></param>
        /// <param name="parallel"></param>
        public static void GetEdgeLines<V, E>(this HeStructure<V, E> structure, Func<V, Vec3d> getPosition, Action<E, Line> setResult, bool parallel = false)
            where V : HeElement<V, E>, IHeVertex<V, E>
            where E : Halfedge<V, E>, IHalfedge<V, E>
        {
            var edges = structure.Edges;

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, edges.Count), range => Body(range.Item1, range.Item2));
            else
                Body(0, edges.Count);

            void Body(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var he = edges[i];
                    if (he.IsUnused) continue;
                    setResult(he, he.ToLine(getPosition));
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="structure"></param>
        /// <param name="getPosition"></param>
        /// <param name="setResult"></param>
        /// <param name="parallel"></param>
        public static void GetFacePolylines<V, E, F>(this HeStructure<V, E, F> structure, Func<V, Vec3d> getPosition, Action<F, Polyline> setResult, bool parallel = false)
            where V : HeElement<V, E>, IHeVertex<V, E, F>
            where E : Halfedge<V, E, F>, IHalfedge<V, E, F>
            where F : HeElement<F, E>, IHeFace<V, E, F>
        {
            var faces = structure.Faces;

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, faces.Count), range => Body(range.Item1, range.Item2));
            else
                Body(0, faces.Count);

            void Body(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var f = faces[i];
                    if (f.IsUnused) continue;
                    setResult(f, f.ToPolyline(getPosition));
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="structure"></param>
        /// <param name="getPosition"></param>
        /// <param name="setResult"></param>
        /// <param name="parallel"></param>
        public static void GetFaceCircumcircles<V, E, F>(this HeStructure<V, E, F> structure, Func<V, Vec3d> getPosition, Action<F, Circle> setResult, bool parallel = false)
            where V : HeElement<V, E>, IHeVertex<V, E, F>
            where E : Halfedge<V, E, F>, IHalfedge<V, E, F>
            where F : HeElement<F, E>, IHeFace<V, E, F>
        {
            var faces = structure.Faces;

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, faces.Count), range => Body(range.Item1, range.Item2));
            else
                Body(0, faces.Count);

            void Body(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var f = faces[i];
                    if (f.IsUnused) continue;
                    setResult(f, f.GetCircumcircle(getPosition));
                }
            }
        }

        #endregion


        #region IHeStructure

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="graph"></param>
        /// <param name="xform"></param>
        /// <param name="parallel"></param>
        public static void Transform<V, E>(this HeStructure<V, E> graph, Transform xform, bool parallel = false)
            where V : HeElement<V, E>, IHeVertex<V, E>, IVertex3d
            where E : Halfedge<V, E>, IHalfedge<V, E>
        {
            var verts = graph.Vertices;

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), range => Body(range.Item1, range.Item2));
            else
                Body(0, verts.Count);

            void Body(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var v = verts[i];
                    v.Position = xform.ApplyToPoint(v.Position);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="graph"></param>
        /// <param name="xmorph"></param>
        /// <param name="parallel"></param>
        public static void SpaceMorph<V, E>(this HeStructure<V, E> graph, SpaceMorph xmorph, bool parallel = false)
            where V : HeElement<V, E>, IHeVertex<V, E>, IVertex3d
            where E : Halfedge<V, E>, IHalfedge<V, E>
        {
            var verts = graph.Vertices;

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), range => Body(range.Item1, range.Item2));
            else
                Body(0, verts.Count);

            void Body(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var v = verts[i];
                    v.Position = xmorph.Apply(v.Position);
                }
            }
        }

        #endregion


        #region HeMesh

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="quadrangulator"></param>
        /// <returns></returns>
        public static Mesh ToMesh<V, E, F>(this HeMeshBase<V, E, F> mesh, IFaceQuadrangulator<V, E, F> quadrangulator = null)
            where V : HeMeshBase<V, E, F>.Vertex, IVertex3d
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return RhinoFactory.Mesh.CreateFromHeMesh(mesh, v => (Point3f)v.Position, null, null, null, quadrangulator);
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="quadrangulator"></param>
        /// <param name="getPosition"></param>
        /// <param name="getNormal"></param>
        /// <param name="getTexture"></param>
        /// <param name="getColor"></param>
        /// <returns></returns>
        public static Mesh ToMesh<V, E, F>(this HeMeshBase<V, E, F> mesh, Func<V, Point3f> getPosition, Func<V, Vector3f> getNormal = null, Func<V, Point2f> getTexture = null, Func<V, Color> getColor = null,  IFaceQuadrangulator<V, E, F> quadrangulator = null)
            where V : HeMeshBase<V, E, F>.Vertex
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return RhinoFactory.Mesh.CreateFromHeMesh(mesh, getPosition, getNormal, getTexture, getColor, quadrangulator);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="getColor"></param>
        /// <param name="quadrangulator"></param>
        /// <returns></returns>
        public static Mesh ToPolySoup<V, E, F>(this HeMeshBase<V, E, F> mesh, Func<F, Color> getColor = null, IFaceQuadrangulator<V, E, F> quadrangulator = null)
            where V : HeMeshBase<V, E, F>.Vertex, IVertex3d
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return RhinoFactory.Mesh.CreatePolySoup(mesh, v => (Point3f)v.Position, getColor, quadrangulator);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="getPosition"></param>
        /// <param name="getColor"></param>
        /// <param name="quadrangulator"></param>
        /// <returns></returns>
        public static Mesh ToPolySoup<V, E, F>(this HeMeshBase<V, E, F> mesh, Func<V, Point3f> getPosition, Func<F,Color> getColor, IFaceQuadrangulator<V, E, F> quadrangulator = null)
            where V : HeMeshBase<V, E, F>.Vertex
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return RhinoFactory.Mesh.CreatePolySoup(mesh, getPosition, getColor, quadrangulator);
        }

        #endregion


        #region HeGraphFactory

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="lines"></param>
        /// <param name="tolerance"></param>
        /// <param name="allowMultiEdges"></param>
        /// <param name="allowLoops"></param>
        /// <returns></returns>
        public static G CreateFromLineSegments<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, IEnumerable<Line> lines, double tolerance = SlurMath.ZeroTolerance, bool allowMultiEdges = false, bool allowLoops = false)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex, IVertex3d
            where E : HeGraphBase<V, E>.Halfedge
        {
            return factory.CreateFromLineSegments(lines, (v, p) => v.Position = p, tolerance, allowMultiEdges, allowLoops);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="factory"></param>
        /// <param name="lines"></param>
        /// <param name="setPosition"></param>
        /// <param name="tolerance"></param>
        /// <param name="allowMultiEdges"></param>
        /// <param name="allowLoops"></param>
        /// <returns></returns>
        public static G CreateFromLineSegments<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, IEnumerable<Line> lines, Action<V, Vec3d> setPosition, double tolerance = SlurMath.ZeroTolerance, bool allowMultiEdges = false, bool allowLoops = false)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex
            where E : HeGraphBase<V, E>.Halfedge
        {
            var pts = new List<Vec3d>();
            foreach(var ln in lines)
            {
                pts.Add(ln.From);
                pts.Add(ln.To);
            }
            
            return factory.CreateFromLineSegments(pts, setPosition, tolerance, allowMultiEdges, allowLoops);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static G CreateFromVertexTopology<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, Mesh mesh)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex, IVertex3d
            where E : HeGraphBase<V, E>.Halfedge
        {
            return factory.CreateFromVertexTopology(
                mesh,
                (v, p) => v.Position = (Vec3d)p,
                (v, n) => v.Normal = (Vec3d)n,
                (v, t) => v.Texture = (Vec2d)t,
                delegate { }
                );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <param name="setPosition"></param>
        /// <param name="setNormal"></param>
        /// <param name="setTexture"></param>
        /// <param name="setColor"></param>
        /// <returns></returns>
        public static G CreateFromVertexTopology<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, Mesh mesh, Action<V, Point3f> setPosition, Action<V, Vector3f> setNormal, Action<V, Point2f> setTexture, Action<V, Color> setColor)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex
            where E : HeGraphBase<V, E>.Halfedge
        {
            // TODO
            throw new NotImplementedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static G CreateFromFaceTopology<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, Mesh mesh)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex, IVertex3d
            where E : HeGraphBase<V, E>.Halfedge
        {
            return factory.CreateFromFaceTopology(
                mesh,
                (v, p) => v.Position = (Vec3d)p,
                (v, n) => v.Normal = (Vec3d)n,
                (v, t) => v.Texture = (Vec2d)t,
                delegate { }
                );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <param name="setPosition"></param>
        /// <param name="setNormal"></param>
        /// <param name="setTexture"></param>
        /// <param name="setColor"></param>
        /// <returns></returns>
        public static G CreateFromFaceTopology<G, V, E>(this HeGraphBaseFactory<G, V, E> factory, Mesh mesh, Action<V, Point3f> setPosition, Action<V, Vector3f> setNormal, Action<V, Point2f> setTexture, Action<V, Color> setColor)
            where G : HeGraphBase<V, E>
            where V : HeGraphBase<V, E>.Vertex
            where E : HeGraphBase<V, E>.Halfedge
        {
            // TODO
            throw new NotImplementedException();
        }

        #endregion


        #region HeMeshFactory

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static M CreateFromMesh<M, V, E, F>(this HeMeshBaseFactory<M, V, E, F> factory, Mesh mesh)
            where M : HeMeshBase<V, E, F>
            where V : HeMeshBase<V, E, F>.Vertex, IVertex3d
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return factory.CreateFromMesh(
                mesh,
                (v, p) => v.Position = (Vec3d)p,
                (v, n) => v.Normal = (Vec3d)n,
                (v, t) => v.Texture = (Vec2d)t,
                delegate { });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="factory"></param>
        /// <param name="mesh"></param>
        /// <param name="setPosition"></param>
        /// <param name="setNormal"></param>
        /// <param name="setTexture"></param>
        /// <param name="setColor"></param>
        /// <returns></returns>
        public static M CreateFromMesh<M, V, E, F>(this HeMeshBaseFactory<M, V, E, F> factory, Mesh mesh, Action<V, Point3f> setPosition, Action<V, Vector3f> setNormal, Action<V, Point2f> setTexture, Action<V, Color> setColor)
            where M : HeMeshBase<V, E, F>
            where V : HeMeshBase<V, E, F>.Vertex
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            var verts = mesh.Vertices;
            var faces = mesh.Faces;
            var norms = mesh.Normals;
            var texCoords = mesh.TextureCoordinates;
            var colors = mesh.VertexColors;

            var result = factory.Create(verts.Count, verts.Count << 3, faces.Count);
            bool hasNorms = (norms.Count == verts.Count);
            bool hasTexCoords = (texCoords.Count == verts.Count);
            bool hasColors = (colors.Count == verts.Count);

            // add vertices
            for (int i = 0; i < verts.Count; i++)
            {
                var v = result.AddVertex();
                setPosition(v, verts[i]);

                if (hasNorms) setNormal(v, norms[i]);
                if (hasTexCoords) setTexture(v, texCoords[i]);
                if (hasColors) setColor(v, colors[i]);
            }

            // add faces
            for (int i = 0; i < faces.Count; i++)
            {
                MeshFace f = faces[i];
                if (f.IsQuad)
                    result.AddFace(f.A, f.B, f.C, f.D);
                else
                    result.AddFace(f.A, f.B, f.C);
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="factory"></param>
        /// <param name="polylines"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static M CreateFromPolylines<M, V, E, F>(this HeMeshBaseFactory<M, V, E, F> factory, IEnumerable<Polyline> polylines, double tolerance = SlurMath.ZeroTolerance)
            where M : HeMeshBase<V, E, F>
            where V : HeMeshBase<V, E, F>.Vertex, IVertex3d
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return factory.CreateFromPolylines(polylines, (v, p) => v.Position = p, tolerance);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="factory"></param>
        /// <param name="polylines"></param>
        /// <param name="setPosition"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static M CreateFromPolylines<M, V, E, F>(this HeMeshBaseFactory<M, V, E, F> factory, IEnumerable<Polyline> polylines, Action<V, Vec3d> setPosition, double tolerance = SlurMath.ZeroTolerance)
            where M : HeMeshBase<V, E, F>
            where V : HeMeshBase<V, E, F>.Vertex
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            List<Vec3d> points = new List<Vec3d>();
            List<int> sizes = new List<int>();

            // get all polyline points
            foreach (Polyline p in polylines)
            {
                int n = p.Count - 1;
                if (!p.IsClosed || n < 3) continue;  // skip open or invalid loops

                // collect all points in the loop
                for (int i = 0; i < n; i++)
                    points.Add(p[i]);

                sizes.Add(n);
            }

            var vertPos = points.RemoveCoincident(out int[] indexMap, tolerance);
            return factory.CreateFromFaceVertexData(vertPos, indexMap.Segment(sizes), setPosition);
        }

        #endregion


        #region HeQuadStrip

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="strip"></param>
        /// <returns></returns>
        public static Mesh ToMesh<V, E, F>(this HeQuadStrip<V, E, F> strip)
            where V : HeMeshBase<V, E, F>.Vertex, IVertex3d
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return ToMesh(strip, v => (Point3f)v.Position);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="strip"></param>
        /// <param name="getPosition"></param>
        /// <param name="getNormal"></param>
        /// <param name="getTexture"></param>
        /// <param name="getColor"></param>
        /// <returns></returns>
        public static Mesh ToMesh<V, E, F>(this HeQuadStrip<V, E, F> strip, Func<V, Point3f> getPosition, Func<V, Vector3f> getNormal = null, Func<V, Point2f> getTexture = null, Func<V, Color> getColor = null)
            where V : HeMeshBase<V, E, F>.Vertex
            where E : HeMeshBase<V, E, F>.Halfedge
            where F : HeMeshBase<V, E, F>.Face
        {
            return RhinoFactory.Mesh.CreateFromQuadStrip(strip, getPosition, getNormal, getTexture, getColor);
        }

        #endregion
    }
}

#endif