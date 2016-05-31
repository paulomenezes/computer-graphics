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
        public double Ka = 0.5f;
        public double Ks = 0.6f;
        public int n = 5;
        public Point Kd = new Point(0.5f, 0, 0);
        public Point Od = new Point(0.5f, 0, 0);
        public Point Pl = new Point(-100, 0, -500);
    }

    struct C
    {
        public int R, G, B;

        public C(int R, int G, int B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }
    }

    class Triangle
    {
        public List<Point> vertices;
        public List<Point> coordenadaVista;
        public List<Point> normaisVertices;
        public List<Point> coordenadaTela;
        public Point normal;
        public Point baricentro;

        public int[] verticesIndex;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MathHelper maths = new MathHelper();
        Camera camera = new Camera(0);

        Luz luz = new Luz();

        int WIDTH = 500;
        int HEIGHT = 500;

        WriteableBitmap wbitmap = new WriteableBitmap(500, 500, 96, 96, PixelFormats.Bgra32, null);
        byte[,,] pixels = new byte[500, 500, 4];

        double[,] zBuffer = new double[500, 500];

        List<double[]> vertices = new List<double[]>();
        List<int[]> triangulos = new List<int[]>();

        List<Triangle> triangles = new List<Triangle>();
        
        private void LoadFile(string name)
        {
            String file = File.ReadAllText(name + ".byu");

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
        }
        
        private void DrawPixels()
        {
            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[HEIGHT * WIDTH * 4];
            int index = 0;
            for (int row = 0; row < HEIGHT; row++)
            {
                for (int col = 0; col < WIDTH; col++)
                {
                    pixels[row, col, 3] = 255;

                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = pixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, WIDTH, HEIGHT);
            int stride = 4 * WIDTH;
            wbitmap.WritePixels(rect, pixels1d, stride, 0);

            // Create an Image to display the bitmap.
            Image image = new Image();
            image.Stretch = Stretch.None;
            image.Margin = new Thickness(0);

            grdMain.Children.Add(image);

            //Set the Image source.
            image.Source = wbitmap;
        }

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < zBuffer.GetLength(0); i++)
            {
                for (int j = 0; j < zBuffer.GetLength(1); j++)
                {
                    zBuffer[i, j] = int.MinValue;
                }
            }

            LoadFile("vaso");

            for (var i = 0; i < triangulos.Count; i++)
            {
                Triangle t = new Triangle();
                t.verticesIndex = new int[3] { triangulos[i][0] - 1, triangulos[i][1] - 1, triangulos[i][2] - 1 };

                t.vertices = new List<Point>();
                t.vertices.Add(new Point(vertices[t.verticesIndex[0]][0], vertices[t.verticesIndex[0]][1], vertices[t.verticesIndex[0]][2]));
                t.vertices.Add(new Point(vertices[t.verticesIndex[1]][0], vertices[t.verticesIndex[1]][1], vertices[t.verticesIndex[1]][2]));
                t.vertices.Add(new Point(vertices[t.verticesIndex[2]][0], vertices[t.verticesIndex[2]][1], vertices[t.verticesIndex[2]][2]));
                
                List<Point> coordenadasVista = new List<Point>();

                for (var j = 0; j < 3; j++)
                {
                    double[,] vista = coordenadaVista(new Point(t.vertices[j].x, t.vertices[j].y, t.vertices[j].z));
                    coordenadasVista.Add(new Point(vista[0, 0], vista[1, 0], vista[2, 0]));
                }

                t.coordenadaVista = coordenadasVista;

                Point V = maths.subtracaoPontos(coordenadasVista[1], coordenadasVista[0]);
                Point W = maths.subtracaoPontos(coordenadasVista[2], coordenadasVista[0]);
                Point normalTriangulo = maths.normalizar(maths.produtoVetorial(V, W));

                t.normal = normalTriangulo;
                
                double x = t.vertices[0].x + t.vertices[1].x + t.vertices[2].x;
                double y = t.vertices[0].y + t.vertices[1].y + t.vertices[2].y;
                double z = t.vertices[0].z + t.vertices[1].z + t.vertices[2].z;
                
                t.baricentro = new Point(x / 3, y / 3, z / 3);

                triangles.Add(t);
            }

            Point[] verticeNormal = new Point[vertices.Count];

            for (int i = 0; i < verticeNormal.Length; i++)
            {
                verticeNormal[i] = new Point(0, 0, 0);
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                verticeNormal[triangles[i].verticesIndex[0]].x += triangles[i].normal.x;
                verticeNormal[triangles[i].verticesIndex[0]].y += triangles[i].normal.y;
                verticeNormal[triangles[i].verticesIndex[0]].z += triangles[i].normal.z;

                verticeNormal[triangles[i].verticesIndex[1]].x += triangles[i].normal.x;
                verticeNormal[triangles[i].verticesIndex[1]].y += triangles[i].normal.y;
                verticeNormal[triangles[i].verticesIndex[1]].z += triangles[i].normal.z;

                verticeNormal[triangles[i].verticesIndex[2]].x += triangles[i].normal.x;
                verticeNormal[triangles[i].verticesIndex[2]].y += triangles[i].normal.y;
                verticeNormal[triangles[i].verticesIndex[2]].z += triangles[i].normal.z;
            }

            for (int i = 0; i < verticeNormal.Length; i++)
            {
                verticeNormal[i] = maths.normalizar(verticeNormal[i]);
            }

            triangles = triangles.OrderBy(p => p.baricentro.z).ToList();
            triangles.Reverse();
                                           
            for (int i = 0; i < triangles.Count; i++)
            {
                triangles[i].normaisVertices = new List<Point>();
                triangles[i].normaisVertices.Add(verticeNormal[triangles[i].verticesIndex[0]]);
                triangles[i].normaisVertices.Add(verticeNormal[triangles[i].verticesIndex[1]]);
                triangles[i].normaisVertices.Add(verticeNormal[triangles[i].verticesIndex[2]]);
                
                List<Point> verticeTela = new List<Point>();

                for (var j = 0; j < 3; j++)
                {
                    double Xs = (camera.d / camera.hX) * (triangles[i].coordenadaVista[j].x / triangles[i].coordenadaVista[j].z);
                    double Ys = (camera.d / camera.hY) * (triangles[i].coordenadaVista[j].y / triangles[i].coordenadaVista[j].z);

                    double I = Math.Floor((((Xs + 1) / 2) * WIDTH) + 0.5);
                    double J = Math.Floor((HEIGHT - ((Ys + 1) / 2) * HEIGHT) + 0.5);

                    verticeTela.Add(new Point(I, J, 0));
                }

                if (verticeTela[0].y > verticeTela[1].y) verticeTela = swap(verticeTela, 0, 1);
                if (verticeTela[0].y > verticeTela[2].y) verticeTela = swap(verticeTela, 0, 2);
                if (verticeTela[1].y > verticeTela[2].y) verticeTela = swap(verticeTela, 1, 2);

                triangles[i].coordenadaTela = verticeTela;

                Point baricentro = new Point(
                    (verticeTela[0].x + verticeTela[1].x + verticeTela[2].x) / 3,
                    (verticeTela[0].y + verticeTela[1].y + verticeTela[2].y) / 3,
                    (verticeTela[0].z + verticeTela[1].z + verticeTela[2].z) / 3);

                double z = triangles[i].baricentro.z;

                if ((int)verticeTela[1].y == (int)verticeTela[2].y)
                {
                    drawTopBottom(triangles[i], 0, 1, 2, z);
                }
                else if ((int)verticeTela[0].y == (int)verticeTela[1].y)
                {
                    drawBottomTop(triangles[i], 0, 1, 2, z);
                }
                else
                {
                    var v4x = (verticeTela[0].x + ((verticeTela[1].y - verticeTela[0].y) / (verticeTela[2].y - verticeTela[0].y)) * (verticeTela[2].x - verticeTela[0].x));
                    verticeTela.Add(new Point(v4x, verticeTela[1].y, 0));
                    
                    drawTopBottom(triangles[i], 0, 1, 3, z);
                    drawBottomTop(triangles[i], 1, 3, 2, z);
                }
            }

            DrawPixels();
        }
        
        private void drawTopBottom (Triangle triangle, int idx1, int idx2, int idx3, double z)
        {
            Point v1 = triangle.coordenadaTela[idx1];
            Point v2 = triangle.coordenadaTela[idx2];
            Point v3 = triangle.coordenadaTela[idx3];
            
            double invslope1 = (v2.x - v1.x) / (v2.y - v1.y);
            double invslope2 = (v3.x - v1.x) / (v3.y - v1.y);
            
            double x1 = v1.x;
            double x2 = v1.x;
            
            for (var y = (int)v1.y; y <= v2.y; y++)
            {
                var xMin = x1 < x2 ? x1 : x2;
                var xMax = x1 > x2 ? x1 : x2;

                for (int x = (int)xMin; x <= xMax; x++)
                {
                    AddPixel(x, y, calcularCor(triangle, x, y, v1, v2, v3), z);
                }

                x1 += invslope1;
                x2 += invslope2;
            }
        }

        private void drawBottomTop(Triangle triangle, int idx1, int idx2, int idx3, double z)
        {
            Point v1 = triangle.coordenadaTela[idx1];
            Point v2 = triangle.coordenadaTela[idx2];
            Point v3 = triangle.coordenadaTela[idx3];

            double invslope1 = (v3.x - v1.x) / (v3.y - v1.y);
            double invslope2 = (v3.x - v2.x) / (v3.y - v2.y);
            
            double x1 = v3.x;
            double x2 = v3.x;
            
            for (int y = (int)v3.y; y >= v1.y; y--)
            {
                var xMin = x1 < x2 ? x1 : x2;
                var xMax = x1 > x2 ? x1 : x2;

                for (int x = (int)xMin; x <= xMax; x++)
                {
                    AddPixel(x, y, calcularCor(triangle, x, y, v1, v2, v3), z);
                }

                x1 -= invslope1;
                x2 -= invslope2;
            }
        }
        
        private Color calcularCor(Triangle triangle, int i, int j, Point p1, Point p2, Point p3)
        {
            double[,] bar = maths.coordenadasBaricentricas(new Point(i, j, 0), p1, p2, p3);

            var alpha = bar[0, 0];
            var beta = bar[0, 1];
            var gama = bar[0, 2];

            Point P = new Point(
                alpha * vertices[triangle.verticesIndex[0]][0] + beta * vertices[triangle.verticesIndex[1]][0] + gama * vertices[triangle.verticesIndex[2]][0],
                alpha * vertices[triangle.verticesIndex[0]][1] + beta * vertices[triangle.verticesIndex[1]][1] + gama * vertices[triangle.verticesIndex[2]][1],
                alpha * vertices[triangle.verticesIndex[0]][2] + beta * vertices[triangle.verticesIndex[1]][2] + gama * vertices[triangle.verticesIndex[2]][2]
            );

            Point N = new Point(
                alpha * triangle.normaisVertices[0].x + beta * triangle.normaisVertices[1].x + gama * triangle.normaisVertices[2].x,
                alpha * triangle.normaisVertices[0].y + beta * triangle.normaisVertices[1].y + gama * triangle.normaisVertices[2].y,
                alpha * triangle.normaisVertices[0].z + beta * triangle.normaisVertices[1].z + gama * triangle.normaisVertices[2].z
            );

            Point L = maths.subtracaoPontos(luz.Pl, P);

            N = maths.normalizar(N);
            L = maths.normalizar(L);

            double NxL = maths.produtoEscalar(N, L);

            Point R = new Point(2 * NxL * N.x - L.x, 2 * NxL * N.y - L.y, 2 * NxL * N.z - L.z);
            Point V = new Point(-P.x, -P.y, -P.z);

            V = maths.normalizar(V);

            double RxV = maths.produtoEscalar(R, V);
            double RxV2 = RxV * RxV;

            if (NxL < 0)
            {
                if (maths.produtoEscalar(V, N) < 0)
                {
                    N = new Point(-N.x, -N.y, -N.z);
                    NxL = maths.produtoEscalar(N, L);

                    R = new Point(2 * NxL * N.x - L.x, 2 * NxL * N.y - L.y, 2 * NxL * N.z - L.z);
                }
                else
                {
                    NxL = 0;
                    RxV2 = 0;
                }
            }

            if (maths.produtoEscalar(V, R) < 0)
            {
                RxV2 = 0;
            }

            C Ia = new C(
                (int)(luz.Ka * luz.Iamb.R),
                (int)(luz.Ka * luz.Iamb.G),
                (int)(luz.Ka * luz.Iamb.B));

            C Id = new C(
                (int)(NxL * luz.Kd.x * luz.Od.x * luz.Il.R),
                (int)(NxL * luz.Kd.y * luz.Od.y * luz.Il.G),
                (int)(NxL * luz.Kd.z * luz.Od.z * luz.Il.B));

            C Is = new C(
                (int)(RxV2 * luz.Ks * luz.Il.R),
                (int)(RxV2 * luz.Ks * luz.Il.G),
                (int)(RxV2 * luz.Ks * luz.Il.B));

            C color = new C(
                Ia.R + Id.R + Is.R,
                Ia.G + Id.G + Is.G,
                Ia.B + Id.B + Is.B);
            
            color.R = color.R > 255 ? 255 : color.R;
            color.G = color.G > 255 ? 255 : color.G;
            color.B = color.B > 255 ? 255 : color.B;

            Color I = Color.FromRgb((byte)color.R, (byte)color.G, (byte)color.B);

            return I;
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
        
        private void AddPixel(double x, double y, Color color, double z)
        {
            if (zBuffer[(int)x, (int)y] < z)
            {
                pixels[(int)y, (int)x, 0] = color.B;
                pixels[(int)y, (int)x, 1] = color.G;
                pixels[(int)y, (int)x, 2] = color.R;
                pixels[(int)y, (int)x, 3] = 255;

                zBuffer[(int)x, (int)y] = z;
            }
        }
    }
}
