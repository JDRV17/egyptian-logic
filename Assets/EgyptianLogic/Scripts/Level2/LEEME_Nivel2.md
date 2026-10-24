# Nivel 2 – Retos interactivos

Los 18 retos del Nivel 2 del documento *Retos por Nivel* (6 juegos × 3 dificultades), hechos con las mismas
piezas del Nivel 1: switches `XRSwitchToggle`, botones `XRPhysicalButton`, compuertas `LogicGateItem` en sockets
XR y un `PuzzleVersionManager` que activa una versión (V1 fácil, V2 medio, V3 difícil).

## Cómo armarlos en la escena

1. Abre (o crea) la escena del Nivel 2 con el XR Rig y el `XRInteractionManager`, igual que `EgyptLevel1`.
2. Menú **Egyptian Logic → Nivel 2 → Construir los 6 retos**.
   Crea `Nivel2_Retos` con un objeto por reto, uno al lado del otro sobre el eje X (cada uno con V1, V2 y V3).
   También puedes construir un solo reto desde el mismo menú.
3. Mueve cada `Reto 2.x` a su lugar en la cámara. El jugador debe mirar los paneles desde su lado **-Z local**.
4. Los materiales se guardan en `Assets/EgyptianLogic/Materials/Level2` (las compuertas AND/OR/XOR/NOT reutilizan los del Nivel 1).
   Puedes cambiar modelos y materiales sin tocar la lógica.

Cada `Reto 2.x` tiene un `PuzzleVersionManager` con *Randomize On Start* activo. Para probar una versión concreta,
apágalo y elige *Forced Version*, o haz clic derecho en el componente → *Previsualizar V1/V2/V3*.
Clic derecho en cualquier reto → **Forzar resuelto (prueba)** lo da por resuelto para probar el flujo del nivel.

## Los retos

| Reto | Componente | Qué hace el jugador | Cómo se aprueba |
|---|---|---|---|
| 2.1 El Detective de Verdades | `TruthTablePuzzle` | Lee el circuito y toca las celdas de la columna S (? → 1 → 0…) | Botón *Comprobar*: todas las filas que no son X correctas (las incorrectas se marcan en rojo) |
| 2.2 Solo Tengo Esto | `CircuitSpecPuzzle` | Encaja compuertas NAND en las ranuras y conecta cables | Botón: la tabla completa coincide y solo hay NAND (las ranuras rechazan otras compuertas) |
| 2.3 El Ahorro de Compuertas | `KarnaughMapPuzzle` | Agrupa celdas del mapa (el cartel dice si cada grupo es válido y qué término da) y escribe la expresión con el teclado | Botón: equivalente en las filas que no son X **y** con el mínimo de literales (calculado automáticamente) |
| 2.4 El Selector (MUX) | `CircuitSpecPuzzle` | V1 con compuertas, V2 con módulos MUX 2:1, V3 en cascada con MUX 4:1 + MUX 2:1 | Prueba en vivo: con el circuito correcto, recorrer todas las combinaciones de los selectores |
| 2.5 El Repartidor (Decodificador) | `CircuitSpecPuzzle` | NOT + AND (de 3 entradas en V2/V3); V3 con Enable | Prueba en vivo de todas las direcciones (y de E en V3) |
| 2.6 La Balanza Digital | `CircuitSpecPuzzle` | V1 medio sumador; V2 4 sumadores completos encadenados; V3 sumador/restador + comparador | V1 prueba en vivo; V2 y V3 botón (se revisan las 256/512 combinaciones) |

### Protoboard (2.2, 2.4, 2.5, 2.6)

- **Entradas** (izquierda): palanca + luz. De cada entrada salen varios **enchufes de cable**.
- **Ranuras** (centro): socket para la compuerta, puertos de entrada a la izquierda y pines de salida (con sus cables) a la derecha.
  Al encajar una compuerta, los puertos cambian de nombre (I0, I1, S… / A, B, Cin…).
- **Salidas** (derecha): puerto + bombilla.
- Los cables se pintan **amarillo = 1**, **azul = 0** y **gris = sin señal**.
- Si se suelta una compuerta o un cable fuera de un socket, vuelve solo a su sitio (`ReturnHome`).

| Compuerta | Puertos | Salidas |
|---|---|---|
| AND, OR, XOR, NAND, NOR, XNOR | todos los conectados (mínimo 2) | Y |
| NOT | el primer puerto conectado | Y |
| MUX 2:1 | I0, I1, S | Y |
| MUX 4:1 | I0, I1, I2, I3, S0, S1 | Y |
| Medio sumador | A, B | S, C |
| Sumador completo | A, B, Cin | S, Cout |

Los módulos se agregaron **al final** de `LogicGateType`, así que los valores ya guardados en el Nivel 1 no cambian.

## Especificaciones (cambiar un reto sin programar)

Cada reto guarda su lógica como texto en el Inspector (`spec`), una línea por salida:

```
X0 = B0 ^ M          # nombres intermedios permitidos
S0 = A0 ^ X0 ^ M
dc GT = !M           # filas donde GT no importa (X)
```

Operadores: `!A`, `~A` o `A'` (NOT) · `A & B`, `A * B` o `AB` (AND) · `A | B` o `A + B` (OR) · `A ^ B` (XOR) ·
`NAND(A,B)`, `NOR(...)`, `XNOR(...)` · constantes `0` y `1`.
Los nombres de las entradas y salidas del protoboard (`displayName`) deben coincidir con los de la especificación;
si no, se muestra un error en la consola al iniciar.

Soluciones de referencia (calculadas por el juego):
2.3.1 → `A` · 2.3.2 → `B' + AC'` · 2.3.3 → `A'B' + CD` o `A'D + CD` (4 literales).

## Desempeño y adaptación (como en el Nivel 1)

Todos los retos heredan de `Level2Puzzle`, que tiene:

- `tracker` + `stageIndex`: reporta interacciones, intentos fallidos y la finalización al `PerformanceTracker`.
- Eventos `onSolved`, `onIncorrect` y `onInteraction` para conectar puertas (`DoorOpener.Open`), sonidos, etc.
- `successSfx` / `failSfx` opcionales (`SfxPlayer`).

Para el `AdaptationEngine` y el `HintManager`, usa el `PuzzleVersionManager` de cada reto como *Version Target* /
*Version Source*, igual que el reto 1.2.

Valores ideales del documento para los `StageConfig` del `PerformanceTracker` (T ideal en segundos / interacciones):

| Reto | V1 | V2 | V3 | Has Submit |
|---|---|---|---|---|
| 2.1 | 25 / 4 | 60 / 8 | 120 / 16 | sí |
| 2.2 | 25 / 7 | 60 / 10 | 150 / 17 | sí |
| 2.3 | 30 / 2 | 60 / 6 | 90 / 7 | sí |
| 2.4 | 30 / 8 | 90 / 20 | 180 / 48 | no |
| 2.5 | 30 / 8 | 90 / 24 | 180 / 33 | no |
| 2.6 | 30 / 12 | 90 / 24 | 180 / 36 | no (V2 y V3 sí tienen botón; ver nota) |

Notas:
- En 2.3 cada toque de celda cuenta como interacción (el documento cuenta "1 para agrupar"). Si quieres alinearlo, sube las interacciones ideales.
- `StageConfig.hasSubmit` es por reto y no por versión. En 2.6 la V1 se valida en vivo y la V2/V3 con botón.

## Pendiente

- La **variante inversa** de 2.1 (armar el circuito a partir de la tabla) no está hecha. Se puede montar con `CircuitSpecPuzzle` y la misma especificación.
- En 2.1 el circuito se muestra como texto (`S = AND( OR(A,B), NAND(A,B) )`), no como un modelo 3D.
- Probado solo fuera de Unity: compila contra las APIs de UnityEngine y la lógica tiene pruebas automáticas. Falta probarlo en el editor 2020.3 con el visor (distancias, tamaños de texto y agarre).
