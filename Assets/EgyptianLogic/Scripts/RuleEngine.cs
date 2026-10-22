using System;
using System.Collections.Generic;
using UnityEngine;

public enum CompareOp { Lt, Le, Gt, Ge, Eq, Ne }
public enum RuleActionType { Set, Add, AddProduct }

[Serializable]
public class RuleCondition
{
    public string fact;
    public CompareOp op;
    public float value;
    [Tooltip("Opcional: si se escribe, se compara contra (valor de ese hecho + value).")]
    public string rhsFact;
}

[Serializable]
public class RuleAction
{
    public RuleActionType type;
    public string target;
    public float value;
    [Tooltip("Set: target = value + sourceFact. AddProduct: target += sourceFact * factB.")]
    public string sourceFact;
    public string factB;
}

[Serializable]
public class AdaptRule
{
    public string name;
    public int priority;
    public bool enabled = true;
    [TextArea(1, 3)] public string explanation;
    public List<RuleCondition> conditions = new List<RuleCondition>();
    public List<RuleAction> actions = new List<RuleAction>();
}

public class FactBase
{
    private readonly Dictionary<string, float> facts = new Dictionary<string, float>();
    public float Get(string key) => facts.TryGetValue(key, out var v) ? v : 0f;
    public void Set(string key, float value) => facts[key] = value;
    public bool Has(string key) => facts.ContainsKey(key);
}

public class RuleEngine
{
    private readonly HashSet<string> warned = new HashSet<string>();

    /// <summary>Encadenamiento hacia adelante. Devuelve los nombres de las reglas disparadas, en orden.</summary>
    public List<string> Run(List<AdaptRule> rules, FactBase facts, int maxFirings = 100)
    {
        var fired = new HashSet<AdaptRule>();
        var trace = new List<string>();

        for (int guard = 0; guard < maxFirings; guard++)
        {
            AdaptRule best = null;
            foreach (var r in rules)
            {
                if (r == null || !r.enabled || fired.Contains(r)) continue;
                if (!Matches(r, facts)) continue;
                if (best == null || r.priority > best.priority) best = r;   // empate: gana la primera de la lista
            }

            if (best == null) break;

            foreach (var a in best.actions) Apply(a, facts);
            fired.Add(best);
            trace.Add(best.name);
        }

        return trace;
    }

    private bool Matches(AdaptRule r, FactBase f)
    {
        foreach (var c in r.conditions)
            if (!Check(c, f)) return false;
        return true;
    }

    private bool Check(RuleCondition c, FactBase f)
    {
        WarnIfMissing(c.fact, f);
        WarnIfMissing(c.rhsFact, f);

        float lhs = f.Get(c.fact);
        float rhs = c.value + (string.IsNullOrEmpty(c.rhsFact) ? 0f : f.Get(c.rhsFact));

        switch (c.op)
        {
            case CompareOp.Lt: return lhs < rhs;
            case CompareOp.Le: return lhs <= rhs;
            case CompareOp.Gt: return lhs > rhs;
            case CompareOp.Ge: return lhs >= rhs;
            case CompareOp.Eq: return Mathf.Abs(lhs - rhs) < 0.0001f;
            case CompareOp.Ne: return Mathf.Abs(lhs - rhs) >= 0.0001f;
        }
        return false;
    }

    private void Apply(RuleAction a, FactBase f)
    {
        switch (a.type)
        {
            case RuleActionType.Set:
                f.Set(a.target, a.value + (string.IsNullOrEmpty(a.sourceFact) ? 0f : f.Get(a.sourceFact)));
                break;
            case RuleActionType.Add:
                f.Set(a.target, f.Get(a.target) + a.value);
                break;
            case RuleActionType.AddProduct:
                f.Set(a.target, f.Get(a.target) + f.Get(a.sourceFact) * f.Get(a.factB));
                break;
        }
    }

    // Un nombre de hecho mal escrito sería un error silencioso, así que se avisa
    private void WarnIfMissing(string fact, FactBase f)
    {
        if (string.IsNullOrEmpty(fact) || f.Has(fact) || !warned.Add(fact)) return;
        Debug.LogWarning($"[RuleEngine] El hecho '{fact}' no existe en la base de hechos (¿error de escritura en una regla?).");
    }
}