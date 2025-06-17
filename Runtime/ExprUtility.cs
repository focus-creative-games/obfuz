namespace Obfuz
{
    public static class ExprUtility
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static long Add(long a, long b)
        {
            return a + b;
        }

        public static float Add(float a, float b)
        {
            return a + b;
        }

        public static double Add(double a, double b)
        {
            return a + b;
        }

        public static int Subtract(int a, int b)
        {
            return a - b;
        }

        public static long Subtract(long a, long b)
        {
            return a - b;
        }

        public static float Subtract(float a, float b)
        {
            return a - b;
        }

        public static double Subtract(double a, double b)
        {
            return a - b;
        }

        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        public static long Multiply(long a, long b)
        {
            return a * b;
        }

        public static float Multiply(float a, float b)
        {
            return a * b;
        }

        public static double Multiply(double a, double b)
        {
            return a * b;
        }

        public static int Divide(int a, int b)
        {
            return a / b;
        }

        public static long Divide(long a, long b)
        {
            return a / b;
        }

        public static float Divide(float a, float b)
        {
            return a / b;
        }

        public static double Divide(double a, double b)
        {
            return a / b;
        }

        public static int DivideUn(int a, int b)
        {
            return (int)((uint)a / (uint)b);
        }

        public static long DivideUn(long a, long b)
        {
            return (long)((ulong)a / (ulong)b);
        }

        public static int Rem(int a, int b)
        {
            return a % b;
        }

        public static long Rem(long a, long b)
        {
            return a % b;
        }

        public static float Rem(float a, float b)
        {
            return a % b;
        }

        public static double Rem(double a, double b)
        {
            return a % b;
        }

        public static int RemUn(int a, int b)
        {
            return (int)((uint)a % (uint)b);
        }

        public static long RemUn(long a, long b)
        {
            return (long)((ulong)a % (ulong)b);
        }

        public static int Negate(int a)
        {
            return -a;
        }

        public static long Negate(long a)
        {
            return -a;
        }

        public static float Negate(float a)
        {
            return -a;
        }

        public static double Negate(double a)
        {
            return -a;
        }

        public static int And(int a, int b)
        {
            return a & b;
        }

        public static long And(long a, long b)
        {
            return a & b;
        }

        public static int Or(int a, int b)
        {
            return a | b;
        }

        public static long Or(long a, long b)
        {
            return a | b;
        }

        public static int Xor(int a, int b)
        {
            return a ^ b;
        }

        public static long Xor(long a, long b)
        {
            return a ^ b;
        }

        public static int Not(int a)
        {
            return ~a;
        }

        public static long Not(long a)
        {
            return ~a;
        }

        public static int ShiftLeft(int a, int b)
        {
            return a << b;
        }

        public static long ShiftLeft(long a, int b)
        {
            return a << b;
        }

        public static int ShiftRight(int a, int b)
        {
            return a >> b;
        }

        public static long ShiftRight(long a, int b)
        {
            return a >> b;
        }

        public static int ShiftRightUn(int a, int b)
        {
            return (int)((uint)a >> b);
        }

        public static long ShiftRightUn(long a, int b)
        {
            return (long)((ulong)a >> b);
        }
    }
}
