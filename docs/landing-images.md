# Imágenes del landing (index.html)

Lista de assets visuales que faltan. La página usa **placeholders CSS** hoy: cuando generes una imagen, guárdala en `site-assets/landing/` con el nombre exacto de la tabla y reemplazá el bloque `<figure class="media ... ph">` por un `<img>`.

## Dirección de arte (usar en todos los prompts)

- **Estilo**: anime cozy, ilustración limpia, líneas suaves, sin texto ni logos ni marcas de agua.
- **Paleta**: crema `#FDF7F0`, espresso `#241614`, rosa `#D94F74`, caramelo `#C07F45`.
- **Luz**: cálida, ventana de tarde, lámparas colgantes, vapor de café.
- **Consistencia**: mismas caras y outfits entre key art, cast y screenshots.
- **Formato de entrega**: `.webp` (o `.png` y convertir), sin transparencia salvo donde se indique.

## Tabla de assets

| # | Archivo destino | Sección | Tamaño | Descripción / prompt (EN) | Estado |
|---|---|---|---|---|---|
| 1 | `hero-keyart.webp` | Hero (fondo) | 1920×1080 | Wide cinematic key art of a cozy anime café interior at golden hour: wooden counter, hanging lamps, steam, a cheerful barista girl in a cream-and-rose apron behind the counter, shelves of cups, plants, warm bokeh. Leave the left third visually calm/darker for text overlay. No text. | Pendiente |
| 2 | `og-image.webp` | Redes / preview | 1200×630 | Social share card: the café logo area on the left (leave space for text overlay), barista and café interior on the right, warm cream background, soft rose gradient. No baked-in text. | Pendiente |
| 3 | `news-01.webp` | Novedades #1 | 1280×720 | Autumn seasonal menu flat-lay on a café counter: pumpkin latte, cinnamon, small pumpkin decorations, warm orange and cream tones, cozy anime illustration style. No text. | Pendiente |
| 4 | `news-02.webp` | Novedades #2 | 1280×720 | Portrait of Camille, the night-shift barista: dark plum hair, calm violet eyes, black-and-rose apron, standing in a dim café lit by moon and a single warm lamp. Anime style. No text. | Pendiente |
| 5 | `news-03.webp` | Novedades #3 | 1280×720 | Close-up of a match-3 game board with café-themed tiles (coffee bean, croissant, strawberry, teacup) glowing in a chain combo, soft magical sparkles, UI-free illustration. No text. | Pendiente |
| 6 | `featured-event.webp` | Evento destacado | 1600×900 | Midnight café festival scene: the shop open at night with string lights, silhouettes of sleepy customers, a banner-free storefront, warm windows against a deep blue night, anime illustration. No text. | Pendiente |
| 7 | `cast-aiko.webp` | Elenco · Aiko | 900×1200 | Full-body anime character sheet of Aiko: short caramel-brown hair, bright amber eyes, cream apron with rose trim, energetic pose holding an espresso cup, plain warm background. No text. | Pendiente |
| 8 | `cast-camille.webp` | Elenco · Camille | 900×1200 | Full-body anime character sheet of Camille: long dark plum hair, violet eyes, black apron with rose accents, calm elegant pose holding a tray, plain warm background. No text. | Pendiente |
| 9 | `cast-yuzu.webp` | Elenco · Yuzu | 900×1200 | Full-body anime character sheet of Yuzu: green-tinted bob, big cheerful eyes, matcha-green apron, clumsy hopeful pose with a whisk, plain warm background. No text. | Pendiente |
| 10 | `screenshot-01.webp` | Galería | 1600×900 | In-game screenshot mockup: café management board with rows of tiles mid-cascade, order tickets on the side, warm UI frames, readable but generic HUD. No real text/numbers if possible. | Pendiente |
| 11 | `screenshot-02.webp` | Galería | 1600×900 | In-game screenshot mockup: decorated café room with tables, plants, lamps, a barista serving a customer, cozy isometric-ish 2D view. | Pendiente |
| 12 | `screenshot-03.webp` | Galería | 1600×900 | In-game screenshot mockup: party/character selection screen with three round portraits, skill icons, soft rose UI panels. | Pendiente |
| 13 | `app-icon.png` | Favicon / tienda | 512×512 | App icon: coffee cup with a heart-shaped latte art and a tiny rose ribbon, bold silhouette, cream background, readable at 48px. Transparent not required. | Pendiente |
| 14 | badges de tienda | Hero | oficial | **NO generar con IA.** Descargar los badges oficiales de Google Play y App Store y usarlos según sus lineamientos. Reemplazar el botón `.btn-primary` del hero. | Pendiente |

## Cómo reemplazar un placeholder

Actual (ejemplo):

```html
<figure class="media ratio-16x9 ph"><span>news-01 · 1280×720</span></figure>
```

Final:

```html
<figure class="media ratio-16x9">
  <img src="site-assets/landing/news-01.webp" alt="Menú de otoño con latte de calabaza" width="1280" height="720" loading="lazy">
</figure>
```

Reglas:
- Mantené el `aspect-ratio` (clase `ratio-16x9` o `ratio-3x4`) para evitar saltos de layout.
- `loading="lazy"` en todo excepto el hero.
- El `alt` describe la escena, no repite el título.
- El hero usa `<div class="hero-art media ph">`; al reemplazar, dejá `<img>` absoluto o cambiá a `background-image` en `.hero-art`.

## Notas de publicación

- `static.yml` copia `index.html`, `policy.html`, `terms.html`, `style.css` y `assets/` a `_site`.
- Los archivos deben quedar en `site-assets/landing/` para que las rutas relativas funcionen en GitHub Pages.
