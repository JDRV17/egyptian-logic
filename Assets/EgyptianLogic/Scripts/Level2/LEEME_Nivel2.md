# Nivel 2 – Retos interactivos

Los 18 retos del Nivel 2 del documento *Retos por Nivel* (6 juegos × 3 dificultades), hechos con las mismas
piezas del Nivel 1: switches `XRSwitchToggle`, botones `XRPhysicalButton`, compuertas `LogicGateItem` en sockets
XR y un `PuzzleVersionManager` que activa una versión (V1 fácil, V2 medio, V3 difícil).

## Cómo armar el nivel en la escena

1. Abre `Assets/EgyptianLogic/Scenes/EgyptLevel2.unity`, borra su *Main Camera* y pon el jugador con el tag **Player**:
   - Para probar en PC: copia **FirstPerson** de `EgyptLevel1` (teclado y mouse) y apaga el XR Origin.
   - Para el visor: el prefab `Prefabs/XR Origin` (ponle el tag **Player** a mano) y **GameObject → XR → Interaction Manager**.
2. Menú **Egyptian Logic → Nivel 2 → Construir Nivel 2 completo (salón, retos y sistemas)**.
   Si ya habías construido el nivel, borra antes el objeto `Nivel2`.
3. Copia la posición y rotación de `Nivel2/Salon/PuntoDeInicio` al jugador (junto a la entrada, mirando a lo largo de la mesa).
4. En `Sistemas_Nivel2` → **Hint Scroll Spawner** → **Head**, revisa que esté la cámara del jugador que estés usando.
5. Agrega `EgyptLevel2` en **File → Build Settings** (`EgyptLevel3` ya está). La salida carga `EgyptLevel3`
   (cámbialo en `LevelExit` → *Next Scene Name* si la escena se llama distinto).
6. Al final, cambia *Scene Name* del `LevelPortal` de `EgyptLevel1` a `EgyptLevel2`. Hoy apunta a `EgyptLevel3` a propósito,
   para probar el Nivel 1 con el 3; cámbialo solo cuando la escena del Nivel 2 esté armada.

El menú también ajusta la luz de la escena: usa la luz direccional (o crea una) sin sombras y pone una luz ambiente cálida.

### El comedor

Un gran comedor egipcio de 40 × 28 m (techo a 8 m), todo abierto: la mesa de banquete al centro (tres mesas con pasos
entre ellas) y 3 retos contra cada muro largo, con sus piezas en el suelo frente a cada uno.

```
          muro norte:  [Detective]      [Solo Tengo]      [Repartidor]
                        · piezas ·       · piezas ·        · piezas ·
 ENTRADA ─┤         ════ mesa ════  ·  ════ mesa ════  ·  ════ mesa ════        ├─ SALIDA → Nivel 3
                        · piezas ·       · piezas ·        · piezas ·
          muro sur:    [Ahorro]         [Selector]        [Balanza]
```

Orden: Detective de Verdades → Solo Tengo Esto → Ahorro de Compuertas → Selector → Repartidor → Balanza Digital.
Los valores de `HallHalfX`, `HallHalfZ` y `ColumnSpacing` en `Level2Builder` cambian el tamaño.

```
Nivel2
├── Sistemas_Nivel2   PerformanceTracker (level 2), AdaptationEngine (CarryOver), HintManager,
│                     HintScrollSpawner, CsvStageLogger y Level2Flow, todo conectado
└── Salon
    ├── Piso, Techo, Muros            en piezas de 5 a 10 m (ver "Luces")
    ├── Entrada                       puerta (cerrada), punto de inicio, baldosas sueltas y estela con el progreso
    ├── Ambiente                      las 3 mesas de banquete con sillas y comida, pilares, braseros y objetos sueltos
    ├── Reto_1 … Reto_6               el reto, cartel con su nombre, luz indicadora, antorchas y zona de pistas
    ├── Paso_1 … Paso_6               el instrumento de cada reto y las antorchas del camino al siguiente
    └── PasilloSalida                 pasillo detrás de la puerta de salida, con la zona que carga el Nivel 3
```

Todo está hecho con figuras básicas (cubos, cilindros, esferas) para reemplazarlas por modelos.
Otras opciones del menú: **Construir los 6 retos** (solo los retos en fila, sin salón ni sistemas) y
**Construir solo 2.x** (un reto suelto para probarlo). Los materiales quedan en `Assets/EgyptianLogic/Materials/Level2`
(las compuertas AND/OR/XOR/NOT usan los del Nivel 1); se pueden cambiar modelos y materiales sin tocar la lógica.

## Recorrido y adaptación

- **Camino:** al iniciar solo está el primer reto; los demás esperan **escondidos bajo el piso**. Al resolver un reto,
  su luz se pone verde y **aparece un instrumento** que sube del piso en el pasillo frente a él: palanca, placa de presión,
  llave (se lleva a la cerradura del poste de al lado), botón, antorcha (se pone en el brasero de al lado) y, en el último,
  la palanca de salida. Al usarlo, `Level2Flow` **enciende en orden las antorchas** que llevan al reto siguiente (cruzando
  entre las mesas si está en el otro muro) y **el reto siguiente sube del piso**. La palanca final abre la puerta del
  pasillo; cruzar el pasillo carga el Nivel 3.
- **Piezas en el suelo:** en 2.2, 2.4, 2.5 y 2.6 las compuertas están repartidas por el piso frente al reto.
  Si se sueltan fuera de una ranura, vuelven a su lugar. En 2.1 y 2.3 todo se responde en el panel: se tocan las celdas de
  la tabla y, en el mapa de Karnaugh, se agrupan celdas y se escribe la expresión con el teclado.
- **Nombres:** los carteles y paneles muestran solo el nombre del reto, sin número ni dificultad.
- **Exploración (no cuenta para la nota):** la comida, las copas, los cántaros, las fuentes, las ánforas, las vasijas y las
  cestas se pueden tomar, mover y tirar. Las baldosas sueltas de la entrada se hunden, suenan y brillan al pisarlas.
- **Capas:** todo lo que se agarra va en la capa **Interactable**, que no choca con el jugador (capa *Body*).
  Si un objeto agarrado chocara con el jugador, al tomarlo por debajo el jugador se subiría encima y saldría volando.
- **Luces:** URP ilumina cada objeto con un máximo de 4 luces. Por eso el piso y los muros van en piezas,
  las antorchas tienen alcance corto (y solo una de cada dos del camino tiene luz propia) y la luz general viene de la
  direccional (que no cuenta en ese límite).
  Si un piso o muro grande recibe más de 4 luces, se ve parpadear al moverse.
- **Métrica:** cada reto reporta al `PerformanceTracker` (tiempo, intentos, interacciones y pistas) y este calcula su P.
  El reloj arranca al entrar a la zona frente al reto (la zona solo se activa cuando el reto sube del piso).
- **Dificultad:** el `AdaptationEngine` empieza con la θ que el jugador trae del Nivel 1 (*CarryOver*), elige con ella la
  versión del primer reto y, al resolver cada uno, decide si el siguiente va en V1, V2 o V3 con las mismas reglas del Nivel 1.
  Por eso los `PuzzleVersionManager` del nivel completo tienen *Randomize On Start* apagado.
  - Si el Nivel 2 se abre **sin pasar por el Nivel 1** (por ejemplo, al probarlo directo en el editor), el motor lee el
    último CSV de decisiones guardado (`gatekeeper_decisiones_*.csv` en `Application.persistentDataPath`) y sigue desde
    su θ. Si no hay ninguno, el primer reto va en **V1**. Esto lo controlan *Load Last Save If Empty* y
    *Version Without Data* en el `AdaptationEngine`; en los Niveles 1 y 3 vienen apagados.
- **Pistas:** el botón de pista del Nivel 1 da la pista del reto frente al que está el jugador.
  Hay 4 por versión (1 = dónde ir, 2 y 3 = concepto, 4 = solución) y se editan en el `HintManager`.
- **Resultados:** `CsvStageLogger` guarda cada reto en el mismo CSV de la partida, con `level = 2`.

Para probar sin jugar todo, en Play, clic derecho en `Level2Flow`:
- **Prueba: resolver el reto actual**: da el reto por resuelto y hace aparecer su instrumento.
- **Prueba: resolver y abrir el siguiente**: además usa el instrumento (se encienden las antorchas y sube el reto siguiente).

También se puede usar **Forzar resuelto (prueba)** en un reto, o **Usar (prueba)** en un `UnlockInstrument`.
En un reto suelto, *Randomize On Start* elige la versión al azar; clic derecho en su `PuzzleVersionManager` →
*Previsualizar V1/V2/V3* para ver una concreta.

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
- Las compuertas están en el suelo frente al reto. Si se suelta una compuerta o un cable fuera de un socket, vuelve solo a su sitio (`ReturnHome`).

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

## Detalles del desempeño

Todos los retos heredan de `Level2Puzzle`, que tiene:

- `tracker` + `stageIndex`: reporta interacciones, intentos fallidos y la finalización al `PerformanceTracker`.
- Eventos `onSolved`, `onIncorrect` y `onInteraction` para conectar sonidos u otros efectos.
- `successSfx` / `failSfx` opcionales (`SfxPlayer`).

El constructor del nivel completo ya llena los `StageConfig` con estos valores ideales del documento
(T ideal en segundos / interacciones):

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
- El comedor, los instrumentos y las compuertas en el suelo compilan con Unity 2020.3, pero falta recorrerlos con el visor
  (distancias, agarre desde el suelo y lectura de los carteles).
