using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis.Tests
{
    public class SchedulingTests
    {
        private GameObject canvas;
        private LyruisLayout root;
        [SetUp] public void Setup()
        {
            canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(canvas.transform, false);
            root = panel.AddComponent<LyruisLayout>(); root.WidthMode = SizeMode.Fixed; root.HeightMode = SizeMode.Content;
            var item = new GameObject("Item", typeof(RectTransform)); item.transform.SetParent(panel.transform, false);
            var layout = item.AddComponent<LyruisLayout>(); layout.WidthMode = layout.HeightMode = SizeMode.Content;
            // Intrinsic provider is added after the component has queued its first update.
            var preferred = item.AddComponent<LayoutElement>(); preferred.preferredWidth = 22; preferred.preferredHeight = 13;
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(canvas);
        [UnityTest] public IEnumerator DeferredLayoutReadsActivatedProviders()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(13, root.RectTransform.sizeDelta.y, .01);
            Assert.AreEqual(new Vector2(22, 13), ((RectTransform)root.transform.GetChild(0)).sizeDelta);
        }
        [UnityTest] public IEnumerator RequestsFromSameTreeAreCoalesced()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            int completed = 0; root.OnLayoutRefreshed += () => completed++;
            for (int i = 0; i < 10; i++) { root.Refresh(); root.transform.GetChild(0).GetComponent<LyruisLayout>().Refresh(); }
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(1, completed);
        }
    }
}
