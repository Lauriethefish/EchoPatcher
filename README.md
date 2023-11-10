# EchoPatcher

Simple command-line APK patching tool that installs [libr15loader](https://github.com/RedBrumbler/Libr15Loader) and makes a few small changes to the manifest.

The permissions `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE` and `MANAGE_EXTERNAL_STORAGE` are added to the manifest, and the `debuggable` flag is enabled on the `application` element.

## Usage
To patch an APK:

`echopatcher <apk path>`

To pull the current APK from the Quest, patch it, then reinstall it:

`echopatcher`