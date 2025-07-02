// OpenSimplex2 noise core - simplified 2D, seeded
// Credit: Kurt Spencer (original); simplified version here
#include <cmath>
#include <cstdint>

struct OpenSimplex2 {
    int64_t seed;

    OpenSimplex2(int64_t s) : seed(s) {}

    static double grad(int64_t hash, double x, double y) {
        int h = hash & 7;
        double u = h < 4 ? x : y;
        double v = h < 4 ? y : x;
        return ((h & 1) ? -u : u) + ((h & 2) ? -2.0 * v : 2.0 * v);
    }

    int64_t hash(int x, int y) const {
        int64_t h = seed;
        h ^= x * 0x27d4eb2d;
        h ^= y * 0x165667b1;
        h *= h * h * 60493;
        return h >> 13;
    }

    /// @brief returns a value between -1 and 1
    /// @param x 
    /// @param y 
    /// @return 
    double noise(double x, double y) const {
        const double stretch = (x + y) * 0.5 * (std::sqrt(3.0) - 1.0);
        int i = (int)std::floor(x + stretch);
        int j = (int)std::floor(y + stretch);

        const double squish = (i + j) * (3.0 - std::sqrt(3.0)) / 6.0;
        double xi = x - (i - squish);
        double yi = y - (j - squish);

        int i1 = xi > yi ? 1 : 0;
        int j1 = xi > yi ? 0 : 1;

        double n0, n1, n2;

        auto extrapolate = [&](int dx, int dy, double x, double y) {
            return grad(hash(i + dx, j + dy), x, y);
        };

        double x0 = xi;
        double y0 = yi;
        double t0 = 0.5 - x0 * x0 - y0 * y0;
        n0 = (t0 < 0) ? 0.0 : (t0 *= t0, t0 * t0 * extrapolate(0, 0, x0, y0));

        double x1 = xi - i1 + (3.0 - std::sqrt(3.0)) / 6.0;
        double y1 = yi - j1 + (3.0 - std::sqrt(3.0)) / 6.0;
        double t1 = 0.5 - x1 * x1 - y1 * y1;
        n1 = (t1 < 0) ? 0.0 : (t1 *= t1, t1 * t1 * extrapolate(i1, j1, x1, y1));

        double x2 = xi - 1.0 + 2.0 * (3.0 - std::sqrt(3.0)) / 6.0;
        double y2 = yi - 1.0 + 2.0 * (3.0 - std::sqrt(3.0)) / 6.0;
        double t2 = 0.5 - x2 * x2 - y2 * y2;
        n2 = (t2 < 0) ? 0.0 : (t2 *= t2, t2 * t2 * extrapolate(1, 1, x2, y2));
        
        return std::clamp(70.0 * (n0 + n1 + n2), -1.0, 1.0); // range ≈ [-1, 1]
    }
};
