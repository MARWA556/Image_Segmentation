using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace ImageTemplate
{
    public class RegionSegmentation
    {
        public class SegmentationResult
        {
            public RGBPixel[,] ColoredRegions;
            public int RegionCount;
            public List<int> RegionSizes;
        }


        private static readonly int[] dx = { 1, 1, 0, -1 };
        private static readonly int[] dy = { 0, 1, 1, 1 };
        //private static readonly int[] dx = { 1, 0};
        //private static readonly int[] dy = { 0, 1};
        public static long Timeelapsed { get; private set; }

        private struct Edge
        {
            public int a, b;
            public double weight;
        }

        private class DisjointSet
        {
            public int[] parent, size;
            public double[] internalDiff;
            public DisjointSet(int n)
            {
                parent = new int[n];
                size = new int[n];
                internalDiff = new double[n];
                for (int i = 0; i < n; i++)
                {
                    parent[i] = i;
                    size[i] = 1;
                    internalDiff[i] = 0;
                }
            }
            public int Find(int x)
            {
                if (parent[x] != x)
                    parent[x] = Find(parent[x]);
                return parent[x];
            }
            public void Union(int x, int y, double edgeWeight, double k)
            {
                int rx = Find(x);
                int ry = Find(y);
                if (rx == ry) return;
                if (size[rx] < size[ry])
                {
                    // Swap rx and ry to ensure rx always points to the root of the larger (or equal-sized) set.
                    int temp = rx;
                    rx = ry;
                    ry = temp;
                }
                parent[ry] = rx;      // Make the root of the smaller set (ry) point to the root of the larger set (rx).
                size[rx] += size[ry]; // Update the size of the new, merged set (only at the new root, rx).

                // Update internalDiff for the new merged component.
                // The internal difference of the new component is the maximum of:
                // 1. The internal difference of the two components being merged (internalDiff[rx] and internalDiff[ry]).
                // 2. The weight of the edge that caused this merge (edgeWeight).
                internalDiff[rx] = Math.Max(Math.Max(internalDiff[rx], internalDiff[ry]), edgeWeight);
            }
            public int GetSize(int x) => size[Find(x)];
            public double GetInternalDiff(int x) => internalDiff[Find(x)];
        }


        public class SegmentOneChannelResult
        {
            public int[,] Labels;
            public int RegionCount;
        }

        public static SegmentOneChannelResult SegmentSingleChannel(byte[,] channel, int k)
        {
            int height = channel.GetLength(0);
            int width = channel.GetLength(1);
            int n = height * width;
            List<Edge> edges = new List<Edge>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx1 = y * width + x;
                    for (int d = 0; d < GetDx(width, height).Length; d++)
                    {
                        int nx = x + GetDx(width, height)[d];
                        int ny = y + GetDy(width, height)[d];
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            int idx2 = ny * width + nx;
                            double w = Math.Abs(channel[y, x] - channel[ny, nx]);
                            edges.Add(new Edge { a = idx1, b = idx2, weight = w });
                        }
                    }
                }
            }

            edges.Sort((a, b) => a.weight.CompareTo(b.weight));
            DisjointSet ds = new DisjointSet(n);
            const double INTERNAL_DIFF_SENSITIVITY_FACTOR = 0.9999;

            foreach (var edge in edges)
            {
                int ra = ds.Find(edge.a);
                int rb = ds.Find(edge.b);
                if (ra == rb) continue;
                //double MIntA = ds.GetInternalDiff(ra) + (double)(k / ds.GetSize(ra));
                //double MIntB = ds.GetInternalDiff(rb) + (double)(k / ds.GetSize(rb));
                double MIntA = (ds.GetInternalDiff(ra) * INTERNAL_DIFF_SENSITIVITY_FACTOR) + (double)k / ds.GetSize(ra);
                double MIntB = (ds.GetInternalDiff(rb) * INTERNAL_DIFF_SENSITIVITY_FACTOR) + (double)k / ds.GetSize(rb);
                if (edge.weight <= Math.Min(MIntA, MIntB))
                {
                    ds.Union(ra, rb, edge.weight, k);
                }
            }

            int[,] labels = new int[height, width];
            Dictionary<int, int> labelMap = new Dictionary<int, int>();
            int currentLabel = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int root = ds.Find(idx);
                    if (!labelMap.ContainsKey(root))
                    {
                        currentLabel++;
                        labelMap[root] = currentLabel;
                    }
                    labels[y, x] = labelMap[root];
                }
            }

            return new SegmentOneChannelResult
            {
                Labels = labels,
                RegionCount = currentLabel
            };
        }

        public static RegionSegmentation.SegmentationResult SegmentByChannels(RGBPixel[,] image, int k)
        {
            Stopwatch timer = Stopwatch.StartNew();

            int height = image.GetLength(0);
            int width = image.GetLength(1);

            byte[,] red = new byte[height, width];
            byte[,] green = new byte[height, width];
            byte[,] blue = new byte[height, width];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    red[y, x] = image[y, x].red;
                    green[y, x] = image[y, x].green;
                    blue[y, x] = image[y, x].blue;
                }

            var rResult = SegmentSingleChannel(red, k);
            var gResult = SegmentSingleChannel(green, k);
            var bResult = SegmentSingleChannel(blue, k);

            Dictionary<string, int> labelMap = new Dictionary<string, int>();
            int[,] finalLabels = new int[height, width];
            int currentLabel = 0;
            Dictionary<int, int> regionSizes = new Dictionary<int, int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    string key = $"{rResult.Labels[y, x]}_{gResult.Labels[y, x]}_{bResult.Labels[y, x]}";
                    if (!labelMap.ContainsKey(key))
                    {
                        currentLabel++;
                        labelMap[key] = currentLabel;
                        regionSizes[currentLabel] = 0;
                    }

                    finalLabels[y, x] = labelMap[key];
                    regionSizes[labelMap[key]]++;
                }
            }
            Random rand = new Random(42);
            Dictionary<int, RGBPixel> regionColors = new Dictionary<int, RGBPixel>();
            foreach (var label in regionSizes.Keys)
            {
                regionColors[label] = new RGBPixel
                {
                    red = (byte)rand.Next(50, 230),
                    green = (byte)rand.Next(50, 230),
                    blue = (byte)rand.Next(50, 230)
                };
            }
            
            RGBPixel[,] output = new RGBPixel[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int label = finalLabels[y, x];
                    output[y, x] = regionColors[label];
                }
            }
            timer.Stop();

            long time = timer.ElapsedMilliseconds;
            Timeelapsed = time;

            return new RegionSegmentation.SegmentationResult
            {
                ColoredRegions = output,
                RegionCount = regionSizes.Count,
                RegionSizes = regionSizes.Values.OrderByDescending(s => s).ToList()
            };
        }
        private static int[] GetDx(int width, int height)
        {
            // Example: Use 4-connectivity for large images, 4-connectivity for small images
            if (width * height < 2970 * 1980)
                return new int[] { 1, 1, 0, -1 }; // 4 directions
            else
                return new int[] { 1, 0 }; // 4 directions
        }

        private static int[] GetDy(int width, int height)
        {
            if (width * height < 2970 * 1980)
                return new int[] { 0, 1, 1, 1 };
            else
                return new int[] { 0, 1 };
        }

    }
}