<p align="center">
  <a href="https://facerec.gjung.com/">
    <img
      alt="logo"
      src="/media/logo-filled.svg"
      width="64"
    />
  </a>
</p>

# Interactive Face Recognition Tutorial

[![facerec.gjung.com](https://img.shields.io/badge/Web_App-facerec.gjung.com-blue)](https://facerec.gjung.com)
[![Google Play](https://img.shields.io/badge/Android-Google_Play-blue?logo=google-play&link=https%3A%2F%2Fplay.google.com%2Fstore%2Fapps%2Fdetails%3Fid%3Dcom.gjung.blazorface.maui%26utm_source%3Dgithub-badge)](https://play.google.com/store/apps/details?id=com.gjung.blazorface.maui&utm_source=github-badge)
[![Microsoft Store](https://img.shields.io/badge/Windows-Microsoft_Store-blue?logo=microsoft&link=https%3A%2F%2Fapps.microsoft.com%2Fstore%2Fdetail%2F9NM3NZ1MQ394%3Flaunch%3Dtrue%26cid%3Dgithub-badge%26mode%3Dmini)](https://apps.microsoft.com/store/detail/9NM3NZ1MQ394?launch=true&cid=github-badge&mode=mini)

Understand how modern technology works with faces, from scratch. You don't need to be an AI expert. If you know how to program, you will be able to apply state-of-the-art methods yourself, locally on your device and for free. This is an interactive tutorial – you can bring your own pictures if you like to.

<p float="left" align="middle">
  <img src="/doc/screenshot-alignment.png" width="32%" />
  <img src="/doc/screenshot-dimred.png" width="32%" />
  <img src="/doc/screenshot-compare.png" width="32%" />
</p>

## Features

* Interactively learn how facial recognition works.
* Use your own images _OR_ get started right away with sample images.
* Apply advanced face AI on your own machine.
* Follow along with hands-on code examples.
* Understand how to use AI with .NET and Blazor.
* Experiment to see how parameters change results.
* Apply what you learn to your own production applications.
* Free, local, no dependency on cloud services.
* Uses the MIT-licensed [FaceAiSharp](https://github.com/georg-jung/FaceAiSharp) library.

## Get started

Get started using the [web app facerec.gjung.com](https://facerec.gjung.com/) or by downloading this tutorial as an app:

<table>
    <tr>
        <td>
            <a href='https://play.google.com/store/apps/details?id=com.gjung.blazorface.maui&utm_source=github&pcampaignid=pcampaignidMKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1'>
                <img style="height: 76px;" alt='Get it on Google Play' src='/media/badges/google-play/en_badge_web_generic.png' />
            </a>
        </td>
        <td>
            <a href="https://apps.microsoft.com/store/detail/9NM3NZ1MQ394?launch=true&cid=github&mode=mini">
                <img style="height: 50px;" alt="Get it from Microsoft" src="/media/badges/microsoft-store/en-us-dark.svg" />
            </a>
        </td>
    </tr>
</table>

Google Play and the Google Play logo are trademarks of Google LLC.

### Using Docker

```bash
docker run -p 8080:8080 ghcr.io/georg-jung/explain-face-rec:latest
```

As soon as your container is running, you can access the tutorial at [localhost:8080](http://localhost:8080).

### Building the MAUI apps

The native apps use stable .NET 10 and MAUI 10. Android targets Android 16 (API 36) and supports Android 7.0 (API 24) or newer; Windows targets the Windows 10 SDK 19041. Install a stable .NET 10 SDK compatible with `global.json`, and use Git LFS to retrieve the sample images (`git lfs pull`). The AI models are restored through NuGet.

For Android, install Microsoft OpenJDK 21 and set `JAVA_HOME` and `ANDROID_HOME` to your JDK and Android SDK directories. Then run from the repository root:

```sh
dotnet workload install maui-android
dotnet build src/BlazorFace.Maui/BlazorFace.Maui.csproj -t:InstallAndroidDependencies -f net10.0-android36.0 -p:MauiTargetFrameworks=net10.0-android36.0 -p:AcceptAndroidSdkLicenses=true
dotnet build src/BlazorFace.Maui/BlazorFace.Maui.csproj -f net10.0-android36.0 -p:MauiTargetFrameworks=net10.0-android36.0 -c Release
```

`InstallAndroidDependencies` installs the SDK components required by the project's target API. See Microsoft's [Android dependency setup](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies) for initial SDK installation and custom paths.

After building Android in Release, check the packaged web UI, stylesheets, fonts, example images and AI models with Python 3:

```sh
python3 scripts/verify-android-assets.py bin/BlazorFace.Maui/Release/net10.0-android36.0/com.gjung.blazorface.maui.aab
```

CI runs this check before release signing to catch missing assets that would break the installed app.

On Windows, install the Windows development tools and SDK with Visual Studio's .NET MAUI workload, then build just the Windows target:

```powershell
dotnet workload install maui-windows
dotnet build src/BlazorFace.Maui/BlazorFace.Maui.csproj -f net10.0-windows10.0.19041.0 -p:MauiTargetFrameworks=net10.0-windows10.0.19041.0 -p:NoAndroid=true -c Release
```

`MauiTargetFrameworks` selects the app platform without overriding the frameworks of its referenced projects. `NoAndroid=true` also skips the shared library's Android target when building on Windows.

The MAUI workflow validates pull requests and manual runs without production signing credentials. Published GitHub releases additionally produce a signed Android App Bundle using the existing `KEYSTORE_FILE_BASE64` and `KEYSTORE_PASSWORD` secrets and key alias `key`. Passwords use temporary files with the `file:` syntax supported by [AAB signing](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0). Windows builds produce unsigned packages for Store submission. Artifacts are attached to the workflow run; the workflow does not submit apps to either store.

Nerdbank.GitVersioning derives Android's version code from `version.json` and Git history. Before submitting the signed AAB to Google Play, confirm its version code exceeds the published version, test it on Android 16 (including image selection, system bars, keyboard and navigation), and check Play Console's native-library/16 KB page-size validation. The target-API warning is cleared only after a compliant production release is accepted.

## Credits

Created in the context of Georg Jung's master thesis, supervised by [Dr. Guido Rößling](https://www.roessling.com/). Thanks for the supervision and the great support!
