using UnityEditor;
using UnityEngine;

namespace Obfuz.Unity
{
    public static class ObfuzMenu
    {

        [MenuItem("Obfuz/Settings...", priority = 61)]
        public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/Obfuz");

        [MenuItem("Obfuz/Documents/Quick Start")]
        public static void OpenQuickStart() => Application.OpenURL("https://obfuz.doc.code-philosophy.com/docs/beginner/quickstart");

        [MenuItem("Obfuz/Documents/FAQ")]
        public static void OpenFAQ() => Application.OpenURL("https://obfuz.doc.code-philosophy.com/docs/help/faq");

        [MenuItem("Obfuz/Documents/Common Errors")]
        public static void OpenCommonErrors() => Application.OpenURL("https://obfuz.doc.code-philosophy.com/docs/help/commonerrors");

        [MenuItem("Obfuz/Documents/Bug Report")]
        public static void OpenBugReport() => Application.OpenURL("https://obfuz.doc.code-philosophy.com/docs/help/issue");

        [MenuItem("Obfuz/Documents/GitHub")]
        public static void OpenGitHub() => Application.OpenURL("https://github.com/focus-creative-games/obfuz");

        [MenuItem("Obfuz/Documents/Gitee")]
        public static void OpenGitee() => Application.OpenURL("https://gitee.com/focus-creative-games/obfuz");

        [MenuItem("Obfuz/Documents/About")]
        public static void OpenAbout() => Application.OpenURL("https://obfuz.doc.code-philosophy.com/docs/intro");
    }

}