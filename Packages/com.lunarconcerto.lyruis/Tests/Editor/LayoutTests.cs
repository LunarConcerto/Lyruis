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
            var item = go.AddComponent<LyruisLayout>(); item.WidthMode = item.HeightMode = SizeMode.Content;
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
        [Test] public void InactiveAndUnregisteredChildrenAreExcluded()
        {
            Child(20, 10); Child(100, 100).gameObject.SetActive(false);
            Object.DestroyImmediate(Child(100, 100).GetComponent<LyruisLayout>()); layout.RebuildNow();
            Assert.AreEqual(10, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void GridUsesCellsAndPartialRow()
        {
            layout.Mode = LayoutMode.Grid; layout.GridCount = 2; layout.CellSize = new Vector2(30, 20);
            Child(1, 1); Child(1, 1); var last = Child(1, 1); layout.RebuildNow();
            Assert.AreEqual(new Vector2(1, 1), last.rect.size);
            Assert.AreEqual(45, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void NestedContentPropagates()
        {
            var container = new GameObject("Nested", typeof(RectTransform), typeof(LyruisLayout)); container.transform.SetParent(root.transform, false);
            var nested = container.GetComponent<LyruisLayout>(); nested.WidthMode = nested.HeightMode = SizeMode.Content;
            var leaf = new GameObject("Leaf", typeof(RectTransform), typeof(LayoutElement)); leaf.transform.SetParent(container.transform, false);
            leaf.GetComponent<LayoutElement>().preferredHeight = 25;
            var item = leaf.AddComponent<LyruisLayout>(); item.WidthMode = item.HeightMode = SizeMode.Content; layout.RebuildNow();
            Assert.AreEqual(25, ((RectTransform)root.transform).rect.height, .01);
        }
        [Test] public void EmptyLeafReadsProviderWithoutContainerPadding()
        {
            layout.padding = new RectOffset(2, 3, 4, 5); layout.RebuildNow();
            Assert.AreEqual(0, ((RectTransform)root.transform).rect.height, .01);
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
        [Test] public void NonePreservesManualPositionsAndMeasuresBounds()
        {
            layout.Mode = LayoutMode.None; layout.WidthMode = SizeMode.Content;
            var child = Child(20, 10); child.anchoredPosition = new Vector2(30, 20);
            layout.RebuildNow();
            Assert.AreEqual(new Vector2(30, 20), child.anchoredPosition);
            Assert.AreEqual(new Vector2(40, 25), ((RectTransform)root.transform).sizeDelta);
        }
        [Test] public void FlowCentersEachLineAndItemsWithinLine()
        {
            layout.Mode = LayoutMode.Flow; layout.childAlignment = TextAnchor.MiddleCenter;
            Child(40, 10); var second = Child(40, 20); var last = Child(60, 10);
            layout.RebuildNow();
            // (100 - (40 + 5 + 40)) / 2 + 40 + 5 + pivot(20).
            Assert.AreEqual(72.5f, second.anchoredPosition.x, .01);
            Assert.AreEqual(50, last.anchoredPosition.x, .01);
            Assert.AreEqual(-30, last.anchoredPosition.y, .01);
        }
        [Test] public void GridZeroCellAndCountUseFirstChildAndTwoColumns()
        {
            layout.Mode = LayoutMode.Grid; layout.GridCount = 0; layout.CellSize = Vector2.zero;
            Child(30, 20); var next = Child(10, 8); var last = Child(12, 6);
            layout.RebuildNow();
            Assert.AreEqual(new Vector2(10, 8), next.sizeDelta);
            Assert.AreEqual(40, next.anchoredPosition.x, .01);
            Assert.AreEqual(-28, last.anchoredPosition.y, .01);
            Assert.AreEqual(45, ((RectTransform)root.transform).sizeDelta.y, .01);
        }
        [Test] public void KeepUsesSizeDeltaForStretchedRect()
        {
            var child = Child(10, 10); var item = child.GetComponent<LyruisLayout>();
            item.WidthMode = SizeMode.Keep; child.anchorMin = Vector2.zero; child.anchorMax = Vector2.one;
            child.sizeDelta = new Vector2(17, 10); layout.RebuildNow();
            Assert.AreEqual(17, child.sizeDelta.x, .01);
        }
        [Test] public void CompletionCallbacksRunChildrenBeforeParent()
        {
            var events = new System.Collections.Generic.List<int>();
            Child(10, 10).GetComponent<LyruisLayout>().OnLayoutRefreshed += () => events.Add(1);
            layout.OnLayoutRefreshed += () => events.Add(2); layout.RebuildNow();
            CollectionAssert.AreEqual(new[] { 1, 2 }, events);
        }
        [Test] public void CombinedWritesMatchLegacyInsetsForArbitraryPivotAndRestoreExternalEdits()
        {
            var item = Child(37, 23); item.pivot = new Vector2(.2f, .8f);
            layout.padding = new RectOffset(7, 3, 9, 5); layout.RebuildNow();
            Vector2 expected = new Vector2(7 + 37 * .2f, -9 - 23 * .2f);
            Assert.AreEqual(expected.x, item.anchoredPosition.x, .001);
            Assert.AreEqual(expected.y, item.anchoredPosition.y, .001);
            item.anchorMin = Vector2.zero; item.anchorMax = Vector2.one;
            item.sizeDelta = new Vector2(400, 300); item.anchoredPosition = new Vector2(100, 100);
            layout.RebuildNow();
            Assert.AreEqual(Vector2.up, item.anchorMin); Assert.AreEqual(Vector2.up, item.anchorMax);
            Assert.AreEqual(new Vector2(37, 23), item.sizeDelta);
            Assert.Less(Vector2.Distance(expected, item.anchoredPosition), .001);
        }
        [Test] public void RootCacheInvalidatesOnReparenting()
        {
            var other = new GameObject("Other root", typeof(RectTransform), typeof(LyruisLayout));
            try
            {
                var leaf = Child(20, 10).GetComponent<LyruisLayout>(); int first = 0, second = 0;
                layout.OnLayoutRefreshed += () => first++;
                other.GetComponent<LyruisLayout>().OnLayoutRefreshed += () => second++;
                leaf.RebuildRootNow(); Assert.AreEqual(1, first);
                leaf.transform.SetParent(other.transform, false); leaf.RebuildRootNow();
                Assert.AreEqual(1, first); Assert.AreEqual(1, second);
            }
            finally { Object.DestroyImmediate(other); }
        }
        [Test] public void RootCacheInvalidatesWhenParentComponentIsDisabledOrAdded()
        {
            var branch = new GameObject("Branch", typeof(RectTransform)); branch.transform.SetParent(root.transform, false);
            var leaf = Child(20, 10).GetComponent<LyruisLayout>(); leaf.transform.SetParent(branch.transform, false);
            int completed = 0; layout.OnLayoutRefreshed += () => completed++;
            leaf.RebuildRootNow(); Assert.AreEqual(0, completed);
            var middle = branch.AddComponent<LyruisLayout>(); leaf.RebuildRootNow(); Assert.AreEqual(1, completed);
            middle.enabled = false; leaf.RebuildRootNow(); Assert.AreEqual(1, completed);
            middle.enabled = true; leaf.RebuildRootNow(); Assert.AreEqual(2, completed);
        }
    }
}
