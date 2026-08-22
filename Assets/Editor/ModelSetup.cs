using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MeowMeowDog.EditorTools
{
    /// <summary>
    /// 角色模型（Quaternius CC0 动物包）的导入配置：
    /// - Generic 骨骼 + 动画循环 + 剪辑改短名（DogArmature|Idle → Idle）
    /// - 为每个模型生成 AnimatorController 到 Resources/Anim，运行时按状态名 CrossFade
    /// </summary>
    public class ModelImportConfig : AssetPostprocessor
    {
        const string ModelDir = "Assets/Resources/Models";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelDir)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importCameras = false;
            importer.importLights = false;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(ModelDir)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                // "DogArmature|Idle" → "Idle"，方便运行时按名字播放
                int bar = clip.name.LastIndexOf('|');
                if (bar >= 0) clip.name = clip.name[(bar + 1)..];
                clip.loopTime = true;
            }
            importer.clipAnimations = clips;
        }
    }

    public static class ModelSetup
    {
        const string ModelDir = "Assets/Resources/Models";
        const string AnimDir = "Assets/Resources/Anim";

        [MenuItem("MeowMeowDog/生成角色 AnimatorController")]
        public static void CreateAnimatorControllers()
        {
            Directory.CreateDirectory(AnimDir);
            foreach (var fbx in Directory.GetFiles(ModelDir, "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(fbx);
                var clips = AssetDatabase.LoadAllAssetsAtPath(fbx.Replace('\\', '/'))
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToArray();
                if (clips.Length == 0)
                {
                    Debug.LogWarning($"[MMDog] {name}.fbx 里没有动画剪辑，跳过");
                    continue;
                }

                string ctrlPath = $"{AnimDir}/{name}.controller";
                AssetDatabase.DeleteAsset(ctrlPath);
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                var sm = ctrl.layers[0].stateMachine;
                foreach (var clip in clips)
                {
                    var state = sm.AddState(clip.name);
                    state.motion = clip;
                    // 默认状态：陆地动物用 Idle，鱼用 Swimming
                    if (clip.name is "Idle" or "Swimming") sm.defaultState = state;
                }
                Debug.Log($"[MMDog] 生成 {ctrlPath}（{string.Join(", ", clips.Select(c => c.name))}）");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
