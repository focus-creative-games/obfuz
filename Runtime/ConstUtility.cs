// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
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
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            Type elementType = array.GetType().GetElementType();
            if (elementType.IsEnum)
            {
                CopyEnumArrayData(array, elementType, data, offset, length);
            }
            else
            {
                Buffer.BlockCopy(data, offset, array, 0, length);
            }
        }

        private static unsafe void CopyEnumArrayData(Array array, Type elementType, byte[] data, int offset, int length)
        {
            // Buffer.BlockCopy does not accept enum arrays on all Unity runtimes.
            // Enums contain no references and have the layout of their underlying type.
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (offset < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(offset < 0 ? nameof(offset) : nameof(length));
            }
            long byteLength = array.LongLength * UnsafeUtility.SizeOf(Enum.GetUnderlyingType(elementType));
            if (offset > data.Length - length || length > byteLength)
            {
                throw new ArgumentException("Source or destination array is too short.");
            }
            if (length == 0)
            {
                return;
            }
            void* destination = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out ulong handle);
            try
            {
                fixed (byte* source = &data[offset])
                {
                    UnsafeUtility.MemCpy(destination, source, length);
                }
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(handle);
            }
        }

        public static unsafe int CastFloatAsInt(float value)
        {
            int* intValue = (int*)&value;
            return *intValue;
        }

        public static unsafe float CastIntAsFloat(int value)
        {
            float* floatValue = (float*)&value;
            return *floatValue;
        }

        public static unsafe long CastDoubleAsLong(double value)
        {
            long* longValue = (long*)&value;
            return *longValue;
        }

        public static unsafe double CastLongAsDouble(long value)
        {
            double* doubleValue = (double*)&value;
            return *doubleValue;
        }
    }
}
