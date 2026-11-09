using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Acabados del comedor del Nivel 2: piso de arena, muros con jeroglíficos, sillas doradas de respaldo ovalado y
/// mesas de madera con incrustaciones de nácar. Las mallas y texturas se generan por código y se guardan en
/// Materials/Level2/Mallas y Materials/Level2/Texturas, así cada silla es un solo objeto con dos materiales.
/// </summary>
public static partial class Level2Builder
{
    private const string TexturesFolder = "Assets/EgyptianLogic/Materials/Textures";
    /// <summary>Metros de muro que ocupa una copia de la imagen de jeroglíficos.</summary>
    private const float GlyphTileWidth = 3.6f;
    /// <summary>Metros de mesa que ocupa una repetición del patrón de estrellas.</summary>
    private const float InlayTile = 0.5f;
    /// <summary>Metros de piso que ocupa (más o menos) una copia de la imagen de arena.</summary>
    private const float SandTileWidth = 1.6f;

    // Se generan una vez por compilación de scripts; si cambia este código, se regeneran al volver a construir
    private static Mesh chairMesh, tableLegMesh;
    private static Texture2D inlayTex, bandTex;

    // ================================================================ Muros

    /// <summary>
    /// Material de muro con la imagen de jeroglíficos repetida a escala real (sin estirarse).
    /// Busca en Materials/Textures una imagen cuyo nombre empiece por "Muro_jer"; si no está, el muro queda liso.
    /// </summary>
    private static Material WallMaterial(float width, float height)
    {
        var tex = WallGlyphs();
        if (tex == null) return Mat("L2_Muro", new Color(0.74f, 0.62f, 0.44f));

        float tileH = GlyphTileWidth * tex.height / tex.width;
        var tiling = new Vector2(width / GlyphTileWidth, height / tileH);
        string name = "L2_MuroJeroglificos_" + Num(width) + "x" + Num(height);
        return TexturedMat(name, tex, tiling, new Color(0.95f, 0.92f, 0.88f), 0.12f, 0f);
    }

    // Espejo a lo ancho: los jeroglíficos se escriben en ambos sentidos y así no se nota la unión entre copias
    private static Texture2D WallGlyphs() => FindTexture("muro_jer", TextureWrapMode.Mirror, TextureWrapMode.Repeat);

    // ================================================================ Piso

    /// <summary>
    /// Material de piso con la imagen de arena de Materials/Textures (nombre que empiece por "Suelo_Arena").
    /// Va en espejo en ambos sentidos y con un número par de copias por baldosa, así la arena sigue sin cortes
    /// de una baldosa a la siguiente. Sin la imagen, el piso queda liso.
    /// </summary>
    private static Material FloorMaterial(float sizeX, float sizeZ)
    {
        var tex = FindTexture("suelo_arena", TextureWrapMode.Mirror, TextureWrapMode.Mirror);
        if (tex == null) return Mat("L2_Piso", new Color(0.66f, 0.55f, 0.38f));

        float copyH = SandTileWidth * tex.height / tex.width;
        int Copies(float size, float copy) => Mathf.Max(2, 2 * Mathf.RoundToInt(size / (2f * copy)));
        var tiling = new Vector2(Copies(sizeX, SandTileWidth), Copies(sizeZ, copyH));
        return TexturedMat("L2_PisoArena_" + Num(tiling.x) + "x" + Num(tiling.y), tex, tiling, new Color(0.95f, 0.92f, 0.88f), 0.08f, 0f);
    }

    /// <summary>Primera imagen de Materials/Textures cuyo nombre empieza por <paramref name="prefix"/> (sin importar mayúsculas).</summary>
    private static Texture2D FindTexture(string prefix, TextureWrapMode wrapU, TextureWrapMode wrapV)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(path).ToLowerInvariant().StartsWith(prefix)) continue;
            ConfigureTexture(path, wrapU, wrapV);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }

    // ================================================================ Sillas

    private static Material GoldMat() => MetalMat("L2_Oro", new Color(1f, 0.76f, 0.33f), 0.65f, 0.72f);

    /// <summary>Silla dorada con respaldo ovalado calado, patas torneadas y cojín crema (submalla 0 = oro, 1 = tela).</summary>
    private static Mesh ChairMesh()
    {
        if (chairMesh != null) return chairMesh;

        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var gold = new List<CombineInstance>();
        var cloth = new List<CombineInstance>();
        void Add(List<CombineInstance> list, Mesh m, Matrix4x4 t) => list.Add(new CombineInstance { mesh = m, transform = t });

        // Marco del asiento y cojín
        Add(gold, cube, Matrix4x4.TRS(new Vector3(0f, 0.445f, 0f), Quaternion.identity, new Vector3(0.46f, 0.05f, 0.44f)));
        Add(cloth, cube, Matrix4x4.TRS(new Vector3(0f, 0.5f, -0.005f), Quaternion.identity, new Vector3(0.43f, 0.065f, 0.41f)));

        // Patas delanteras torneadas: bolas y anillos sobre un fuste que se adelgaza hacia el pie
        var leg = Tube(new[]
        {
            new Vector2(0f, 0f), new Vector2(0.02f, 0f), new Vector2(0.025f, 0.012f), new Vector2(0.02f, 0.026f),
            new Vector2(0.014f, 0.036f), new Vector2(0.02f, 0.046f), new Vector2(0.014f, 0.058f),
            new Vector2(0.015f, 0.08f), new Vector2(0.022f, 0.28f), new Vector2(0.03f, 0.292f), new Vector2(0.034f, 0.305f),
            new Vector2(0.026f, 0.318f), new Vector2(0.021f, 0.328f), new Vector2(0.035f, 0.345f), new Vector2(0.043f, 0.368f),
            new Vector2(0.04f, 0.39f), new Vector2(0.028f, 0.402f), new Vector2(0.027f, 0.42f), new Vector2(0f, 0.42f),
        }, 14);
        foreach (float x in new[] { -0.2f, 0.2f })
            Add(gold, leg, Matrix4x4.Translate(new Vector3(x, 0f, -0.19f)));

        // Patas traseras en sable y los dos parales que sostienen el respaldo
        foreach (float s in new[] { -1f, 1f })
        {
            Add(gold, cube, Bar(new Vector3(s * 0.2f, 0f, 0.29f), new Vector3(s * 0.185f, 0.45f, 0.2f), 0.032f));
            Add(gold, cube, Bar(new Vector3(s * 0.185f, 0.44f, 0.2f), new Vector3(s * 0.15f, 0.695f, 0.248f), 0.03f));
        }

        // Respaldo: aro ovalado grueso, inclinado 10° hacia atrás, con óvalos calados adentro
        var back = Matrix4x4.TRS(new Vector3(0f, 0.886f, 0.282f), Quaternion.Euler(10f, 0f, 0f), Vector3.one);
        Add(gold, EllipseRing(0.22f, 0.27f, 0.05f, 0.025f, 48), back);
        const float innerBottom = -0.22f;
        foreach (var o in new[] { new Vector2(0.12f, 0.175f), new Vector2(0.085f, 0.125f), new Vector2(0.05f, 0.075f) })
            Add(gold, EllipseRing(o.x, o.y, 0.012f, 0.014f, 40), back * Matrix4x4.Translate(new Vector3(0f, innerBottom + o.y, 0f)));
        // Dos óvalos anchos que cruzan a los anteriores, como el lazo de la foto
        Add(gold, EllipseRing(0.165f, 0.07f, 0.012f, 0.014f, 40), back * Matrix4x4.Translate(new Vector3(0f, -0.12f, 0f)));
        Add(gold, EllipseRing(0.13f, 0.045f, 0.012f, 0.014f, 40), back * Matrix4x4.Translate(new Vector3(0f, -0.16f, 0f)));

        var mesh = new Mesh { name = "L2_SillaDorada" };
        mesh.CombineMeshes(new[]
        {
            new CombineInstance { mesh = Combine(gold), transform = Matrix4x4.identity },
            new CombineInstance { mesh = Combine(cloth), transform = Matrix4x4.identity },
        }, false, true);
        mesh.RecalculateBounds();
        return chairMesh = SaveMesh(mesh);
    }

    // ================================================================ Mesas

    private static Material InlayWood() => Mat("L2_MaderaTaracea", new Color(0.3f, 0.11f, 0.07f));

    /// <summary>Tablero de madera rojiza con el campo de estrellas en nácar y una banda a cuadros alrededor.</summary>
    private static void InlaidTabletop(Transform table, float length, float depth)
    {
        var wood = InlayWood();
        Prim(PrimitiveType.Cube, table, "Tablero", new Vector3(0f, 0.74f, 0f), new Vector3(length, 0.07f, depth), wood, true);
        Prim(PrimitiveType.Cube, table, "Faldon", new Vector3(0f, 0.655f, 0f), new Vector3(length - 0.1f, 0.1f, depth - 0.1f), wood);

        float fieldL = length - 0.24f, fieldD = depth - 0.24f;
        Prim(PrimitiveType.Cube, table, "Incrustacion", new Vector3(0f, 0.777f, 0f), new Vector3(fieldL, 0.004f, fieldD),
            TexturedMat("L2_Taracea_" + Num(fieldL) + "x" + Num(fieldD), InlayTexture(), new Vector2(fieldL / InlayTile, fieldD / InlayTile), Color.white, 0.55f, 0f));

        // Banda a cuadros: sobre el tablero (largos y cortos) y en el canto del faldón
        const float band = 0.035f;
        float longL = length - 0.12f, shortL = depth - 0.12f;
        var longMat = BandMat(longL, band);
        var shortMat = BandMat(shortL, band);
        foreach (float s in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, table, "Banda", new Vector3(0f, 0.777f, s * (depth * 0.5f - 0.06f)), new Vector3(longL, 0.004f, band), longMat);
            Prim(PrimitiveType.Cube, table, "Banda", new Vector3(s * (length * 0.5f - 0.06f), 0.777f, 0f), new Vector3(shortL, 0.004f, band), shortMat,
                false, Quaternion.Euler(0f, 90f, 0f));
            Prim(PrimitiveType.Cube, table, "BandaCanto", new Vector3(0f, 0.655f, s * ((depth - 0.1f) * 0.5f + 0.002f)), new Vector3(longL, band, 0.004f), longMat);
        }
    }

    /// <summary>Pata curva (cabriole) que se abre hacia +z local; la base va en el suelo.</summary>
    private static void CurvedLeg(Transform table, Vector3 pos, float yaw)
    {
        if (tableLegMesh == null)
        {
            const int n = 16;
            var centers = new Vector3[n + 3];
            var radii = new float[n + 3];
            const float top = 0.605f;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;   // 0 arriba, 1 en el pie
                float z = -0.035f * Mathf.Sin(Mathf.PI * t) + 0.07f * t * t * t;
                centers[i + 1] = new Vector3(0f, top * (1f - t), z);
                radii[i + 1] = Mathf.Lerp(0.04f, 0.022f, t) + (t > 0.9f ? (t - 0.9f) * 0.12f : 0f);
            }
            centers[0] = centers[1];            // tapa de arriba
            centers[n + 2] = centers[n + 1];    // tapa del pie
            var mesh = TubeAlong(centers, radii, 10);
            mesh.name = "L2_PataCurva";
            tableLegMesh = SaveMesh(mesh);
        }

        var go = new GameObject("Pata");
        go.transform.SetParent(table, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = tableLegMesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = InlayWood();
    }

    private static Material BandMat(float length, float width)
    {
        // Cada cuadro de la banda mide lo mismo que su ancho útil (8 cuadros por repetición)
        float repeat = width * 0.64f * 8f;
        return TexturedMat("L2_BandaTaracea_" + Num(length), BandTexture(), new Vector2(length / repeat, 1f), Color.white, 0.5f, 0f);
    }

    private static Texture2D InlayTexture() =>
        inlayTex != null ? inlayTex : inlayTex = GeneratedTexture("L2_Taracea", 512, 512, InlayPixel);

    private static Texture2D BandTexture() =>
        bandTex != null ? bandTex : bandTex = GeneratedTexture("L2_BandaTaracea", 256, 32, BandPixel);

    // Colores de la taracea
    private static readonly Color Pearl = new Color(0.86f, 0.84f, 0.79f);
    private static readonly Color InlayDark = new Color(0.09f, 0.06f, 0.05f);
    private static readonly Color InlayRed = new Color(0.33f, 0.12f, 0.08f);

    /// <summary>
    /// Una repetición del patrón: estrella de 8 puntas al centro cuyos lados se prolongan formando la red de líneas,
    /// rosetas pequeñas en las esquinas y el fondo hecho de astillas de nácar de tonos levemente distintos.
    /// </summary>
    private static Color InlayPixel(float u, float v)
    {
        const float s = 0.21f, line = 0.0075f, aa = 1.4f / 512f, r2 = 1.41421356f;
        float k = s * r2;

        // Astillas de nácar: celdas pequeñas con brillo y tono propios
        float h1 = Hash(Mathf.Floor(u * 42f), Mathf.Floor(v * 42f));
        float h2 = Hash(Mathf.Floor(u * 42f) + 17f, Mathf.Floor(v * 42f) + 5f);
        var tint = Color.Lerp(new Color(0.97f, 0.95f, 1.02f), new Color(0.95f, 1.01f, 0.97f), h2);
        Color c = Pearl * (0.9f + 0.14f * h1) * tint;

        // Estrella central de 8 puntas (unión de un cuadrado y un rombo): > 0 adentro
        float du = u - 0.5f, dv = v - 0.5f;
        float star = Mathf.Max(s - Mathf.Max(Mathf.Abs(du), Mathf.Abs(dv)), (k - (Mathf.Abs(du) + Mathf.Abs(dv))) / r2);
        if (star > 0f) c = Color.Lerp(c, new Color(0.96f, 0.95f, 0.91f), 0.6f);

        // Estrella pequeña de madera rojiza en las esquinas
        float cu = Mathf.Abs(Mathf.Repeat(u + 0.5f, 1f) - 0.5f), cv = Mathf.Abs(Mathf.Repeat(v + 0.5f, 1f) - 0.5f);
        float rose = Mathf.Max(0.11f - Mathf.Max(cu, cv), (0.11f * r2 - (cu + cv)) / r2);
        if (rose > 0f) c = InlayRed * (0.9f + 0.2f * h1);
        float d = Mathf.Min(Mathf.Abs(rose), Mathf.Abs(star));

        // Red de líneas: los lados de la estrella central prolongados; se cortan al entrar en una estrella
        float lines = Mathf.Min(Mathf.Min(Mathf.Abs(u - (0.5f - s)), Mathf.Abs(u - (0.5f + s))),
                                Mathf.Min(Mathf.Abs(v - (0.5f - s)), Mathf.Abs(v - (0.5f + s))));
        lines = Mathf.Min(lines, Mathf.Min(PeriodicDist(u + v, 1f + k), PeriodicDist(u + v, 1f - k)) / r2);
        lines = Mathf.Min(lines, Mathf.Min(PeriodicDist(u - v, k), PeriodicDist(u - v, -k)) / r2);
        if (star <= line && rose <= line) d = Mathf.Min(d, lines);

        // Punto central de la estrella
        float center = Mathf.Sqrt(du * du + dv * dv);
        if (center < 0.028f) c = InlayRed;
        d = Mathf.Min(d, Mathf.Abs(center - 0.028f));

        return Color.Lerp(InlayDark, c, Mathf.SmoothStep(0f, 1f, (d - line) / aa));
    }

    /// <summary>Banda de cuadros alternos nácar / madera, con un filete oscuro a cada lado.</summary>
    private static Color BandPixel(float u, float v)
    {
        if (v < 0.18f || v > 0.82f) return InlayDark;
        return Mathf.Repeat(u * 8f, 1f) < 0.5f ? Pearl : InlayRed;
    }

    private static float PeriodicDist(float x, float c) => Mathf.Abs(Mathf.Repeat(x - c + 0.5f, 1f) - 0.5f);

    private static float Hash(float x, float y)
    {
        float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    // ================================================================ Mallas

    /// <summary>Malla de revolución: <paramref name="profile"/> son pares (radio, altura) de abajo hacia arriba.</summary>
    private static Mesh Tube(Vector2[] profile, int segments)
    {
        var centers = new Vector3[profile.Length];
        var radii = new float[profile.Length];
        for (int i = 0; i < profile.Length; i++)
        {
            centers[i] = new Vector3(0f, profile[i].y, 0f);
            radii[i] = profile[i].x;
        }
        return TubeAlong(centers, radii, segments);
    }

    /// <summary>
    /// Tubo que sigue los puntos <paramref name="centers"/> (que se mueven en el plano YZ) con el radio de cada punto.
    /// Un radio 0 cierra el tubo (tapa).
    /// </summary>
    private static Mesh TubeAlong(Vector3[] centers, float[] radii, int segments)
    {
        int rings = centers.Length, cols = segments + 1;
        var verts = new Vector3[rings * cols];
        var uvs = new Vector2[verts.Length];
        for (int i = 0; i < rings; i++)
        {
            // Dirección del tubo en este punto (los puntos repetidos de las tapas toman la del vecino)
            Vector3 tangent = Vector3.zero;
            for (int step = 1; step < rings && tangent.sqrMagnitude < 1e-8f; step++)
                tangent = centers[Mathf.Min(i + step, rings - 1)] - centers[Mathf.Max(i - step, 0)];
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.up;
            if (tangent.y < 0f) tangent = -tangent;   // siempre "hacia arriba", así la orientación de las caras no cambia
            Vector3 side = Vector3.right;
            Vector3 normal = Vector3.Cross(side, tangent.normalized).normalized;

            for (int j = 0; j < cols; j++)
            {
                float a = j * Mathf.PI * 2f / segments;
                verts[i * cols + j] = centers[i] + radii[i] * (Mathf.Cos(a) * side + Mathf.Sin(a) * normal);
                uvs[i * cols + j] = new Vector2(j / (float)segments, i / (float)(rings - 1));
            }
        }

        // Con los puntos de abajo hacia arriba, (a, c, d) y (a, d, b) quedan mirando hacia afuera
        var tris = new List<int>();
        bool upward = centers[rings - 1].y >= centers[0].y;
        for (int i = 0; i < rings - 1; i++)
            for (int j = 0; j < segments; j++)
            {
                int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                if (upward) tris.AddRange(new[] { a, c, d, a, d, b });
                else tris.AddRange(new[] { a, d, c, a, b, d });
            }

        var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Aro ovalado plano en el plano XY: radios exteriores, ancho de la banda y grosor (en Z).</summary>
    private static Mesh EllipseRing(float rx, float ry, float band, float depth, int segments)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float hz = depth * 0.5f;
        Vector3 Outer(int j, float z) { float a = j * Mathf.PI * 2f / segments; return new Vector3(rx * Mathf.Cos(a), ry * Mathf.Sin(a), z); }
        Vector3 Inner(int j, float z) { float a = j * Mathf.PI * 2f / segments; return new Vector3((rx - band) * Mathf.Cos(a), (ry - band) * Mathf.Sin(a), z); }

        // Cada franja con sus propios vértices, para que los cantos queden marcados
        void Strip(System.Func<int, Vector3> rowA, System.Func<int, Vector3> rowB, System.Func<Vector3, Vector3> facing)
        {
            for (int j = 0; j < segments; j++)
            {
                Vector3 a0 = rowA(j), a1 = rowA(j + 1), b0 = rowB(j), b1 = rowB(j + 1);
                int i0 = verts.Count;
                verts.AddRange(new[] { a0, a1, b0, b1 });
                Vector3 mid = (a0 + a1 + b0 + b1) * 0.25f;
                bool flip = Vector3.Dot(Vector3.Cross(b0 - a0, b1 - a0), facing(mid)) < 0f;
                if (flip) tris.AddRange(new[] { i0, i0 + 3, i0 + 2, i0, i0 + 1, i0 + 3 });
                else tris.AddRange(new[] { i0, i0 + 2, i0 + 3, i0, i0 + 3, i0 + 1 });
            }
        }
        Vector3 Radial(Vector3 p) => new Vector3(p.x / (rx * rx), p.y / (ry * ry), 0f);
        Strip(j => Outer(j, -hz), j => Inner(j, -hz), p => Vector3.back);
        Strip(j => Outer(j, hz), j => Inner(j, hz), p => Vector3.forward);
        Strip(j => Outer(j, -hz), j => Outer(j, hz), Radial);
        Strip(j => Inner(j, -hz), j => Inner(j, hz), p => -Radial(p));

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, new Vector2[verts.Count]);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Barra de sección cuadrada entre dos puntos (sobre el cubo de Unity).</summary>
    private static Matrix4x4 Bar(Vector3 from, Vector3 to, float thick) =>
        Matrix4x4.TRS((from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, to - from), new Vector3(thick, (to - from).magnitude, thick));

    private static Mesh Combine(List<CombineInstance> parts)
    {
        var mesh = new Mesh();
        mesh.CombineMeshes(parts.ToArray(), true, true);
        return mesh;
    }

    /// <summary>Guarda la malla como asset (o actualiza la que ya existe con ese nombre, sin romper referencias).</summary>
    private static Mesh SaveMesh(Mesh mesh)
    {
        string folder = EnsureSubfolder("Mallas");
        string path = $"{folder}/{mesh.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh, existing);
        existing.name = mesh.name;
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    // ================================================================ Texturas y materiales

    /// <summary>Dibuja la textura con <paramref name="pixel"/> (u, v en 0..1) y la guarda como PNG en Materials/Level2/Texturas.</summary>
    private static Texture2D GeneratedTexture(string name, int width, int height, System.Func<float, float, Color> pixel)
    {
        string path = $"{EnsureSubfolder("Texturas")}/{name}.png";
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        var px = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                px[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
        tex.SetPixels(px);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        ConfigureTexture(path, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ConfigureTexture(string path, TextureWrapMode wrapU, TextureWrapMode wrapV)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;
        if (imp.wrapModeU == wrapU && imp.wrapModeV == wrapV && imp.mipmapEnabled && imp.anisoLevel >= 8) return;
        imp.wrapModeU = wrapU;
        imp.wrapModeV = wrapV;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 8;   // se ve nítida aunque se mire de lado (muros largos, mesa)
        imp.SaveAndReimport();
    }

    /// <summary>Material con textura repetida. Si ya existe, se actualiza (así los cambios de este código se aplican).</summary>
    private static Material TexturedMat(string name, Texture tex, Vector2 tiling, Color color, float smoothness, float metallic)
    {
        var mat = Mat(name, color);
        foreach (var prop in new[] { "_BaseMap", "_MainTex" })
            if (mat.HasProperty(prop))
            {
                mat.SetTexture(prop, tex);
                mat.SetTextureScale(prop, tiling);
            }
        SetSurface(mat, color, smoothness, metallic);
        return mat;
    }

    private static Material MetalMat(string name, Color color, float metallic, float smoothness)
    {
        var mat = Mat(name, color);
        SetSurface(mat, color, smoothness, metallic);
        return mat;
    }

    private static void SetSurface(Material mat, Color color, float smoothness, float metallic)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
    }

    private static string EnsureSubfolder(string name)
    {
        EnsureFolder();
        string path = $"{MaterialFolder}/{name}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(MaterialFolder, name);
        return path;
    }

    private static string Num(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
