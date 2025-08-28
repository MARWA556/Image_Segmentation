# Image Segmentation Algorithms Project

This project implements a **graph-based image segmentation technique**. It converts an image into a weighted undirected graph and segments it into distinct regions based on internal consistency and boundary evidence. The approach follows the method proposed in **“Efficient Graph-Based Image Segmentation”** by *Felzenszwalb and Huttenlocher*.

---

## 📌 Project Features

- Graph representation of images using **8-connected pixel neighborhoods**
- Edge weights based on pixel intensity or RGB channel differences
- Efficient segmentation using **Kruskal’s Algorithm** combined with **Breadth-First Search (BFS)** for connected components
- Visualization of segmented regions with distinct colors
- Output of **number and size of regions** to a text file
- Supports both **grayscale** and **color images**
- Optional **Gaussian filter** for noise reduction

---

## 🛠 Technologies & Skills

- **Programming Language:** C#
- **Domain:** Image Processing, Graph-Based Algorithms
- **Algorithms:** Kruskal’s Algorithm, BFS, Gaussian Filter
- **Skills Applied:** Algorithms · Image Processing · Graph Algorithms · C#
