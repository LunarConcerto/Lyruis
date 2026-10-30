using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LunarConcerto.Lyruis.Tests
{
    public class LayoutTests
    {
        private GameObject root;
        private LyruisLayout layout;
        [SetUp] public void Setup()
        {
            root = new GameObject("Layout test", typeof(RectTransform));
            layout = root.AddComponent<LyruisLayout>();
            layout.WidthMode = SizeMode.Fixed; layout.FixedSize = new Vector2(100, 100);
            layout.HeightMode = SizeMode.Content; layout.Spacing = new Vector2(5, 5);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
        private RectTransform Child(float w, float h)
        {
            var go = new GameObject("Child", typeof(RectTransform), typeof(LayoutElement)); go.transform.SetParent(root.transform, false);
            var e = go.GetComponent<LayoutElement>(); e.preferredWidth = w; e.preferredHeight = h;
            return (RectTransform)go.transform;
        }
        [Test] public void VerticalFitsAndAligns()
        {
            Child(20, 10); var second = Child(30, 15); layout.RebuildNow();
            Assert.AreEqual(30, ((RectTransform)root.transform).rect.height, .01);
            Assert.AreEqual(-22.5f, second.anchoredPosition.y, .01);
        }
        [Test] public void FlowWrapsAtAvailableWidth()
        {
            layout.Mode = LayoutMode.Flow; Child(60, 10); var second = Child(60, 20); layout.RebuildNow();
            Assert.AreEqual(35, ((RectTransform)root.transform).rect.height, .01);
            Assert.AreEqual(-25, second.anchoredPosition.y, .01);
        }
        [Test] public void InactiveAndIgnoredChildrenAreExcluded()
        {
            Child(20, 10); Child(100, 100).gameObject.SetActive(false);
            Child(100, 100).GetComponent<LayoutElement>().ignoreLayout = true; layout.RebuildNow();
            Assert.AreEqual(10, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void GridUsesCellsAndPartialRow()
        {
            layout.Mode = LayoutMode.Grid; layout.GridCount = 2; layout.CellSize = new Vector2(30, 20);
            Child(1, 1); Child(1, 1); var last = Child(1, 1); layout.RebuildNow();
            Assert.AreEqual(new Vector2(30, 20), last.rect.size);
            Assert.AreEqual(45, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void NestedContentPropagates()
        {
            var container = new GameObject("Nested", typeof(RectTransform), typeof(LyruisLayout)); container.transform.SetParent(root.transform, false);
            var nested = container.GetComponent<LyruisLayout>(); nested.WidthMode = nested.HeightMode = SizeMode.Content;
            var leaf = new GameObject("Leaf", typeof(RectTransform), typeof(LayoutElement)); leaf.transform.SetParent(container.transform, false);
            leaf.GetComponent<LayoutElement>().preferredHeight = 25; layout.RebuildNow();
            Assert.AreEqual(25, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void EmptyContentRetainsPadding()
        {
            layout.padding = new RectOffset(2, 3, 4, 5); layout.RebuildNow();
            Assert.AreEqual(9, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void LeafCanMeasureItsOwnPreferredSizeProvider()
        {
            root.AddComponent<LayoutElement>().preferredHeight = 42; layout.RebuildNow();
            Assert.AreEqual(42, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void VerticalFlowWrapsIntoColumns()
        {
            layout.Mode = LayoutMode.Flow; layout.Axis = FlowAxis.Vertical;
            layout.WidthMode = SizeMode.Content; layout.HeightMode = SizeMode.Fixed;
            layout.FixedSize = new Vector2(0, 30); Child(20, 20); var last = Child(30, 20); layout.RebuildNow();
            Assert.AreEqual(55, ((RectTransform)root.transform).rect.width, .01);
            Assert.AreEqual(40, last.anchoredPosition.x, .01);
        }
        [Test] public void ContentMainAxisDoesNotWrap()
        {
            layout.Mode = LayoutMode.Flow; layout.WidthMode = SizeMode.Content;
            Child(80, 20); Child(80, 20); layout.RebuildNow();
            Assert.AreEqual(165, ((RectTransform)root.transform).rect.width, .01);
            Assert.AreEqual(20, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void BottomRightAlignmentRespectsPadding()
        {
            layout.HeightMode = SizeMode.Fixed; layout.childAlignment = TextAnchor.LowerRight;
            layout.padding = new RectOffset(2, 3, 4, 5); var item = Child(20, 10); layout.RebuildNow();
            Assert.AreEqual(new Vector2(87, -90), item.anchoredPosition);
        }
        [Test] public void RemovingChildUpdatesContent()
        {
            Child(20, 10); var removed = Child(20, 20); layout.RebuildNow();
            Object.DestroyImmediate(removed.gameObject); layout.RebuildNow();
            Assert.AreEqual(10, ((RectTransform)root.transform).rect.height, .01);
        }
    }
}
