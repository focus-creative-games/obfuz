using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Obfuz
{
    public static class ConstUtility
    {
        public static int GetInt(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static long GetLong(byte[] data, int offset)
        {
            return BitConverter.ToInt64(data, offset);
        }

        public static float GetFloat(byte[] data, int offset)
        {
            return BitConverter.ToSingle(data, offset);
        }

        public static double GetDouble(byte[] data, int offset)
        {
            return BitConverter.ToDouble(data, offset);
        }

        public static string GetString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        public static byte[] GetBytes(byte[] data, int offset, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, offset, result, 0, length);
            return result;
        }

        public static int[] GetInts(byte[] data, int offset, int byteLength)
        {
            Assert.IsTrue(byteLength % 4 == 0);
            int[] result = new int[byteLength >> 2];
            Buffer.BlockCopy(data, offset, result, 0, byteLength);
            return result;
        }

        public static void InitializeArray(Array array, byte[] data, int offset, int length)
        {
            Buffer.BlockCopy(data, offset, array, 0, length);
        }

        public static int CastFloatAsInt(float value)
        {
            return UnsafeUtility.As<float, int>(ref value);
        }

        public static float CastIntAsFloat(int value)
        {
            return UnsafeUtility.As<int, float>(ref value);
        }

        public static long CastDoubleAsLong(double value)
        {
            return UnsafeUtility.As<double, long>(ref value);
        }

        public static double CastLongAsDouble(long value)
        {
            return UnsafeUtility.As<long, double>(ref value);
        }
    }
}
