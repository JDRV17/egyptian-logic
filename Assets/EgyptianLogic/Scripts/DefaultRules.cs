using System.Collections.Generic;

public static class DefaultRules
{
    public static List<AdaptRule> Build()
    {
        var l = new List<AdaptRule>();

        // ---- Grupo 1 (prioridad 90): factor de aprendizaje K según cuántos retos van ----
        l.Add(R("K_reto1", 90, "Primer dato: la habilidad inicial es solo un supuesto, se reacciona fuerte.",
            new[] { C("stagesCompleted", CompareOp.Le, 1) }, new[] { Set("K", 1.0f) }));
        l.Add(R("K_reto2", 90, "Segundo dato.",
            new[] { C("stagesCompleted", CompareOp.Eq, 2) }, new[] { Set("K", 0.9f) }));
        l.Add(R("K_reto3", 90, "Tercer dato.",
            new[] { C("stagesCompleted", CompareOp.Eq, 3) }, new[] { Set("K", 0.8f) }));
        l.Add(R("K_reto4", 90, "Cuarto dato.",
            new[] { C("stagesCompleted", CompareOp.Eq, 4) }, new[] { Set("K", 0.7f) }));
        l.Add(R("K_reto5_o_mas", 90, "Con varios datos la estimación se estabiliza (K bajo).",
            new[] { C("stagesCompleted", CompareOp.Ge, 5) }, new[] { Set("K", 0.6f) }));

        // ---- Grupo 2 (80): ajuste de la evidencia ----
        l.Add(R("S_solucion_no_suma", 80, "Si necesitó la solución, un buen P no cuenta como evidencia de habilidad.",
            new[] { C("usedSolution", CompareOp.Eq, 1), C("delta", CompareOp.Gt, 0) },
            new[] { Set("delta", 0) }));

        // ---- Grupo 3 (70/65): actualización de la habilidad ----
        l.Add(R("U_actualizar_theta", 70, "θ ← θ + K·(P − P_esperado)",
            new[] { C("K", CompareOp.Gt, 0), C("thetaUpdated", CompareOp.Eq, 0) },
            new[] { AddProduct("theta", "K", "delta"), Set("thetaUpdated", 1) }));
        l.Add(R("U_techo_theta", 65, "θ no pasa de +2 (evita acumular ventaja imposible de revertir).",
            new[] { C("thetaUpdated", CompareOp.Eq, 1), C("theta", CompareOp.Gt, 2) }, new[] { Set("theta", 2) }));
        l.Add(R("U_piso_theta", 65, "θ no baja de −2.",
            new[] { C("thetaUpdated", CompareOp.Eq, 1), C("theta", CompareOp.Lt, -2) }, new[] { Set("theta", -2) }));

        // ---- Grupo 4 (50): selección de versión según θ ----
        l.Add(R("V_facil", 50, "θ < −0.5: la dificultad más cercana es V1.",
            new[] { C("thetaUpdated", CompareOp.Eq, 1), C("decided", CompareOp.Eq, 0), C("theta", CompareOp.Lt, -0.5f) },
            new[] { Set("nextVersion", 0), Set("decided", 1) }));
        l.Add(R("V_medio", 50, "−0.5 ≤ θ < 0.5: la dificultad más cercana es V2.",
            new[] { C("thetaUpdated", CompareOp.Eq, 1), C("decided", CompareOp.Eq, 0),
                    C("theta", CompareOp.Ge, -0.5f), C("theta", CompareOp.Lt, 0.5f) },
            new[] { Set("nextVersion", 1), Set("decided", 1) }));
        l.Add(R("V_dificil", 50, "θ ≥ 0.5: la dificultad más cercana es V3.",
            new[] { C("thetaUpdated", CompareOp.Eq, 1), C("decided", CompareOp.Eq, 0), C("theta", CompareOp.Ge, 0.5f) },
            new[] { Set("nextVersion", 2), Set("decided", 1) }));

        // ---- Grupo 5 (30/10): reglas de seguridad sobre la decisión ----
        l.Add(R("A_salto_max_arriba", 30, "No se sube más de una versión de golpe.",
            new[] { C("decided", CompareOp.Eq, 1), C("nextVersion", CompareOp.Ge, 2, "version") },
            new[] { Set("nextVersion", 1, "version") }));
        l.Add(R("A_salto_max_abajo", 30, "No se baja más de una versión de golpe.",
            new[] { C("decided", CompareOp.Eq, 1), C("nextVersion", CompareOp.Le, -2, "version") },
            new[] { Set("nextVersion", -1, "version") }));
        l.Add(R("A_pistas_no_sube", 30, "Con 2 o más pistas que cuentan, no se sube de versión.",
            new[] { C("decided", CompareOp.Eq, 1), C("hints", CompareOp.Ge, 2), C("nextVersion", CompareOp.Gt, 0, "version") },
            new[] { Set("nextVersion", 0, "version") }));
        l.Add(R("A_solucion_baja", 30, "Si necesitó la solución, la siguiente versión es más fácil.",
            new[] { C("decided", CompareOp.Eq, 1), C("usedSolution", CompareOp.Eq, 1), C("nextVersion", CompareOp.Gt, -1, "version") },
            new[] { Set("nextVersion", -1, "version") }));
        l.Add(R("A_piso_version", 10, "No existe una versión menor que V1.",
            new[] { C("decided", CompareOp.Eq, 1), C("nextVersion", CompareOp.Lt, 0) },
            new[] { Set("nextVersion", 0) }));

        return l;
    }

    private static RuleCondition C(string fact, CompareOp op, float value, string rhsFact = "") =>
        new RuleCondition { fact = fact, op = op, value = value, rhsFact = rhsFact };

    private static RuleAction Set(string target, float value, string source = "") =>
        new RuleAction { type = RuleActionType.Set, target = target, value = value, sourceFact = source };

    private static RuleAction AddProduct(string target, string a, string b) =>
        new RuleAction { type = RuleActionType.AddProduct, target = target, sourceFact = a, factB = b };

    private static AdaptRule R(string name, int priority, string why, RuleCondition[] conds, RuleAction[] acts) =>
        new AdaptRule
        {
            name = name,
            priority = priority,
            explanation = why,
            conditions = new List<RuleCondition>(conds),
            actions = new List<RuleAction>(acts)
        };
}