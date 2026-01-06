# Jellyfin Custom Logo

> [!IMPORTANT]  
> The plugin have bugs, i'm working on a new version, but i don't have enough time, i will stop support from now

This plugin allows you to replace the default Jellyfin logos.

[Voir la version française](https://github.com/ImLacy/Jellyfin-CustomLogo/blob/main/README_FR.md)

---

> [!IMPORTANT]  
> This plugin change the website logo, this might not work for apps (Like mobile or tv)

## 🛠️ Installation Steps

### Access Jellyfin Dashboard:

- Navigate to your Jellyfin server's dashboard.

### Add Plugin Repository:

1. Go to `Plugins` > `Repositories`.
2. Click on `+` to add a new repository.
3. Enter the following details:
    - Add a name
    - **URL:**
      ```
      https://raw.githubusercontent.com/ImLacy/Jellyfin-CustomLogo/refs/heads/main/manifest.json
      ```
4. Click **Save**.

### Install the Plugin:

- Navigate to `Plugins` > `Catalog`.
- Find **Custom Logo** in the list.
- Click **Install**.

Restart Jellyfin

---

## 🎨 Uploading Your Custom Logo

### Access Plugin Configuration:

- Go to `Plugins` > `Custom Logo`.

### Upload Logo:

- Use the provided interface to upload your custom logo image.

⚠️ Ensure the image is in **PNG format**.

- Save
---

## Troubleshooting

## The logo doesn't change
Try to clear your browsers cache

Firefox : https://support.mozilla.org/en-US/kb/how-clear-firefox-cache
Chrome : https://support.google.com/accounts/answer/32050?hl=en&co=GENIE.Platform%3DDesktop

For mobile apps, try restarting the app or clearing its cache.

## Docker Support

✅ **This plugin fully supports Docker installations!**

The plugin:
1. Stores logos in the plugin data directory (`/config/plugins/configurations/CustomLogo/`) which is always writable
2. Automatically intercepts requests to `/web/assets/img/icon-transparent.png`, `/web/assets/img/banner-dark.png`, and `/web/assets/img/banner-light.png`
3. Serves your custom logo when available, or falls back to the original

No additional configuration or JavaScript injection needed - it just works!
