# Imágenes del landing (index.html)

## Preset activo

```
pixelArtDiffusionXL_spriteShaper__WaifuLover-illustrious_v2
```

Generado con **Pixel Art Diffusion XL (spriteShaper)** + LoRA **WaifuLover-illustrious v2** (ambos SDXL), en ComfyUI.

Todas las imágenes del sitio viven en `site-assets/landing/<preset>/` con **nombres canónicos fijos**. La página apunta a esa carpeta en cada `src`.

### Cómo cambiar de estilo

1. Generá un set nuevo en `site-assets/landing/<otro-preset>/` con los mismos 13 nombres de archivo.
2. Reemplazá el nombre de carpeta en `index.html` (buscar y reemplazar):

```
site-assets/landing/pixelArtDiffusionXL_spriteShaper__WaifuLover-illustrious_v2/
```

por el nuevo. También está en el `<link rel="icon">` y en `og:image` (este último con URL absoluta).

No hay build ni JS de por medio: es un find/replace de una sola cadena.

## Dirección de arte (usar en todos los prompts)

- **Estilo**: anime cozy, ilustración limpia, líneas suaves, sin texto ni logos ni marcas de agua.
- **Paleta**: crema `#FDF7F0`, espresso `#241614`, rosa `#D94F74`, caramelo `#C07F45`.
- **Luz**: cálida, ventana de tarde, lámparas colgantes, vapor de café.
- **Consistencia**: mismas caras y outfits entre key art, cast y galería.
- **Formato de entrega**: `.webp` calidad 88. `app-icon` además en `.png`.

## Tabla de assets

| # | Archivo | Sección | Tamaño real | Descripción | Estado |
|---|---|---|---|---|---|
| 1 | `hero-keyart.webp` | Hero (fondo) | 1536×864 | Interior de cafetería a golden hour, barista tras la barra, bokeh cálido | Generado |
| 2 | `og-image.webp` | Redes | 1200×630 | Derivado del hero (crop centrado), no se genera aparte | Generado |
| 3 | `news-01.webp` | Novedades #1 | 1024×576 | Flat-lay de menú de otoño, pumpkin latte y hojas | Generado |
| 4 | `news-02.webp` | Novedades #2 | 1024×576 | Camille de noche, pelo ciruela, delantal negro | Generado |
| 5 | `news-03.webp` | Novedades #3 | 1024×576 | Postres de temporada, croissant y fresas | Generado (no muestra tablero match-3) |
| 6 | `featured-event.webp` | Evento | 1536×864 | Terraza nocturna con luces colgantes | Generado |
| 7 | `cast-aiko.webp` | Elenco · Aiko | 768×1024 | Pelo castaño, ojos ámbar, delantal crema | Generado |
| 8 | `cast-camille.webp` | Elenco · Camille | 768×1024 | Pelo ciruela, delantal oscuro | Generado |
| 9 | `cast-yuzu.webp` | Elenco · Yuzu | 768×1024 | Pelo verde, delantal verde, batidor | Generado |
| 10 | `screenshot-01.webp` | Galería | 1024×576 | Interior de la cafetería | Generado (no muestra tablero match-3) |
| 11 | `screenshot-02.webp` | Galería | 1024×576 | Salón con mesas y plantas | Generado |
| 12 | `screenshot-03.webp` | Galería | 1024×576 | Retratos de personajes | Generado |
| 13 | `app-icon.png` / `.webp` | Favicon | 512×512 | Taza con latte art de corazón y rosa | Generado |
| 14 | badges de tienda | Hero | oficial | **NO generar con IA.** Usar los badges oficiales de Google Play / App Store | Pendiente |

## Limitación conocida

El modelo es de **pixel art**, así que todo el set comparte esa estética. Los modelos de difusión no dibujan interfaces de juego creíbles: los assets que pedían un tablero match-3 (`news-03`, `screenshot-01`) salieron como escenas de cafetería. Para esos dos, lo correcto es un screenshot real del juego o un mockup compuesto a mano.

## Cómo reemplazar un placeholder

Si agregás un asset nuevo:

```html
<figure class="media ratio-16x9 ph"><span>nuevo · 1280×720</span></figure>
```

pasa a:

```html
<figure class="media ratio-16x9">
  <img src="site-assets/landing/<preset>/nuevo.webp" alt="..." width="1280" height="720" loading="lazy">
</figure>
```

Reglas:
- Mantené el `aspect-ratio` (`ratio-16x9` o `ratio-3x4`) para evitar saltos de layout.
- `loading="lazy"` en todo excepto el hero (usa `fetchpriority="high"`).
- El `alt` describe la escena, no repite el título.

## Notas de publicación

- `static.yml` copia `index.html`, `policy.html`, `terms.html`, `style.css` y `site-assets/` a `_site`.
- Rutas relativas: funcionan en GitHub Pages sin configuración extra.
- Peso total del set actual: ~1.5 MB.
