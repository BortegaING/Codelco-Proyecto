# Codelco-Proyecto
Proyecto de codelco sobre camaras y pesaje

Propuesta de Ingmétrica para la **Báscula N9 de la Refinería Ventanas** (Codelco División Ventanas): identificar y pesar las cargas de cátodos comerciales, láminas y restos de ánodos. La etiqueta se imprime en la nave, la báscula controla el peso y todo queda en un solo registro, listo en Excel.

## Contenido

| Archivo | Qué es |
|---|---|
| `simulacion-bascula.html` | Simulación de la báscula: escena, cámara, balizas B1 y B2, pantalla del pesador y los sistemas actuales. Usa `estilos.css`, `casos.js`, `sim.js`, `iso.js` y `prueba.js` (tienen que estar en la misma carpeta). |
| `terminal-vt1200.html` | Maqueta de la pantalla de la VT1200 en la nave (1024 × 768): imprime la etiqueta de cada carga, muestra la Báscula N9 en vivo controlando el peso, deja editar el pesaje con los datos de las planillas (pantalla Báscula), guarda el registro y baja el Excel. |
| `app/` | La app para instalar en la VT1200: `TerminalVentanas.exe`, una app nativa de Windows (WPF, .NET 8 self-contained). No usa navegador ni páginas web: trae las mismas pantallas de la maqueta, funciona sin internet, guarda el Excel en una carpeta del equipo e imprime en la Zebra. `app/Lanzador` es el `.exe` que queda a la vista y abre el programa. `app/construir.py` arma el release y lo prueba. |
| `diagramas/flujo-bascula-n9.svg` | Diagrama del flujo propuesto (también en `.png`). Se genera con `diagramas/generar_diagrama.py`. |
| `presentacion/Bascula-N9-Ventanas.pptx` | Presentación de 16 láminas con los datos de la visita, las pantallas y los flujos. |
| `presentacion/fuente/` | Lo que arma la presentación: `generar.js` (pptxgenjs), `capturas.py` (saca las capturas) e `img/`. Las láminas de las imágenes del sistema están apagadas (`INCLUIR` en `generar.js`). |

## Cómo verlo

Lo que va a la VT1200 es la app (`TerminalVentanas.exe`, más abajo). Los dos HTML son material de diseño y de presentación: sirven para mostrar la propuesta en un computador cualquiera y necesitan internet para las fuentes y las librerías.

- **Simulación:** elija un producto (cátodos, restos, láminas, dos pallets montados) o una situación (sin etiqueta, grúa encima, carro fuera del área, nódulos, fuera de orden, fuera de rango, lado parcial).
- **Maqueta VT1200:** toque Entrar e Imprimir. La báscula de la derecha pesa cada carga. En la guía de la derecha están los botones para probar grúa encima, carro fuera del área y báscula sin cero. El botón Excel baja el registro del día.

## App para la VT1200

Se baja desde **Releases** (`terminal-ventanas-vX.Y.Z-AAAAMMDD.zip`).

1. Descomprimir con **Extraer todo** en la VT1200, por ejemplo en `C:\Users\vt1200\Documents\` (como los releases del calibrador).
2. Abrir `TerminalVentanas.exe`, que queda a la vista apenas se descomprime (se le puede dejar un acceso directo en el escritorio). El programa real está en la subcarpeta `programa`, con las DLL de .NET: no separar los archivos. Es un programa de Windows normal: no abre ningún navegador. Ocupa la pantalla completa; F11 la pasa a ventana. Para cerrarla: **Salir** en la pantalla del RUT (pregunta antes) o Alt+F4.
3. F2 abre el panel de prueba: grúa encima, carro fuera del área, báscula sin cero, caja sin etiqueta y reiniciar el registro.

Se ajusta en `programa\appsettings.json`:

| Dato | Qué hace |
|---|---|
| `CarpetaExcel` | Dónde queda el Excel al tocar Excel (por defecto `C:\BasculaN9\Excel`). |
| `Impresora.Modo` | `seco` guarda cada etiqueta como `.zpl` en `CarpetaZpl` y no imprime. `zebra` imprime en `Impresora.Nombre`. |
| `PantallaCompleta` | `false` la abre en ventana. |

El `TerminalVentanas.exe` de afuera es un lanzador chico (.NET Framework 4.8, que viene con Windows) que abre el de `programa`. No se usa un `.exe` de un solo archivo porque Smart App Control de Windows lo bloquea ("Una directiva de Control de aplicaciones bloqueó este archivo"); el lanzador y la carpeta sí abren. No necesita nada instalado en la VT1200: el runtime de .NET 8, las fuentes, el código QR y el Excel van dentro de la carpeta (Windows 10 u 11 de 64 bits). El registro del día se guarda en `%LOCALAPPDATA%\IngMetrica\TerminalVentanas\` y vuelve si la app se cierra. Los pesos son simulados: todavía no se conecta a la Báscula N9.

Para armar un release nuevo: `python app/construir.py`. Compila self-contained en `programa/`, compila el lanzador, revisa que estén `hostfxr.dll`, `hostpolicy.dll` y `coreclr.dll`, arma el zip, lo descomprime en una carpeta limpia y corre `TerminalVentanas.exe --prueba <carpeta>` por el lanzador, que recorre cátodos, grúa encima, láminas, restos sin tara, edición en la báscula, caja sin etiqueta y Excel, y deja capturas de cada paso. Necesita el SDK de .NET 8; internet solo la primera vez, para bajar los paquetes.

## Flujo propuesto

![Flujo propuesto para la Báscula N9](diagramas/flujo-bascula-n9.svg)

La misma idea en texto, para editarla rápido:

```mermaid
flowchart LR
  A["VT1200 en la nave:<br/>elige lo que sigue"] --> B["Imprime la etiqueta<br/>rótulo + QR"]
  B --> C["Grúa o tractor<br/>lleva la carga"]
  C --> D{"¿La cámara lee<br/>la etiqueta?"}
  D -- No --> D2["La identifica el pesador<br/>B1 ámbar"] --> E
  D -- Sí --> E{"¿Grúa fuera, carga en el área<br/>y báscula en cero?"}
  E -- No --> E2["Bloqueado<br/>B2 roja"] -.-> E
  E -- Sí --> F["Peso estable"] --> G["Neto: tara 12 kg,<br/>carro + rack o diferencia"]
  G --> H{"¿Dentro de<br/>lo esperado?"}
  H -- Sí --> I["Registro único<br/>con la hora real"]
  H -- No --> H2["Se registra con nota<br/>B1 roja"] --> I
  I --> J["Excel por turno"] --> K["Sistemas de Codelco"]
```

## Regenerar la presentación y el diagrama

```
cd presentacion/fuente
corepack pnpm install        # con pnpm, no con npm
python capturas.py           # opcional: vuelve a sacar las capturas (Playwright + Brave)
node generar.js              # escribe presentacion/Bascula-N9-Ventanas.pptx

python diagramas/generar_diagrama.py   # desde la raíz: vuelve a escribir el SVG del flujo
```

`generar.js` revisa que los totales cuadren con el Resumen del Sistema de Cátodos (lado T 100.089 kg, lado P 64.309 kg, total 165.252 kg) antes de armar el archivo.

## Datos

- Pesos de cátodos y restos del 22-10-2026, grupo 21, y la hoja de láminas del 21-10, tomados en la visita.
- Las taras de carro + rack son de ejemplo, hasta tener la lista real.
- La maqueta simula la báscula y la impresora: no se conecta a ningún equipo.
