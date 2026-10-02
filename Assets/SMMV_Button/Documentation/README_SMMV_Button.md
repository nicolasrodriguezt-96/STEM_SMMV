# SMMV Button

Botón 2D reutilizable para Unity 6, diseñado para el SMMV y pensado para
botones físicos A-D.

## Características

-   Botón circular 2D.
-   Texto editable mediante TextMeshPro.
-   Estados visuales:
    -   Normal
    -   Pressed
    -   Disabled
-   Sin estado Hover, porque la interacción final será mediante botones
    físicos.
-   Tecla configurable desde el Inspector para pruebas.
-   Utiliza el **Input System nuevo de Unity**.
-   Acción configurable desde `On Pressed()` en el Inspector.
-   Puede reutilizarse como prefab en otras escenas y proyectos.
-   La función principal del botón está separada de la entrada: teclado,
    mouse o hardware pueden terminar llamando a `Press()`.

------------------------------------------------------------------------

# 1. Requisitos

Proyecto recomendado:

-   Unity 6
-   Input System
-   TextMeshPro

## Activar Input System

Ve a:

`Edit > Project Settings > Player > Other Settings`

En:

`Active Input Handling`

selecciona:

`Input System Package (New)`

Si Unity solicita reiniciar el editor, acepta.

También verifica en:

`Window > Package Manager`

que **Input System** esté instalado.

------------------------------------------------------------------------

# 2. Estructura del botón

El objeto debe tener esta estructura:

``` text
SMMV_Button
├── Background
└── ButtonText
```

## SMMV_Button

Debe tener:

-   Rect Transform
-   Canvas Renderer
-   Image
-   Button
-   SMMV Button (Script)

El componente `SMMV Button` controla la lógica del botón.

## Background

Debe tener:

-   Rect Transform
-   Image

Este `Image` es el que muestra los sprites Normal, Pressed y Disabled.

## ButtonText

Debe tener:

-   Rect Transform
-   TextMeshPro - Text (UI)

Este objeto muestra la letra del botón.

------------------------------------------------------------------------

# 3. Configurar Background

Selecciona:

`SMMV_Button > Background`

En el componente `Image` asigna:

`Button_Normal`

Este será el sprite inicial.

El objeto Background debe ocupar el área completa del botón.

------------------------------------------------------------------------

# 4. Configurar ButtonText

Selecciona:

`SMMV_Button > ButtonText`

Configura el TextMeshPro para que:

-   esté centrado horizontalmente;
-   esté centrado verticalmente;
-   quede delante del Background;
-   tenga el tamaño de letra deseado.

No pongas la letra dentro del PNG.

La letra se controla mediante TextMeshPro.

------------------------------------------------------------------------

# 5. Configurar SMMV Button

Selecciona el objeto raíz:

`SMMV_Button`

En `SMMV Button (Script)` aparecerán estos campos.

## Identificación

### Button ID

Ejemplo:

``` text
A
```

Puedes utilizar:

``` text
A
B
C
D
```

Esto sirve para identificar el botón.

------------------------------------------------------------------------

# 6. Texto del botón

### Button Text

Arrastra:

``` text
ButtonText
```

al campo:

``` text
Button Text
```

Debe aparecer:

``` text
ButtonText (Text Mesh Pro UGUI)
```

### Button Label

Escribe:

``` text
A
```

o:

``` text
B
```

o:

``` text
C
```

o:

``` text
D
```

Al cambiar este valor, el texto visible de TextMeshPro también cambiará.

------------------------------------------------------------------------

# 7. Tecla para pruebas

En:

`Teclado - Modo Prueba`

aparece:

`Keyboard Key`

Ejemplo:

``` text
R
```

Esto permite probar el botón desde el teclado antes de tener conectado
el hardware físico.

Ejemplo:

``` text
Button A
Keyboard Key = R
```

Al presionar `R`:

``` text
Normal → Pressed
```

Al soltar `R`:

``` text
Pressed → Normal
```

Para otro botón puedes utilizar otra tecla:

``` text
A → R
B → T
C → Y
D → U
```

Las teclas son solamente para pruebas y pueden cambiarse posteriormente.

------------------------------------------------------------------------

# 8. Configurar los sprites

En la sección:

`Sprites`

## Button Image

Arrastra:

``` text
Background
```

al campo:

``` text
Button Image
```

Debe aparecer:

``` text
Background (Image)
```

**No arrastres el PNG directamente a Button Image.**

## Normal Sprite

Asignar:

``` text
Button_Normal
```

## Pressed Sprite

Asignar:

``` text
Button_Pressed
```

## Disabled Sprite

Asignar:

``` text
Button_Disabled
```

Debe quedar:

``` text
Button Image     → Background (Image)
Normal Sprite    → Button_Normal
Pressed Sprite   → Button_Pressed
Disabled Sprite  → Button_Disabled
```

------------------------------------------------------------------------

# 9. Interactable

En:

`Estado`

encontrarás:

`Interactable`

### Activado

``` text
✓ Interactable
```

El botón funciona normalmente.

### Desactivado

``` text
□ Interactable
```

El botón muestra:

``` text
Disabled Sprite
```

y no responde a la tecla ni al mouse.

También puede desactivarse mediante código:

``` csharp
GetComponent<SMMVButton>().SetInteractable(false);
```

Para volver a activarlo:

``` csharp
GetComponent<SMMVButton>().SetInteractable(true);
```

------------------------------------------------------------------------

# 10. Acción del botón

En:

`Acción`

aparece:

`On Pressed()`

Aquí se configura qué debe hacer el botón.

Ejemplo:

``` text
On Pressed()
    GameManager
        StartGame()
```

El botón no necesita conocer la lógica del juego.

Solo ejecuta la función que se le asigne.

Esto permite reutilizar el mismo prefab en diferentes pantallas.

------------------------------------------------------------------------

# 11. Prueba del botón

Para probarlo:

1.  Coloca el botón dentro de un Canvas.
2.  Asegúrate de que el Canvas tenga:

``` text
Render Mode = Screen Space - Overlay
```

3.  Debe existir un `EventSystem` en la escena.
4.  Entra en Play Mode.
5.  Abre la pestaña `Game`.
6.  Haz clic dentro de la ventana Game para darle foco.
7.  Presiona la tecla configurada.

Ejemplo:

``` text
Keyboard Key = R
```

Presiona:

``` text
R
```

Resultado:

``` text
Normal
   ↓
Pressed
```

Al soltar:

``` text
Pressed
   ↓
Normal
```

------------------------------------------------------------------------

# 12. No necesita cámara

Si el Canvas utiliza:

``` text
Screen Space - Overlay
```

el botón no necesita una cámara para funcionar.

Si aparece en Game:

``` text
No cameras rendering
```

revisa el `Render Mode` del Canvas.

Debe ser:

``` text
Screen Space - Overlay
```

------------------------------------------------------------------------

# 13. Crear el Prefab

Una vez que el botón esté funcionando:

1.  En el Project crea una carpeta:

``` text
Assets/SMMV_Button/Prefabs
```

2.  En la Hierarchy selecciona:

``` text
SMMV_Button
```

3.  Arrástralo desde la Hierarchy hacia:

``` text
Assets/SMMV_Button/Prefabs
```

Unity creará:

``` text
SMMV_Button.prefab
```

Ahora el botón es reutilizable.

------------------------------------------------------------------------

# 14. Usar el Prefab en otra escena

Abre otra escena.

Desde:

``` text
Assets/SMMV_Button/Prefabs
```

arrastra:

``` text
SMMV_Button
```

hacia el Canvas.

El botón aparecerá con toda su configuración.

Solo cambia:

-   Button ID
-   Button Label
-   Keyboard Key
-   On Pressed()

según lo que necesites.

------------------------------------------------------------------------

# 15. Crear A, B, C y D

Puedes crear cuatro instancias del mismo prefab:

``` text
SMMV_Button_A
SMMV_Button_B
SMMV_Button_C
SMMV_Button_D
```

Configuración de ejemplo:

  Botón   Button ID   Button Label   Tecla de prueba
  ------- ----------- -------------- -----------------
  A       A           A              R
  B       B           B              T
  C       C           C              Y
  D       D           D              U

Los sprites y la estructura siguen siendo los mismos.

------------------------------------------------------------------------

# 16. Importante: no usar Hover

Este botón está pensado para el SMMV.

La interacción final se realizará mediante botones físicos A-D. Por eso
no se necesita:

``` text
Hover
```

Los estados utilizados son únicamente:

``` text
Normal
Pressed
Disabled
```

Esto coincide con la idea del prototipo físico, donde los cuatro botones
A-D son el mecanismo principal de interacción.

------------------------------------------------------------------------

# 17. Arquitectura preparada para hardware

La función principal del botón es:

``` csharp
Press()
```

Actualmente puede ser activada mediante:

``` text
Teclado
   ↓
SMMVButton
   ↓
Press()
```

o mediante interacción de UI:

``` text
Mouse
   ↓
SMMVButton
   ↓
Press()
```

En la versión física se podrá conectar el hardware:

``` text
Botón físico A
      ↓
Controlador / Raspberry Pi
      ↓
Input
      ↓
SMMVButton
      ↓
Press()
      ↓
On Pressed()
```

La ventaja es que la lógica visual y las acciones del botón pueden
mantenerse mientras cambia únicamente el origen de la entrada.

------------------------------------------------------------------------

# 18. Estructura recomendada del paquete

Para reutilizarlo en otros proyectos:

``` text
Assets
└── SMMV_Button
    ├── Prefabs
    │   └── SMMV_Button.prefab
    │
    ├── Scripts
    │   └── SMMVButton.cs
    │
    ├── Sprites
    │   ├── Button_Normal.png
    │   ├── Button_Pressed.png
    │   └── Button_Disabled.png
    │
    ├── Documentation
    │   └── README.md
    │
    └── Examples
        └── ExampleScene
```

------------------------------------------------------------------------

# 19. Llevarlo a otro proyecto

La opción más sencilla para compartirlo entre proyectos es copiar la
carpeta:

``` text
Assets/SMMV_Button
```

al proyecto nuevo.

Después verifica que el proyecto nuevo tenga instalado:

``` text
Input System
```

y:

``` text
TextMeshPro
```

Luego puedes arrastrar:

``` text
SMMV_Button.prefab
```

a cualquier Canvas.

------------------------------------------------------------------------

# 20. Regla principal de uso

Una vez creado el prefab, normalmente solo necesitas configurar:

``` text
Button ID
Button Label
Keyboard Key
On Pressed()
```

Los demás elementos deberían quedar configurados dentro del prefab.

Ejemplo:

``` text
SMMV_Button
│
├── Button ID      → A
├── Button Label   → A
├── Keyboard Key   → R
│
├── Normal Sprite
├── Pressed Sprite
├── Disabled Sprite
│
└── On Pressed()
       └── Acción que quieras ejecutar
```

Así el botón funciona como un componente **plug and play** para las
diferentes escenas del SMMV.
