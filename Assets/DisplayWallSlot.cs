using UnityEngine;

public class DisplayWallSlot : MonoBehaviour
{
    [SerializeField] private int slotId;

    /// <summary>
    /// The slot ID used to identify this slot on a DisplayWall.
    /// </summary>
    public int SlotId => slotId;
}
