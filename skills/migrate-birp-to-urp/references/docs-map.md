# Docs map

Unity Manual pages for the Built-in to URP migration. Each entry gives the online `.md` page and the local file name.

Read each page's local copy from `<editor-install>/Editor/Data/Documentation/en/Manual/<local file>` (or `en/ScriptReference/` for API pages) when it exists, otherwise the online `.md` page. Replace `<VERSION>` with the project's Unity version, e.g. `6000.3`.

If a page isn't found, fetch https://docs.unity.com/en-us/engine/<VERSION>/manual.md and search it for the page title.

- [Upgrading from the Built-in Render Pipeline to URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp.md) — local `urp/upgrading-from-birp.html`: the overall upgrade order (install URP, assign assets, convert).
- [Install URP into an existing project](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/creating-a-urp-project/install-urpinto-aproject.md) — local `urp/InstallURPIntoAProject.html`: creating the URP asset and assigning it in Graphics and Quality settings.
- [Convert assets using the Render Pipeline Converter](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/rp-converter.md) — local `urp/features/rp-converter.html`: how to open the converter, the conversion types, and what each converter does.
- [Upgrade material assets to URP or HDRP](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/materials/upgrade-material.md) — local `upgrade-material.html`: converting selected materials and the Built-in to URP shader mappings.
- [Upgrade custom shaders for URP compatibility](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/upgrade-shaders/birp-urp-custom-shader-upgrade-guide.md) — local `urp/urp-shaders/birp-urp-custom-shader-upgrade-guide.html`: hand-porting Built-in shaders.
- [Prepare and upgrade sprites for 2D lighting in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-urp/2d-index/prep-shader.md) — local `urp/PrepShader.html`: sprite materials that react to 2D lights.
- [Find Built-in quality settings in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/quality-settings-location.md) — local `urp/birp-onboarding/quality-settings-location.html`: where each Built-in quality setting lives in URP.
- [Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md) — local `urp/Volumes.html`: URP Volume and Volume Profile setup that replaces PPv2.
- [Reflection probes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes.md) — local `urp/lighting/reflection-probes.html`: how reflection probes work in URP.
- [Render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) — local `render-pipelines-feature-comparison.html`: Built-in features that URP lacks or handles differently.
