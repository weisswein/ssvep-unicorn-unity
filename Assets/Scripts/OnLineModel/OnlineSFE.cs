using System.Collections.Generic;
using UnityEngine;

public static class OnlineSFE
{
    public static float[] ExtractFeatures(float[,] eeg, float fs, List<float> targetFreqs, int harmonics = 2)
    {
        int chCount = eeg.GetLength(0);
        int n = eeg.GetLength(1);

        if (chCount <= 0 || n <= 2)
            return new float[targetFreqs.Count];

        // EEG: [samples, channels]
        float[,] X = Transpose(eeg);

        float[] scores = new float[targetFreqs.Count];

        for (int i = 0; i < targetFreqs.Count; i++)
        {
            float[,] Y = BuildReferenceSignals(targetFreqs[i], fs, n, harmonics);
            scores[i] = ComputeCcaMaxCorrelation(X, Y);
        }

        return scores;
    }

    public static int PredictClass(float[,] eeg, float fs, List<float> targetFreqs, int harmonics = 2)
    {
        float[] scores = ExtractFeatures(eeg, fs, targetFreqs, harmonics);

        int bestIdx = -1;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private static float[,] BuildReferenceSignals(float freq, float fs, int samples, int harmonics)
    {
        int cols = harmonics * 2;
        float[,] Y = new float[samples, cols];

        for (int t = 0; t < samples; t++)
        {
            float time = t / fs;

            for (int h = 1; h <= harmonics; h++)
            {
                float angle = 2f * Mathf.PI * freq * h * time;
                Y[t, (h - 1) * 2] = Mathf.Sin(angle);
                Y[t, (h - 1) * 2 + 1] = Mathf.Cos(angle);
            }
        }

        return Y;
    }

    private static float ComputeCcaMaxCorrelation(float[,] X, float[,] Y)
    {
        // X: [samples, px], Y: [samples, py]
        float[,] Xc = CenterColumns(X);
        float[,] Yc = CenterColumns(Y);

        float[,] Sxx = Covariance(Xc);
        float[,] Syy = Covariance(Yc);
        float[,] Sxy = CrossCovariance(Xc, Yc);
        float[,] Syx = Transpose(Sxy);

        float[,] invSxx = InverseRegularized(Sxx, 1e-4f);
        float[,] invSyy = InverseRegularized(Syy, 1e-4f);

        if (invSxx == null || invSyy == null)
            return 0f;

        // M = inv(Sxx) * Sxy * inv(Syy) * Syx
        float[,] M = Multiply(Multiply(Multiply(invSxx, Sxy), invSyy), Syx);

        float lambdaMax = PowerIterationLargestEigenvalue(M, 50);
        if (lambdaMax < 0f) lambdaMax = 0f;

        return Mathf.Sqrt(lambdaMax);
    }

    private static float[,] CenterColumns(float[,] A)
    {
        int rows = A.GetLength(0);
        int cols = A.GetLength(1);
        float[,] C = new float[rows, cols];

        for (int j = 0; j < cols; j++)
        {
            float mean = 0f;
            for (int i = 0; i < rows; i++)
                mean += A[i, j];
            mean /= rows;

            for (int i = 0; i < rows; i++)
                C[i, j] = A[i, j] - mean;
        }

        return C;
    }

    private static float[,] Covariance(float[,] A)
    {
        int rows = A.GetLength(0);
        float scale = 1f / Mathf.Max(1, rows - 1);

        float[,] At = Transpose(A);
        float[,] C = Multiply(At, A);

        int n = C.GetLength(0);
        int m = C.GetLength(1);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
                C[i, j] *= scale;
        }

        return C;
    }

    private static float[,] CrossCovariance(float[,] A, float[,] B)
    {
        int rows = A.GetLength(0);
        float scale = 1f / Mathf.Max(1, rows - 1);

        float[,] At = Transpose(A);
        float[,] C = Multiply(At, B);

        int n = C.GetLength(0);
        int m = C.GetLength(1);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
                C[i, j] *= scale;
        }

        return C;
    }

    private static float[,] Transpose(float[,] A)
    {
        int rows = A.GetLength(0);
        int cols = A.GetLength(1);
        float[,] T = new float[cols, rows];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                T[j, i] = A[i, j];

        return T;
    }

    private static float[,] Multiply(float[,] A, float[,] B)
    {
        int aRows = A.GetLength(0);
        int aCols = A.GetLength(1);
        int bRows = B.GetLength(0);
        int bCols = B.GetLength(1);

        if (aCols != bRows)
            return null;

        float[,] C = new float[aRows, bCols];

        for (int i = 0; i < aRows; i++)
        {
            for (int k = 0; k < aCols; k++)
            {
                float a = A[i, k];
                for (int j = 0; j < bCols; j++)
                    C[i, j] += a * B[k, j];
            }
        }

        return C;
    }

    private static float[,] InverseRegularized(float[,] A, float lambda)
    {
        int n = A.GetLength(0);
        if (n != A.GetLength(1))
            return null;

        float[,] M = new float[n, n];
        float[,] I = new float[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                M[i, j] = A[i, j];

            M[i, i] += lambda;
            I[i, i] = 1f;
        }

        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            float maxAbs = Mathf.Abs(M[pivot, col]);

            for (int r = col + 1; r < n; r++)
            {
                float v = Mathf.Abs(M[r, col]);
                if (v > maxAbs)
                {
                    maxAbs = v;
                    pivot = r;
                }
            }

            if (maxAbs < 1e-8f)
                return null;

            if (pivot != col)
            {
                SwapRows(M, pivot, col);
                SwapRows(I, pivot, col);
            }

            float diag = M[col, col];
            for (int j = 0; j < n; j++)
            {
                M[col, j] /= diag;
                I[col, j] /= diag;
            }

            for (int r = 0; r < n; r++)
            {
                if (r == col) continue;

                float factor = M[r, col];
                for (int j = 0; j < n; j++)
                {
                    M[r, j] -= factor * M[col, j];
                    I[r, j] -= factor * I[col, j];
                }
            }
        }

        return I;
    }

    private static void SwapRows(float[,] A, int r1, int r2)
    {
        int cols = A.GetLength(1);
        for (int j = 0; j < cols; j++)
        {
            float tmp = A[r1, j];
            A[r1, j] = A[r2, j];
            A[r2, j] = tmp;
        }
    }

    private static float PowerIterationLargestEigenvalue(float[,] A, int iterations)
    {
        int n = A.GetLength(0);
        if (n != A.GetLength(1))
            return 0f;

        float[] b = new float[n];
        for (int i = 0; i < n; i++)
            b[i] = 1f / Mathf.Sqrt(n);

        for (int iter = 0; iter < iterations; iter++)
        {
            float[] Ab = Multiply(A, b);
            float norm = VectorNorm(Ab);
            if (norm < 1e-8f)
                return 0f;

            for (int i = 0; i < n; i++)
                b[i] = Ab[i] / norm;
        }

        float[] AbFinal = Multiply(A, b);
        float num = Dot(b, AbFinal);
        float den = Dot(b, b) + 1e-8f;

        return num / den;
    }

    private static float[] Multiply(float[,] A, float[] x)
    {
        int rows = A.GetLength(0);
        int cols = A.GetLength(1);

        if (cols != x.Length)
            return null;

        float[] y = new float[rows];
        for (int i = 0; i < rows; i++)
        {
            float sum = 0f;
            for (int j = 0; j < cols; j++)
                sum += A[i, j] * x[j];
            y[i] = sum;
        }

        return y;
    }

    private static float Dot(float[] a, float[] b)
    {
        float s = 0f;
        for (int i = 0; i < a.Length; i++)
            s += a[i] * b[i];
        return s;
    }

    private static float VectorNorm(float[] x)
    {
        return Mathf.Sqrt(Dot(x, x));
    }
}