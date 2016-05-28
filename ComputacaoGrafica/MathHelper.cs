using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputacaoGrafica
{
    class Point
    {
        public double x;
        public double y;
        public double z;

        public Point a;
        public Point b;
        public Point c;

        public Point(Point a, Point b, Point c) : this(0, 0, 0)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public Point(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }
    }

    class MathHelper
    {
        public double[,] multiplicarMatriz(double[,] m1, double[,] m2)
        {
            double[,] resultado = new double[m1.GetLength(1), m2.Length];

            for (var i = 0; i < m1.GetLength(0); i++)
            {
                for (var j = 0; j < m2.GetLength(1); j++)
                {
                    double t = 0;
                    for (var k = 0; k < m1.GetLength(1); k++)
                    {
                        t += m1[i,k] * m2[k,j];
                    }

                    resultado[i,j] = t;
                }
            }

            return resultado;
        }

        public Point subtracaoPontos(Point ponto1, Point ponto2)
        {
            return new Point(ponto2.x - ponto1.x, ponto2.y - ponto1.y, ponto2.z - ponto1.z);
        }

        public double produtoEscalar(Point vetor1, Point vetor2)
        {
            return (vetor1.x * vetor2.x) + (vetor1.y * vetor2.y) + (vetor1.z * vetor2.z);
        }

        public Point produtoVetorial(Point vetor1, Point vetor2)
        {
            return new Point(
                    vetor1.y * vetor2.z - vetor1.z * vetor2.y,
                    vetor1.z * vetor2.x - vetor1.x * vetor2.z,
                    vetor1.x * vetor2.y - vetor1.y * vetor2.x);
        }

        public double norma(Point vetor1)
        {
            return Math.Sqrt(Math.Pow(vetor1.x, 2) + Math.Pow(vetor1.y, 2) + Math.Pow(vetor1.z, 2));
        }

        public Point normalizar(Point vector)
        {
            double n = norma(vector);

            return new Point(vector.x / n, vector.y / n, vector.z / n);
        }

        public double[,] coordenadasBaricentricas(Point p, Point p1, Point p2, Point p3)
        {
            double[,] matriz = new double[2, 2] {
                { p1.x - p3.x, p2.x - p3.x},
                {p1.y - p3.y, p2.y - p3.y}
            };

            double cof = 1 / (matriz[1,1] * matriz[0,0] - matriz[0,1] * matriz[1,0]);

            double[,] inv = new double[2, 2] {
                { cof * matriz[0,0], cof * matriz[0,1] },
                { cof * matriz[1,0], cof * matriz[1,1] }
            };

            double[,] outraMatriz = new double[2, 1] {
                { p.x - p3.x },
                { p.y - p3.y }
            };

            double[,] alphaBeta = multiplicarMatriz(inv, outraMatriz);

            return new double[1, 3]
            {
                { alphaBeta[0,0], alphaBeta[1,0], 1 - alphaBeta[0,0] - alphaBeta[1,0] }
            };
        }
    }
}
