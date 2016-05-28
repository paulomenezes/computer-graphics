using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ComputacaoGrafica
{
    struct Camera
    {
        public Point C;
        public Point N;
        public Point V;
        public int d;
        public int hX;
        public int hY;

        public Camera(int x)
        {
            C = new Point(0, -500, 500);
            N = new Point(0, 1, -1);
            V = new Point(0, -1, -1);
            d = 5;
            hX = 2;
            hY = 2;
        }
    }

    class Luz
    {
        public Color Iamb = Color.FromRgb(255, 0, 0);
        public Color Il = Color.FromRgb(255, 0, 0);
        public double Ka = 0.5;
        public double Ks = 0.4;
        public int n = 5;
        public Point Kd = new Point(0.5, 0, 0);
        public Point Od = new Point(0.5, 0, 0);
        public Point Pl = new Point(0, 100, 200);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MathHelper maths = new MathHelper();
        Camera camera = new Camera(0);

        Luz luz = new Luz();

        WriteableBitmap wbitmap = new WriteableBitmap(500, 500, 96, 96, PixelFormats.Bgra32, null);
        byte[,,] pixels = new byte[500, 500, 4];

        double[,] zBuffer = new double[500, 500];

        public MainWindow()
        {
            int width = 500;
            int height = 500;

            InitializeComponent();

            for (int i = 0; i < zBuffer.GetLength(0); i++)
            {
                for (int j = 0; j < zBuffer.GetLength(1); j++)
                {
                    zBuffer[i, j] = int.MinValue;
                }
            }

            String file = File.ReadAllText("vaso.byu");

            List<double[]> vertices = new List<double[]>();
            List<int[]> triangulos = new List<int[]>();

            string[] data = file.Split(new string[2] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var vLen = data[0].Split(' ')[0];
            var tLen = data[0].Split(' ')[1];

            for (var i = 1; i <= int.Parse(vLen); i++)
            {
                string[] arr = data[i].Split(' ');
                double[] nArr = new double[arr.Length];

                for (int j = 0; j < arr.Length; j++)
                    nArr[j] = Convert.ToDouble(arr[j].Replace('.', ','));

                vertices.Add(nArr);
            }

            for (var i = int.Parse(vLen) + 1; i < data.Count(); i++)
            {
                string[] arr = data[i].Split(' ');
                int[] nArr = new int[arr.Length];

                for (int j = 0; j < arr.Length; j++)
                    nArr[j] = Convert.ToInt32(arr[j]);

                triangulos.Add(nArr);
            }

            List<Point> trianguloNormal = new List<Point>();
            List<double[,]> trianguloBarycentric = new List<double[,]>();

            for (var i = 0; i < triangulos.Count; i++)
            {
                double[,] verticesTriangulo = new double[3, 3] {
                    { vertices[triangulos[i][0] - 1][0], vertices[triangulos[i][0] - 1][1], vertices[triangulos[i][0] - 1][2] },
                    { vertices[triangulos[i][1] - 1][0], vertices[triangulos[i][1] - 1][1], vertices[triangulos[i][1] - 1][2] },
                    { vertices[triangulos[i][2] - 1][0], vertices[triangulos[i][2] - 1][1], vertices[triangulos[i][2] - 1][2] }
                };
                
                List<Point> coordenadasVista = new List<Point>();

                for (var j = 0; j < verticesTriangulo.GetLength(0); j++)
                {
                    double[,] vista = coordenadaVista(new Point(verticesTriangulo[j, 0], verticesTriangulo[j, 1], verticesTriangulo[j, 2]));
                    
                    double Xs = (camera.d / camera.hX) * (vista[0, 0] / vista[2, 0]);
                    double Ys = (camera.d / camera.hY) * (vista[1, 0] / vista[2, 0]);

                    coordenadasVista.Add(new Point(Xs, Ys, camera.d));
                }

                Point V = maths.subtracaoPontos(coordenadasVista[1], coordenadasVista[0]);
                Point W = maths.subtracaoPontos(coordenadasVista[2], coordenadasVista[0]);
                Point normalTriangulo = maths.normalizar(maths.produtoVetorial(V, W));

                trianguloNormal.Add(normalTriangulo);
            }

            List<Point> verticeNormal = new List<Point>();
            Dictionary<int, Point> barycentric = new Dictionary<int, Point>();

            for (int i = 0; i < trianguloNormal.Count - 2; i++)
            {
                Point p = new Point(0, 0, 0);
                for (int j = 0; j < 3; j++)
                {
                    p.x += trianguloNormal[i + j].x;
                    p.y += trianguloNormal[i + j].y;
                    p.z += trianguloNormal[i + j].z;
                }

                verticeNormal.Add(maths.normalizar(p));
                
                Point p0 = new Point(
                    (trianguloNormal[i].x + trianguloNormal[i + 1].x + trianguloNormal[i + 2].x) / 3,
                    (trianguloNormal[i].y + trianguloNormal[i + 1].y + trianguloNormal[i + 2].y) / 3,
                    (trianguloNormal[i].z + trianguloNormal[i + 1].z + trianguloNormal[i + 2].z) / 3);

                barycentric.Add(i, p0);
            }

            var sorted = barycentric.OrderBy(x => x.Value.z).ToDictionary(pair => pair.Key, pair => pair.Value);
                        
            for (int i = 0; i < triangulos.Count; i++)
            {
                double[,] verticesTriangulo = new double[3, 3] {
                    { vertices[triangulos[i][0] - 1][0], vertices[triangulos[i][0] - 1][1], vertices[triangulos[i][0] - 1][2] },
                    { vertices[triangulos[i][1] - 1][0], vertices[triangulos[i][1] - 1][1], vertices[triangulos[i][1] - 1][2] },
                    { vertices[triangulos[i][2] - 1][0], vertices[triangulos[i][2] - 1][1], vertices[triangulos[i][2] - 1][2] }
                };

                List<Point> verticesVista = new List<Point>();

                for (var j = 0; j < verticesTriangulo.GetLength(0); j++)
                {
                    double[,] vista = coordenadaVista(new Point(verticesTriangulo[j, 0], verticesTriangulo[j, 1], verticesTriangulo[j, 2]));

                    double Xs = (camera.d / camera.hX) * (vista[0, 0] / vista[2, 0]);
                    double Ys = (camera.d / camera.hY) * (vista[1, 0] / vista[2, 0]);
                    
                    double xi = (((Xs + 1) / 2) * width);
                    double xj = (height - ((Ys + 1) / 2) * height);

                    verticesVista.Add(new Point(xi, xj, 0));
                }

                if (verticesVista[0].y > verticesVista[1].y) verticesVista = swap(verticesVista, 0, 1);
                if (verticesVista[0].y > verticesVista[2].y) verticesVista = swap(verticesVista, 0, 2);
                if (verticesVista[1].y > verticesVista[2].y) verticesVista = swap(verticesVista, 1, 2);

                Point baricentro = new Point(
                    (verticesVista[0].x + verticesVista[1].x + verticesVista[2].x) / 3,
                    (verticesVista[0].y + verticesVista[1].y + verticesVista[2].y) / 3,
                    (verticesVista[0].z + verticesVista[1].z + verticesVista[2].z) / 3);

                Point N = maths.normalizar(baricentro);
                Point L = maths.subtracaoPontos(luz.Pl, barycentric.ContainsKey(i) ? barycentric[i] : baricentro);

                Color Ia = Color.FromRgb(
                    (byte)(luz.Ka * luz.Iamb.R),
                    (byte)(luz.Ka * luz.Iamb.G),
                    (byte)(luz.Ka * luz.Iamb.B));

                double NxL = maths.produtoEscalar(N, L);
                
                Color Id = Color.FromRgb(
                    (byte)(NxL * luz.Kd.x * luz.Od.x * luz.Il.R),
                    (byte)(NxL * luz.Kd.y * luz.Od.y * luz.Il.G),
                    (byte)(NxL * luz.Kd.z * luz.Od.z * luz.Il.B));

                Color I = Color.FromRgb(
                    (byte)(Ia.R + Id.R), 
                    (byte)(Ia.G + Id.G), 
                    (byte)(Ia.B + Id.B));
                
                int z = barycentric.ContainsKey(i) ? (int)barycentric[i].z : (int)baricentro.z;
                
                if ((int)verticesVista[1].y == (int)verticesVista[2].y)
                {
                    drawTopBottom(verticesVista, 0, 1, 2, I, z);
                }
                else if ((int)verticesVista[0].y == (int)verticesVista[1].y)
                {
                    drawBottomTop(verticesVista, 0, 1, 2, I, z);
                }
                else
                {
                    var v4x = (verticesVista[0].x + ((verticesVista[1].y - verticesVista[0].y) / (verticesVista[2].y - verticesVista[0].y)) * (verticesVista[2].x - verticesVista[0].x));
                    verticesVista.Add(new Point(v4x, verticesVista[1].y, 0));

                    drawTopBottom(verticesVista, 0, 1, 3, I, z);
                    drawBottomTop(verticesVista, 1, 3, 2, I, z);
                }
            }

            double[,] vistaCamera = coordenadaVista(luz.Pl);
            double XsCamera = (camera.d / camera.hX) * (vistaCamera[0, 0] / vistaCamera[2, 0]);
            double YsCamera = (camera.d / camera.hY) * (vistaCamera[1, 0] / vistaCamera[2, 0]);
            double xiCamera = (((XsCamera + 1) / 2) * width);
            double xjCamera = (height - ((YsCamera + 1) / 2) * height);

            AddPixel(xiCamera - 1, xjCamera, Colors.Blue, 99);
            AddPixel(xiCamera + 1, xjCamera, Colors.Blue, 99);
            AddPixel(xiCamera, xjCamera - 1, Colors.Blue, 99);
            AddPixel(xiCamera, xjCamera + 1, Colors.Blue, 99);
            AddPixel(xiCamera, xjCamera, Colors.Blue, 99);

            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[height * width * 4];
            int index = 0;
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    pixels[row, col, 3] = 255;

                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = pixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = 4 * width;
            wbitmap.WritePixels(rect, pixels1d, stride, 0);

            // Create an Image to display the bitmap.
            Image image = new Image();
            image.Stretch = Stretch.None;
            image.Margin = new Thickness(0);

            grdMain.Children.Add(image);

            //Set the Image source.
            image.Source = wbitmap;
        }

        double ComputeNDotL(Point vertex, Point normal, Point lightPosition)
        {
            var lightDirection = maths.subtracaoPontos(lightPosition, vertex);

            normal = maths.normalizar(normal);
            lightDirection = maths.normalizar(lightDirection);

            return Math.Max(0, maths.produtoEscalar(normal, lightDirection));
        }

        private void drawTopBottom (List<Point> triV, int idx1, int idx2, int idx3, Color color, int z)
        {
            Point v1 = triV[idx1];
            Point v2 = triV[idx2];
            Point v3 = triV[idx3];
            
            double invslope1 = (v2.x - v1.x) / (v2.y - v1.y);
            double invslope2 = (v3.x - v1.x) / (v3.y - v1.y);

            if (invslope1 > 20)
                invslope1 = invslope2;

            double x1 = v1.x;
            double x2 = v1.x + 0.5;
            
            for (var scanlineY = (int)v1.y; scanlineY <= v2.y; scanlineY++)
            {
                var xMin = x1 < x2 ? x1 : x2;
                var xMax = x1 > x2 ? x1 : x2;

                for (int k = (int)xMin; k < xMax; k++)
                {
                    AddPixel(k, scanlineY, color, z);
                }
                x1 += invslope1;
                x2 += invslope2;
            }
        }

        private void drawBottomTop(List<Point> triV, int idx1, int idx2, int idx3, Color color, int z)
        {
            Point v1 = triV[idx1];
            Point v2 = triV[idx2];
            Point v3 = triV[idx3];

            double invslope1 = (v3.x - v1.x) / (v3.y - v1.y);
            double invslope2 = (v3.x - v2.x) / (v3.y - v2.y);
            
            double x1 = v3.x;
            double x2 = v3.x + 0.5;
            
            for (int j = (int)v3.y; j > v1.y; j--)
            {
                var xMin = x1 < x2 ? x1 : x2;
                var xMax = x1 > x2 ? x1 : x2;
                for (int k = (int)xMin; k < xMax; k++)
                {
                    AddPixel(k, j, color, z);
                }
                x1 -= invslope1;
                x2 -= invslope2;
            }
        }

        private List<Point> swap(List<Point> array, int i, int j)
        {
            var aux = array[i];
            array[i] = array[j];
            array[j] = aux;

            return array;
        }

        private double[,] coordenadaVista(Point ponto)
        {
            // ortogonalizar V
            var prod = maths.produtoEscalar(camera.V, camera.N) / maths.produtoEscalar(camera.N, camera.N);
            var vLinha = new Point(camera.V.x - prod * camera.N.x, camera.V.y - prod * camera.N.y, camera.V.z - prod * camera.N.z);

            // Normalização
            camera.N = maths.normalizar(camera.N);
            vLinha = maths.normalizar(vLinha);

            // U = N x V'
            Point U = new Point(
                camera.N.y * vLinha.z - camera.N.z * vLinha.y,
                camera.N.z * vLinha.x - camera.N.x * vLinha.z,
                camera.N.x * vLinha.y - camera.N.y * vLinha.x);

            Point alpha = new Point(maths.normalizar(U), vLinha, camera.N);

            double[,] matrizTransformacao = new double[3, 3]
            {
                { alpha.a.x, alpha.a.y, alpha.a.z },
                { alpha.b.x, alpha.b.y, alpha.b.z },
                { alpha.c.x, alpha.c.y, alpha.c.z },
            };

            Point subPonto = maths.subtracaoPontos(camera.C, ponto);

            return maths.multiplicarMatriz(matrizTransformacao, new double[3, 1]
            {
                { subPonto.x },
                { subPonto.y },
                { subPonto.z }
            });
        }
        
        private void AddPixel(double x, double y, Color color, int z)
        {
            if (zBuffer[(int)x, (int)y] < z)
            {
                pixels[(int)y, (int)x, 0] = color.B; 
                pixels[(int)y, (int)x, 1] = color.G; 
                pixels[(int)y, (int)x, 2] = color.R; 
                pixels[(int)y, (int)x, 3] = 255;
            }

            zBuffer[(int)x, (int)y] = z;
        }
    }
}
