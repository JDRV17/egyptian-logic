using System.Collections.Generic;
using System.Text;

/// <summary>
/// Minimización exacta de suma de productos (hasta 6 variables) con celdas "no importa".
/// Se usa en 2.3 para saber cuántos literales tiene la solución óptima y comparar con la del jugador.
/// </summary>
public static class LogicMinimizer
{
    public struct Result
    {
        public int literals;
        public int terms;
        public string expression;
    }

    /// <param name="names">Nombres de las variables; names[0] es el bit más significativo.</param>
    /// <param name="ones">ones[fila] = la función vale 1 (obligatorio cubrir).</param>
    /// <param name="dontCares">dontCares[fila] = puede valer cualquier cosa (puede ser null).</param>
    public static Result Minimize(IList<string> names, bool[] ones, bool[] dontCares = null)
    {
        int n = names.Count;
        int rows = 1 << n;

        var required = new List<int>();
        bool anyZero = false;
        for (int r = 0; r < rows; r++)
        {
            bool dc = dontCares != null && dontCares[r];
            if (ones[r] && !dc) required.Add(r);
            else if (!ones[r] && !dc) anyZero = true;
        }

        if (required.Count == 0) return new Result { literals = 0, terms = 0, expression = "0" };
        if (!anyZero) return new Result { literals = 0, terms = 1, expression = "1" };

        bool Allowed(int r) => ones[r] || (dontCares != null && dontCares[r]);

        // Un cubo es (care, value): care marca los bits que importan, value sus valores.
        var valid = new HashSet<long>();
        int full = rows - 1;
        for (int care = 0; care <= full; care++)
        {
            for (int value = 0; value <= full; value++)
            {
                if ((value & ~care) != 0) continue;
                bool ok = true;
                for (int r = 0; r < rows && ok; r++)
                    if ((r & care) == value && !Allowed(r)) ok = false;
                if (ok) valid.Add(Key(care, value));
            }
        }

        // Primos: no se pueden agrandar quitando un bit de "care"
        var primes = new List<(int care, int value)>();
        foreach (var k in valid)
        {
            int care = (int)(k >> 32), value = (int)(k & 0xffffffff);
            bool prime = true;
            for (int b = 0; b < n && prime; b++)
            {
                int bit = 1 << b;
                if ((care & bit) == 0) continue;
                if (valid.Contains(Key(care & ~bit, value & ~bit))) prime = false;
            }
            if (!prime) continue;

            bool coversRequired = false;
            foreach (var r in required) if ((r & care) == value) { coversRequired = true; break; }
            if (coversRequired) primes.Add((care, value));
        }

        // Búsqueda exacta de la cobertura con menos literales (desempate: menos términos)
        var chosen = new List<int>();
        List<int> best = null;
        int bestLits = int.MaxValue, bestTerms = int.MaxValue;

        void Search(int lits)
        {
            if (lits > bestLits) return;

            int pick = -1, pickOptions = int.MaxValue;
            foreach (var r in required)
            {
                bool covered = false;
                foreach (var c in chosen) if ((r & primes[c].care) == primes[c].value) { covered = true; break; }
                if (covered) continue;

                int options = 0;
                for (int p = 0; p < primes.Count; p++) if ((r & primes[p].care) == primes[p].value) options++;
                if (options < pickOptions) { pickOptions = options; pick = r; }
            }

            if (pick < 0)
            {
                if (lits < bestLits || (lits == bestLits && chosen.Count < bestTerms))
                {
                    bestLits = lits;
                    bestTerms = chosen.Count;
                    best = new List<int>(chosen);
                }
                return;
            }

            for (int p = 0; p < primes.Count; p++)
            {
                if ((pick & primes[p].care) != primes[p].value) continue;
                chosen.Add(p);
                Search(lits + PopCount(primes[p].care));
                chosen.RemoveAt(chosen.Count - 1);
            }
        }

        Search(0);

        var sb = new StringBuilder();
        foreach (var p in best)
        {
            if (sb.Length > 0) sb.Append(" + ");
            sb.Append(CubeToString(names, primes[p].care, primes[p].value));
        }

        return new Result { literals = bestLits, terms = bestTerms, expression = sb.ToString() };
    }

    private static long Key(int care, int value) => ((long)care << 32) | (uint)value;

    private static int PopCount(int x)
    {
        int c = 0;
        while (x != 0) { c += x & 1; x >>= 1; }
        return c;
    }

    private static string CubeToString(IList<string> names, int care, int value)
    {
        int n = names.Count;
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            int bit = 1 << (n - 1 - i);
            if ((care & bit) == 0) continue;
            sb.Append(names[i]);
            if ((value & bit) == 0) sb.Append('\'');
        }
        return sb.Length == 0 ? "1" : sb.ToString();
    }
}
