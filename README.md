# TypeKit Proxy App

[![.NET](https://github.com/jadiagaurang/TypeKitProxyApp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jadiagaurang/TypeKitProxyApp/actions/workflows/dotnet.yml)

An application to proxy Adobe TypeKit JS SDK and CSS files with `font-display` modification to improve webpage performance.

# Motivation

In September 2020, TypeKit has released a font-display option via Adobe Fonts Dashboard [https://fonts.adobe.com/my_fonts#web_projects-section](https://fonts.adobe.com/my_fonts#web_projects-section) for better web performance.

Knowledge Base Article [https://helpx.adobe.com/fonts/using/font-display-settings.html](https://helpx.adobe.com/fonts/using/font-display-settings.html).

But, the same functionality is not extended to RESTful API or JS SDK.

Google Lighthouse recommends to use `font-display: swap` in `@font-face` style to avoid [FOIT](https://fonts.google.com/knowledge/glossary/foit) and [FOUT](https://fonts.google.com/knowledge/glossary/fout) in most modern browsers.

Google Fonts supports same feature by just adding the `&display=swap` [parameter](https://developer.mozilla.org/docs/Learn/Common_questions/What_is_a_URL#Basics_anatomy_of_a_URL) to the end of your Google Fonts URL:
```html
<link href="https://fonts.googleapis.com/css?family=Roboto:400,700&display=swap" rel="stylesheet">
```

# Example

## Load TypeKit by JS

The TypeKit [https://use.typekit.net/yyj6orp.js](https://use.typekit.net/yyj6orp.js) Hosted on Adobe's CDN has `"display":"auto"` in the `window.Typekit.config` variable

Using Proxy App; it can be changed to `"display":"swap"`

```html
<script type="text/javascript" src="//localhost:80/yyj6orp.js"></script>
<script type="text/javascript">try{Typekit.load({async:true});}catch(e){}</script>
```

## Load TypeKit by CSS

The TypeKit [https://use.typekit.net/yyj6orp.css](https://use.typekit.net/yyj6orp.css) Hosted on Adobe's CDN has `font-display:auto` property for all the `@font-face`

Using Proxy App; it can be changed to `"display":"swap"`

```html
<link href="//localhost:80/yyj6orp.css" rel="stylesheet">
```

## License

Please see the [license file](https://github.com/jadiagaurang/TypeKitProxyApp/blob/main/LICENSE) for more information.
