﻿using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rampancy
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToUnity(this System.Numerics.Vector2 vec2)
        {
            return new(vec2.X, vec2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 ToNumerics(this Vector2 vec2)
        {
            return new(vec2.x, vec2.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToUnity(this System.Numerics.Vector3 vec3)
        {
            return new(vec3.X, vec3.Y, vec3.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToNumerics(this Vector3 vec3)
        {
            return new(vec3.x, vec3.y, vec3.z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToNumerics(this Color color)
        {
            return new(color.r, color.g, color.b);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToYUp(this Vector3 vec3)
        {
            return new(vec3.x, vec3.z, vec3.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToUnity(this System.Numerics.Vector4 vec4)
        {
            return new(vec4.X, vec4.Y, vec4.Z, vec4.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 ToNumerics(this Vector4 vec4)
        {
            return new(vec4.x, vec4.y, vec4.z, vec4.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToUnity(this System.Numerics.Quaternion quat)
        {
            return new(quat.X, quat.Y, quat.Z, quat.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Quaternion ToNumerics(this Quaternion quat)
        {
            return new(quat.x, quat.y, quat.z, quat.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Quaternion ToNumericsYUpToZUp(this Quaternion quat)
        {
            return new(-quat.x, -quat.z, quat.y, quat.w);
        }
    }
}