using System.Collections.Generic;
using UnityEngine;

namespace LunarConcerto.Lyruis
{
    // Requests coalesce at the root; no LayoutRebuilder traversal is involved.
    internal static class LayoutScheduler
    {
        private static readonly HashSet<LyruisLayout> Pending = new HashSet<LyruisLayout>();
        private static readonly HashSet<LyruisLayout> Roots = new HashSet<LyruisLayout>();
        private static readonly List<LyruisLayout> Batch = new List<LyruisLayout>();
        private static bool attached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SceneReady()
        {
            // Scene deserialization can flush editor requests before intrinsic providers are enabled.
            // Requeue roots after all scene components have completed their initial activation.
            foreach (var node in Object.FindObjectsOfType<LyruisLayout>())
                if (node.isActiveAndEnabled && node.Root == node) Request(node);
        }
        internal static void Request(LyruisLayout node)
        {
            Pending.Add(node);
            if (attached) return;
            attached = true;
            Canvas.willRenderCanvases += Flush;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += Flush;
#endif
        }
        private static void Flush()
        {
            using (LyruisDiagnostics.Coarse(LyruisDiagnostics.Flush, LyruisDiagnostics.Phase.FlushRequests)) FlushCore();
        }
        private static void FlushCore()
        {
            Canvas.willRenderCanvases -= Flush;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= Flush;
#endif
            attached = false;
            Roots.Clear();
            foreach (var node in Pending) if (node && node.isActiveAndEnabled) Roots.Add(node.Root);
            Pending.Clear();
            Batch.Clear(); Batch.AddRange(Roots);
            // Clear the queue before callbacks so changes requested by clients survive to the next flush.
            for (int i = 0; i < Batch.Count; i++) if (Batch[i] && Batch[i].isActiveAndEnabled) LayoutTree.Rebuild(Batch[i]);
        }
    }
}
