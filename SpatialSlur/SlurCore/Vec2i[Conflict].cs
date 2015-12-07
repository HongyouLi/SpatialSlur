﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialSlur.SlurCore
{
    public struct Vec2i
    {
        #region Static

        /// <summary>
        /// 
        /// </summary>
        public static Vec2i Zero
        {
            get { return new Vec2i(0, 0); }
        }


        /// <summary>
        /// 
        /// </summary>
        public static Vec2i UnitX
        {
            get { return new Vec2i(1, 0); }
        }


        /// <summary>
        /// 
        /// </summary>
        public static Vec2i UnitY
        {
            get { return new Vec2i(0, 1); }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static bool operator ==(Vec2i v0, Vec2i v1)
        {
            return (v0.x == v1.x) && (v0.y == v1.y);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static bool operator !=(Vec2i v0, Vec2i v1)
        {
            return (v0.x != v1.x) || (v0.y != v1.y);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vec2i operator +(Vec2i v0, Vec2i v1)
        {
            v0.x += v1.x;
            v0.y += v1.y;
            return v0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vec2i operator -(Vec2i v0, Vec2i v1)
        {
            v0.x -= v1.x;
            v0.y -= v1.y;
            return v0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vec2i operator -(Vec2i v)
        {
            v.x = -v.x;
            v.y = -v.y;
            return v;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vec2i operator *(Vec2i v, int t)
        {
            v.x *= t;
            v.y *= t;
            return v;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vec2i operator *(int t, Vec2i v)
        {
            v.x *= t;
            v.y *= t;
            return v;
        }


        /// <summary>
        /// returns the dot product of two vectors
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int operator *(Vec2i v0, Vec2i v1)
        {
            return v0.x * v1.x + v0.y * v1.y;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static implicit operator Vec2i(Vec3i v)
        {
            return new Vec2i(v.x, v.y);
        }

        #endregion


        public int x, y;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Vec2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double Length
        {
            get { return Math.Sqrt(SquareLength); }
        }


        /// <summary>
        /// 
        /// </summary>
        public int SquareLength
        {
            get { return x * x + y * y; }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ManhattanLength
        {
            get { return x + y; }
        }


        /// <summary>
        /// 
        /// </summary>
        public bool IsZero
        {
            get { return x == 0 && y == 0; }
        }


        /// <summary>
        /// 
        /// </summary>
        public bool IsUnit
        {
            get { return SquareLength == 1; }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Set(int x, int y)
        {
            this.x = x;
            this.y = y;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // allow overflow
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                return hash;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Vec2i)
                return this == (Vec2i)obj;

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public double DistanceTo(Vec2i other)
        {
            other -= this;
            return other.Length;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public double SquareDistanceTo(Vec2i other)
        {
            other -= this;
            return other.SquareLength;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public double ManhattanDistanceTo(Vec2i other)
        {
            other -= this;
            return other.ManhattanLength;
        }

    }
}