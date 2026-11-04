# Codelco-Proyecto
Proyecto de codelco sobre camaras y pesaje

Propuesta de Ingmétrica para la **Báscula N9 de la Refinería Ventanas** (Codelco División Ventanas): identificar y pesar las cargas de cátodos comerciales, láminas y restos de ánodos. La etiqueta se imprime en la nave, la báscula controla el peso y todo queda en un solo registro, listo en Excel.

## Contenido

| Archivo | Qué es |
|---|---|
| `simulacion-bascula.html` | Simulación de la báscula: escena, cámara, balizas B1 y B2, pantalla del pesador y los sistemas actuales. Usa `estilos.css`, `casos.js`, `sim.js`, `iso.js` y `prueba.js` (tienen que estar en la misma carpeta). |
| `terminal-vt1200.html` | Maqueta de la pantalla de la VT1200 en la nave (1024 × 768): imprime la etiqueta de cada carga, muestra la Báscula N9 en vivo controlando el peso, deja editar el pesaje con los datos de las planillas (pantalla Báscula), guarda el registro y baja el Excel. |
| `app/` | La app para instalar en la VT1200: `TerminalVentanas` (WPF + WebView2, .NET 8 self-contained) muestra esa misma pantalla a pantalla completa, sin internet, guarda el Excel en una carpeta del equipo e imprime en la Zebra. `app/construir.py` arma el release. |
| `diagramas/flujo-bascula-n9.svg` | Diagrama del flujo propuesto (también en `.png`). Se genera con `diagramas/generar_diagrama.py`. |
| `presentacion/Bascula-N9-Ventanas.pptx` | Presentación de 14 láminas con los datos de la visita, las pantallas y los flujos. |
| `presentacion/fuente/` | Lo que arma la presentación: `generar.js` (pptxgenjs), `capturas.py` (saca las capturas) e `img/`. Las láminas de las imágenes del sistema están apagadas (`INCLUIR` en `generar.js`). |

## Cómo verlo

Los dos HTML se abren directo en el navegador, sin instalar nada. Necesitan internet solo para las fuentes (Google Fonts) y las librerías (cdnjs: código QR y Excel).

- **Simulación:** elija un producto (cátodos, restos, láminas, dos pallets montados) o una situación (sin etiqueta, grúa encima, carro fuera del área, nódulos, fuera de orden, fuera de rango, lado parcial).
- **Maqueta VT1200:** toque Entrar e Imprimir. La báscula de la derecha pesa cada carga. En la guía de la derecha están los botones para probar grúa encima, carro fuera del área y báscula sin cero. El botón Excel baja el registro del día.

## App para la VT1200

Se baja desde **Releases** (`terminal-ventanas-vX.Y.Z-AAAAMMDD.zip`).

1. Descomprimir en la VT1200, por ejemplo en `C:\Users\vt1200\Documents\` (como los releases del calibrador).
2. Abrir `TerminalVentanas.exe`. Ocupa la pantalla completa; F11 la pasa a ventana y Alt+F4 la cierra.
3. F2 abre el panel de prueba: grúa encima, carro fuera del área, báscula sin cero, caja sin etiqueta y reiniciar el registro.

Se ajusta en `appsettings.json`, junto al `.exe`:

| Dato | Qué hace |
|---|---|
| `CarpetaExcel` | Dónde queda el Excel al tocar Excel (por defecto `C:\BasculaN9\Excel`). |
| `Impresora.Modo` | `seco` guarda cada etiqueta como `.zpl` en `CarpetaZpl` y no imprime. `zebra` imprime en `Impresora.Nombre`. |
| `PantallaCompleta` | `false` la abre en ventana. |

Necesita **Microsoft Edge WebView2 Runtime**, que viene con Windows 11 y con Windows 10 actualizado. Si falta, la app lo dice al abrir y hay que instalar el WebView2 Runtime x64 sin conexión de Microsoft. Los pesos son simulados: todavía no se conecta a la Báscula N9.

Para armar un release nuevo: `python app/construir.py` (necesita el SDK de .NET 8 e internet la primera vez, para bajar las librerías y las fuentes).

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
