using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hardness of each of the block types is saved in this class.
/// </summary>
public static class Block {
    static float[] blockHardness = { .5f, 1, 1.5f, 0, 2.5f };       // time it takes for player to destroy the block

    public static float GetHardness(BlockType blockType) {
        if (blockHardness.Length-1 < (int)blockType)
            return 0;
        else return blockHardness[(int)blockType];
    }
}

public enum BlockType {
    Grass=0,
    Sand,
    Snow,
    Water,
    Stone,
    None
}
