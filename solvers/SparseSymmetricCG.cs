﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g3
{
    // ported from WildMagic5 Wm5LinearSystem.cpp
    public class SparseSymmetricCG
    {
        public Action<double[], double[]> MultiplyF;
        public Action<double[], double[]> PreconditionMultiplyF;

        // B is not modified!
        public double[] B;

        // X will be used as initial guess if non-null and UseXAsInitialGuess is true
        public double[] X;
        public bool UseXAsInitialGuess = true;

        public int MaxIterations = 1024;

        // internal
        double[] R, P, W, Z;




        public bool Solve()
        {
            int size = B.Length;

            // Based on the algorithm in "Matrix Computations" by Golum and Van Loan.
            R = new double[size];
            P = new double[size];
            W = new double[size];

            if ( X == null || UseXAsInitialGuess == false ) {
                if ( X == null )
                    X = new double[size];
                Array.Clear(X, 0, X.Length);
                Array.Copy(B, R, B.Length);
            } else {
                // hopefully is X is a decent initialization...
                InitializeR(R);
            }

            // The first iteration.
            double rho0 = BufferUtil.Dot(R, R);
            Array.Copy(R, P, R.Length);

            MultiplyF(P, W);

            double alpha = rho0 / BufferUtil.Dot(P, W);
            BufferUtil.MultiplyAdd(X, alpha, P);
            BufferUtil.MultiplyAdd(R, -alpha, W);
            double rho1 = BufferUtil.Dot(R, R);

            // [RMS] these were inside loop but they are constant!
            double norm = BufferUtil.Dot(B, B);
            double root1 = Math.Sqrt(norm);

            // The remaining iterations.
            int iter;
            for (iter = 1; iter < MaxIterations; ++iter) {
                double root0 = Math.Sqrt(rho1);
                if (root0 <= MathUtil.ZeroTolerance * root1) {
                    break;
                }

                double beta = rho1 / rho0;
                UpdateP(P, beta, R);

                MultiplyF(P, W);

                alpha = rho1 / BufferUtil.Dot(P, W);
                BufferUtil.MultiplyAdd(X, alpha, P);
                BufferUtil.MultiplyAdd(R, -alpha, W);
                rho0 = rho1;
                rho1 = BufferUtil.Dot(R, R);
            }


            System.Console.WriteLine("{0} iterations", iter);
            return iter < MaxIterations;
        }


        void UpdateP(double[] P, double beta, double[] R)
        {
            for (int i = 0; i < P.Length; ++i)
                P[i] = R[i] + beta * P[i];
        }


        void InitializeR(double[] R)
        {
            // R = B - A*X
            MultiplyF(X, R);
            for (int i = 0; i < X.Length; ++i)
                R[i] = B[i] - R[i];
        }







        public bool SolvePreconditioned()
        {
            int size = B.Length;

            // Based on the algorithm in "Matrix Computations" by Golum and Van Loan.
            // [RMS] added preconditioner...

            R = new double[size];
            P = new double[size];
            W = new double[size];
            Z = new double[size];

            if ( X == null || UseXAsInitialGuess == false ) {
                if ( X == null )
                    X = new double[size];
                Array.Clear(X, 0, X.Length);
                Array.Copy(B, R, B.Length);
            } else {
                // hopefully is X is a decent initialization...
                InitializeR(R);
            }

            // The first iteration.
            Array.Copy(R, P, R.Length);

            MultiplyF(P, W);
            PreconditionMultiplyF(R, Z);

            double rho0 = BufferUtil.Dot(Z, R);
            double alpha = rho0 / BufferUtil.Dot(P, W);
            BufferUtil.MultiplyAdd(X, alpha, P);
            BufferUtil.MultiplyAdd(R, -alpha, W);
            double rho1 = BufferUtil.Dot(Z, R);

            // [RMS] these were inside loop but they are constant!
            double norm = BufferUtil.Dot(B, B);
            double root1 = Math.Sqrt(norm);

            // The remaining iterations.
            int iter = 0;
            for (iter = 1; iter < MaxIterations; ++iter) {
                double root0 = Math.Sqrt(rho1);
                if (root0 <= MathUtil.ZeroTolerance * root1) {
                    break;
                }

                double beta = rho1 / rho0;
                UpdateP(P, beta, Z);

                MultiplyF(P, W);

                alpha = rho1 / BufferUtil.Dot(P, W);
                BufferUtil.MultiplyAdd(X, alpha, P);
                BufferUtil.MultiplyAdd(R, -alpha, W);
                PreconditionMultiplyF(R, Z);
                rho0 = rho1;
                rho1 = BufferUtil.Dot(Z, R);
            }


            System.Console.WriteLine("{0} iterations", iter);
            return iter < MaxIterations;
        }

    }
}
