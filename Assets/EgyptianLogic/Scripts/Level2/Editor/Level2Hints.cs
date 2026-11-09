/// <summary>
/// Pistas propuestas para el Nivel 2. [reto][versión][pista]. Pista 1 = dónde ir, 2 y 3 = concepto, 4 = solución.
/// El constructor las copia al HintManager; después se editan en el Inspector.
/// </summary>
public static class Level2Hints
{
    private static readonly System.Collections.Generic.Dictionary<string, string> Names = new System.Collections.Generic.Dictionary<string, string>
    {
        { "2.1", "El Detective de Verdades" }, { "2.2", "Solo Tengo Esto" }, { "2.3", "El Ahorro de Compuertas" },
        { "2.4", "El Selector" }, { "2.5", "El Repartidor" }, { "2.6", "La Balanza Digital" },
    };

    // En 2.1 y 2.3 todo se responde en el panel; en los de circuitos, las compuertas están en el suelo
    private static string Where(string id) =>
        $"Sigue las antorchas encendidas por el comedor hasta «{Names[id]}», junto al muro. La luz ámbar sobre el reto indica " +
        (id == "2.1" || id == "2.3" ? "que está abierto." : "que está abierto, y las compuertas para resolverlo están en el suelo frente a él.");

    public static readonly string[][][] All =
    {
        // ---------------- 2.1 El Detective de Verdades
        new[]
        {
            new[]
            {
                Where("2.1"),
                "Evalúa por partes: para cada fila calcula OR(A,B) y NAND(A,B), y después únelos con el AND.",
                "OR vale 1 si alguna entrada es 1. NAND vale 0 solo cuando ambas son 1. El AND final necesita las dos partes en 1.",
                "Solución: S = 0, 1, 1, 0 para las filas 00, 01, 10, 11 (el circuito es un XOR)."
            },
            new[]
            {
                Where("2.1"),
                "Resuelve por etapas: primero NOT(A), luego el AND con B y al final el OR con C.",
                "Si C = 1 la salida siempre es 1. Si C = 0, solo importa A'·B (A apagado y B encendido).",
                "Solución (filas 000 a 111): 0, 1, 1, 1, 0, 1, 0, 1."
            },
            new[]
            {
                Where("2.1"),
                "Las filas con A = 1 y B = 1 son X: nunca ocurren y se aceptan con cualquier valor. Concéntrate en las otras 12.",
                "La salida es 1 solo si A y B son distintos y además C = 1 o D = 0.",
                "Solución: S = 1 en 0100, 0110, 0111, 1000, 1010 y 1011. Las filas 11xx son X. El resto es 0."
            },
        },

        // ---------------- 2.2 Solo Tengo Esto
        new[]
        {
            new[]
            {
                Where("2.2"),
                "Una NAND es un AND con la salida negada. ¿Cómo deshaces esa negación usando otra NAND?",
                "NOT(X) = NAND(X, X): conecta la misma señal a las dos entradas de una NAND.",
                "Solución: NAND1(A, B) y luego NAND2 con sus dos entradas desde la salida de NAND1 → S."
            },
            new[]
            {
                Where("2.2"),
                "Por De Morgan: X + Y = NAND(X', Y'). Aquí X = A·B y Y = C.",
                "(A·B)' es justo NAND(A, B). Solo falta C negado: NAND(C, C).",
                "Solución: NAND1(A, B); NAND2(C, C); NAND3(NAND1, NAND2) → S."
            },
            new[]
            {
                Where("2.2"),
                "Empieza por el acarreo: C = A·B se hace con dos NAND en serie, como en el reto fácil.",
                "El XOR con NAND: N1 = NAND(A,B); N2 = NAND(A,N1); N3 = NAND(B,N1); S = NAND(N2,N3).",
                "Solución: N1 = NAND(A,B); N2 = NAND(A,N1); N3 = NAND(B,N1); S = NAND(N2,N3); C = NAND(N1,N1)."
            },
        },

        // ---------------- 2.3 El Ahorro de Compuertas
        new[]
        {
            new[]
            {
                Where("2.3"),
                "Marca las celdas con 1 en el mapa y busca un grupo de 2 celdas vecinas.",
                "Dentro de un grupo, la variable que cambia de valor desaparece del término.",
                "Solución: F = A"
            },
            new[]
            {
                Where("2.3"),
                "Busca el grupo más grande: las columnas BC = 00 y 01 forman un bloque de 4 celdas con 1.",
                "Los bordes del mapa son vecinos: la celda A=1, BC=10 se agrupa con A=1, BC=00.",
                "Solución: F = B' + AC'"
            },
            new[]
            {
                Where("2.3"),
                "Las X pueden contar como 1 si te ayudan a formar grupos más grandes.",
                "La columna CD = 11 es un grupo de 4. Para cubrir la celda 0001, usa las X cercanas.",
                "Solución: F = A'B' + CD (también vale A'D + CD)."
            },
        },

        // ---------------- 2.4 El Selector
        new[]
        {
            new[]
            {
                Where("2.4"),
                "Cuando S = 0 debe pasar I0 a la salida; cuando S = 1 debe pasar I1.",
                "Usa NOT(S) para dejar pasar I0 con un AND, y S para dejar pasar I1 con otro AND. Júntalos con un OR.",
                "Solución: AND1(NOT S, I0); AND2(S, I1); OR(AND1, AND2) → Y. Luego prueba S = 0 y S = 1."
            },
            new[]
            {
                Where("2.4"),
                "Divide el problema: un MUX 2:1 elige entre I0 e I1, y otro entre I2 e I3.",
                "Los dos primeros MUX usan S0 (el bit menos significativo). El MUX final elige con S1.",
                "Solución: MUXa(I0, I1, S0); MUXb(I2, I3, S0); MUXc(MUXa, MUXb, S1) → Y. Prueba las 4 combinaciones."
            },
            new[]
            {
                Where("2.4"),
                "Un MUX 4:1 para I0–I3, otro para I4–I7, y un MUX 2:1 final que elige entre los dos.",
                "Los dos MUX 4:1 usan S0 y S1. El MUX 2:1 final usa S2.",
                "Solución: MUX4a(I0..I3, S0, S1); MUX4b(I4..I7, S0, S1); MUX2(MUX4a, MUX4b, S2) → Y."
            },
        },

        // ---------------- 2.5 El Repartidor
        new[]
        {
            new[]
            {
                Where("2.5"),
                "Cada salida corresponde a una dirección: Y0 = 00, Y1 = 01, Y2 = 10, Y3 = 11.",
                "Saca A1' y A0' con dos NOT. Cada salida es un AND de dos señales.",
                "Solución: Y0 = A1'·A0', Y1 = A1'·A0, Y2 = A1·A0', Y3 = A1·A0. Prueba las 4 direcciones."
            },
            new[]
            {
                Where("2.5"),
                "Es como el decodificador 2→4 pero con 3 bits: cada salida es un AND de 3 entradas.",
                "Saca A2', A1' y A0' con tres NOT. La salida Yk usa el bit negado donde k tiene un 0.",
                "Solución: Y0 = A2'·A1'·A0', Y1 = A2'·A1'·A0, ..., Y7 = A2·A1·A0. Prueba las 8 direcciones."
            },
            new[]
            {
                Where("2.5"),
                "Arma primero el decodificador 2→4 normal y después agrega el Enable.",
                "Usa E como tercera entrada de cada AND: si E = 0, ninguna salida puede encenderse.",
                "Solución: Yk = E · (término de la dirección k), por ejemplo Y0 = E·A1'·A0'. Prueba con E = 0 y E = 1."
            },
        },

        // ---------------- 2.6 La Balanza Digital
        new[]
        {
            new[]
            {
                Where("2.6"),
                "En binario, 1 + 1 = 10: la suma es 0 y el acarreo es 1.",
                "La suma vale 1 cuando A y B son distintos. El acarreo vale 1 cuando ambos son 1.",
                "Solución: S = XOR(A, B), C = AND(A, B). Prueba las 4 combinaciones."
            },
            new[]
            {
                Where("2.6"),
                "Cada sumador completo suma un bit: Ai, Bi y el acarreo que viene del bit anterior.",
                "El Cout de cada etapa va al Cin de la siguiente. El primer Cin va a GND (0).",
                "Solución: FA0(A0, B0, GND) → S0; FA1(A1, B1, Cout0) → S1; FA2 → S2; FA3 → S3, y su Cout → C4."
            },
            new[]
            {
                Where("2.6"),
                "Restar es sumar el complemento a 2: se invierte B y se suma 1.",
                "XOR(Bi, M) invierte B solo cuando M = 1, y M como primer Cin suma ese 1.",
                "Solución: Xi = XOR(Bi, M); sumadores en cadena con Cin0 = M; EQ = NOR(S0..S3); LT = NOT(C4); GT = AND(C4, NOT(EQ))."
            },
        },
    };
}
