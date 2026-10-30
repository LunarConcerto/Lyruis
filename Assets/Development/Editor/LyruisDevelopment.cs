using System.IO;
using LunarConcerto.Lyruis;
using LunarConcerto.Lyruis.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LyruisDevelopment
{
    [MenuItem("Tools/Lyruis/Generate Sample Scenes")]
    public static void Generate()
    {
        const string galleryPath = "Assets/Samples/Lyruis/0.1.0/Gallery";
        const string benchmarkPath = "Assets/Samples/Lyruis/0.1.0/Benchmark";
        Directory.CreateDirectory(galleryPath); Directory.CreateDirectory(benchmarkPath);
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var canvas = new GameObject("Lyruis Gallery", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 900);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var modes = new[] { LayoutMode.Horizontal, LayoutMode.Vertical, LayoutMode.Grid, LayoutMode.Flow, LayoutMode.Overlay, LayoutMode.Vertical };
        for (int p = 0; p < modes.Length; p++)
        {
            var panel = new GameObject(p == 5 ? "Nested" : modes[p].ToString(), typeof(RectTransform), typeof(Image), typeof(LyruisLayout));
            panel.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(30 + p % 2 * 620, -60 - p / 2 * 280); rect.sizeDelta = new Vector2(590, 230);
            panel.GetComponent<Image>().color = new Color(.08f, .11f, .18f);
            var layout = panel.GetComponent<LyruisLayout>(); layout.Mode = modes[p]; layout.padding = new RectOffset(16, 16, 16, 16);
            layout.CellSize = new Vector2(100, 54); layout.GridCount = 4;
            var title = new GameObject("Title", typeof(RectTransform), typeof(Text)); title.transform.SetParent(canvas.transform, false);
            var tr = (RectTransform)title.transform; tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0, 1);
            tr.anchoredPosition = rect.anchoredPosition + new Vector2(0, 28); tr.sizeDelta = new Vector2(590, 28);
            var label = title.GetComponent<Text>(); label.font = font; label.fontSize = 20; label.text = p == 5 ? "Nested content sizing" : modes[p].ToString();
            for (int i = 0; i < (p == 4 ? 1 : 7); i++)
            {
                Transform parent = panel.transform;
                if (p == 5)
                {
                    if (i >= 3) break;
                    var row = new GameObject("Row", typeof(RectTransform), typeof(LyruisLayout)); row.transform.SetParent(parent, false);
                    var r = row.GetComponent<LyruisLayout>(); r.Mode = LayoutMode.Horizontal; r.WidthMode = r.HeightMode = SizeMode.Content;
                    parent = row.transform;
                }
                int n = p == 5 ? 3 : 1;
                for (int j = 0; j < n; j++)
                {
                    var item = new GameObject("Item " + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement)); item.transform.SetParent(parent, false);
                    item.GetComponent<Image>().color = Color.HSVToRGB((i * .11f + j * .15f) % 1, .55f, .9f);
                    var e = item.GetComponent<LayoutElement>(); e.preferredWidth = p == 3 ? 100 + i % 3 * 35 : 60; e.preferredHeight = p == 1 ? 20 : 40;
                    var itemLayout = item.AddComponent<LyruisLayout>(); itemLayout.WidthMode = itemLayout.HeightMode = SizeMode.Content;
                }
            }
            layout.RebuildNow();
        }
        EditorSceneManager.SaveScene(scene, galleryPath + "/Gallery.unity");
        EditorSceneManager.CloseScene(scene, true);
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        new GameObject("Benchmark - enter Play mode", typeof(LyruisBenchmark));
        EditorSceneManager.SaveScene(scene, benchmarkPath + "/Benchmark.unity"); EditorSceneManager.CloseScene(scene, true);
        SceneManager.SetActiveScene(previous); AssetDatabase.Refresh();
    }
    [MenuItem("Tools/Lyruis/Run Benchmark And Save Report")]
    public static void Benchmark()
    {
        Directory.CreateDirectory("BenchmarkResults");
        File.WriteAllText("BenchmarkResults/editor-2022.3.json", LyruisBenchmark.RunSuite());
        Debug.Log("Benchmark saved to BenchmarkResults/editor-2022.3.json");
    }
    [MenuItem("Tools/Lyruis/Run Nested Benchmark And Save Report")]
    public static void NestedBenchmark()
    {
        Directory.CreateDirectory("BenchmarkResults");
        File.WriteAllText("BenchmarkResults/nested-editor-2022.3.json", NestedLayoutBenchmark.RunSuite());
        Debug.Log("Nested benchmark saved to BenchmarkResults/nested-editor-2022.3.json");
    }
}
