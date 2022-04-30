# TypeKit Proxy App

[![.NET](https://github.com/jadiagaurang/TypeKitProxyApp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jadiagaurang/TypeKitProxyApp/actions/workflows/dotnet.yml)

An application to proxy Adobe TypeKit JS SDK and CSS files with `font-display` modification to improve webpage performance.

## Motivation

In September 2020, TypeKit has released a font-display option via Adobe Fonts Dashboard [https://fonts.adobe.com/my_fonts#web_projects-section](https://fonts.adobe.com/my_fonts#web_projects-section) for better web performance.

Knowledge Base Article [https://helpx.adobe.com/fonts/using/font-display-settings.html](https://helpx.adobe.com/fonts/using/font-display-settings.html).

But, the same functionality is not extended to RESTful API or JS SDK.

Google Lighthouse recommends to use `font-display: swap` in `@font-face` style to avoid [FOIT](https://fonts.google.com/knowledge/glossary/foit) and [FOUT](https://fonts.google.com/knowledge/glossary/fout) in most modern browsers.

Google Fonts supports same feature by just adding the `&display=swap` [parameter](https://developer.mozilla.org/docs/Learn/Common_questions/What_is_a_URL#Basics_anatomy_of_a_URL) to the end of your Google Fonts URL:

```html
<link href="https://fonts.googleapis.com/css?family=Roboto:400,700&display=swap" rel="stylesheet">
```

## Code Example

### Load TypeKit by JS

Example JS: [https://use.typekit.net/yyj6orp.js](https://use.typekit.net/yyj6orp.js)

The TypeKit is hosted on Adobe's CDN with `"display":"auto"` under window variable `window.Typekit.config`.

![TypeKit_JS](https://user-images.githubusercontent.com/430637/166125251-7fdcd678-2a6e-46cc-9c56-4a3c9ce48317.png)

Using the TypeKitProxyApp, it can be changed to `"display":"swap"`

#### JS Example

```html
<script type="text/javascript" src="//localhost:8081/yyj6orp.js"></script>
<script type="text/javascript">try{Typekit.load({async:true});}catch(ex){console.log(ex)}</script>
```

![TypeKitProxy_JS](https://user-images.githubusercontent.com/430637/166125254-b5b4b194-b402-4c2a-af70-d99311cde6ca.png)

### Load TypeKit by CSS

Example CSS: [https://use.typekit.net/yyj6orp.css](https://use.typekit.net/yyj6orp.css)

The TypeKit is hosted on Adobe's CDN with `font-display:auto` property for all the `@font-face`.

![TypeKit_CSS](https://user-images.githubusercontent.com/430637/166125256-d0264b33-cc82-4764-bbba-92dd4a4411bf.png)

Using the TypeKitProxyApp, it can be changed to `font-display:swap`

#### CSS Example

```html
<link href="//localhost:8081/yyj6orp.css" rel="stylesheet">
```

![TypeKitProxy_CSS](https://user-images.githubusercontent.com/430637/166125258-1a29a5fd-c84c-4d9a-97ff-7010b050f98a.png)

## License

Please see the [license file](https://github.com/jadiagaurang/TypeKitProxyApp/blob/main/LICENSE) for more information.
