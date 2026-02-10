using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// FrameLayout groups a set of ArtworkFrame instances under a logical layout ID.
///
/// Usage:
/// - Add this script to a parent GameObject that either:
///   - already has an ArtworkFrame, or
///   - has children that each have an ArtworkFrame.
/// - Set <see cref="layoutId"/> (e.g. 1, 2, 3). This will be matched to the numeric part of the JSON layoutId (e.g. "layout_1").
/// - Click "Refresh Frames" in the inspector (or let Awake run) to populate the list.
///
/// Frames are collected and then sorted by their GameObject name in ascending order,
/// so children named "1", "2", "3" or "Frame_01", "Frame_02" will end up in
/// predictable order even if the scene hierarchy is changed.
/// </summary>
public class FrameLayout : MonoBehaviour
{
    [Header("Layout Info")]
    [Tooltip("Numeric layout identifier (e.g. 1, 2, 3). This will be matched to the numeric part of the JSON layoutId (e.g. 'layout_1').")]
    [SerializeField] private int layoutId;

    [Tooltip("The slot index this layout was placed at (set at runtime by ArtworkManagerNew).")]
    [SerializeField] private int startSlot = -1;

    [Header("Frames in this Layout")]
    [Tooltip("All ArtworkFrame components that belong to this layout, sorted by GameObject name.")]
    [SerializeField] private List<ArtworkFrame> frames = new List<ArtworkFrame>();

    /// <summary>
    /// The numeric layout identifier for this group of frames.
    /// </summary>
    public int LayoutId
    {
        get => layoutId;
        set => layoutId = value;
    }

    /// <summary>
    /// The slot index this layout was placed at on its parent DisplayWall.
    /// Set at runtime when the layout is instantiated. Returns -1 if not set.
    /// </summary>
    public int StartSlot
    {
        get => startSlot;
        set => startSlot = value;
    }

    /// <summary>
    /// Read-only view of the frames in this layout, sorted in ascending name order.
    /// </summary>
    public IReadOnlyList<ArtworkFrame> Frames => frames;

    private void Awake()
    {
        // Auto-populate on Awake as a convenience.
        if (frames == null || frames.Count == 0)
        {
            RefreshFrames();
        }
    }

    /// <summary>
    /// Rebuilds the list of ArtworkFrame references.
    /// If an ArtworkFrame exists on this GameObject, it is included.
    /// Otherwise all ArtworkFrames in the children are collected.
    /// The final list is sorted by GameObject.name ascending for stable ordering.
    /// </summary>
    public void RefreshFrames()
    {
        frames = new List<ArtworkFrame>();

        // 1) Check if this GameObject itself has an ArtworkFrame
        ArtworkFrame selfFrame = GetComponent<ArtworkFrame>();
        if (selfFrame != null)
        {
            frames.Add(selfFrame);
        }
        else
        {
            // 2) Otherwise, collect from children
            ArtworkFrame[] childFrames = GetComponentsInChildren<ArtworkFrame>(includeInactive: true);

            foreach (var f in childFrames)
            {
                if (f != null && !frames.Contains(f))
                {
                    frames.Add(f);
                }
            }
        }

        // Failsafe: sort by GameObject.name ascending, to match numbering like 1, 2, 3...
        frames = frames
            .Where(f => f != null)
            .OrderBy(f => f.name, StringComparer.Ordinal)
            .ToList();

        // Debug: log what we found
        #if UNITY_EDITOR
        var names = string.Join(", ", frames.Select(f => f != null ? f.name : "<null>"));
        Debug.Log($"[FrameLayout '{name}'] RefreshFrames found {frames.Count} frame(s): {names}");
        #endif
    }
}
