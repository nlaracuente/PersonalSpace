using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A list of static utlity functions
/// </summary>
public static class Utility
{

    /// <summary>
    /// A collection of cardinal points to use when checking around a tile
    /// </summary>
    public static List<Vector2> FourCardinalPoints = new List<Vector2>()
    {
        new Vector2(0, 1),  // Up
        new Vector2(-1, 0), // Left
        new Vector2(0, -1), // Down
        new Vector2(1, 0),  // Right
    };

    /// <summary>
    /// A collection of the cardinal points that represent corners
    /// </summary>
    public static List<Vector2> CornerCardinalPoints = new List<Vector2>()
    {
        new Vector2(-1, 1),  // Up-left
        new Vector2(-1, -1), // Down-left
        new Vector2(1, -1),  // Down-right
        new Vector2(1, 1),   // Up-right
    };

    /// <summary>
    /// A collection of all available cardinal points
    /// </summary>
    public static List<Vector2> AllCardinalPoints {
        get {
            List<Vector2> points = new List<Vector2>();
            points.AddRange(FourCardinalPoints);
            points.AddRange(CornerCardinalPoints);
            return points;
        }
    }

    /// <summary>
    /// Takes an array and shuffles it contents using the Fisher-Yates method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="seed">Specify which seed to use for random operations</param>
    /// <returns></returns>
    public static T[] ShuffleArray<T>(T[] array, int seed)
    {
        System.Random prng = new System.Random(seed);

        // Loops through all the elements in the array swapping
        // A randomly chosen item from the array with the current
        // item we are iterating through
        for (int i = 0; i < array.Length - 1; i++)
        {
            // To prevent grabbing a previous element, the random range is
            // limited to the current index all the way to the end of the array
            int randomIndex = prng.Next(i, array.Length);

            // Save the item we want to shuffle
            T tempItem = array[randomIndex];

            // Swap them
            array[randomIndex] = array[i];
            array[i] = tempItem;
        } // for

        return array;
    } // ShuffleArray

    /// <summary>
    /// Iterates recursive through all children of the same type returning them
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="root"></param>
    /// <param name="children"></param>
    /// <returns></returns>
    public static IEnumerable<T> DepthFirstTreeTraversal<T>(T root, Func<T, IEnumerable<T>> children)
    {
        var stack = new Stack<T>();
        stack.Push(root);
        while (stack.Count != 0)
        {
            var current = stack.Pop();
            // If you don't care about maintaining child order then remove the Reverse.
            foreach (var child in children(current).Reverse())
                stack.Push(child);
            yield return current;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when coordinate A is adjacent to Coordinate B
    /// </summary>
    /// <param name="coordsA"></param>
    /// <param name="coordsB"></param>
    /// <returns></returns>
    public static bool CoordinatesAreAdjacent(Vector2 coordsA, Vector2 coordsB)
    {
        bool areAdjacent = false;

        foreach (Vector2 point in AllCardinalPoints)
        {
            Vector2 adjacentCoords = coordsB + point;
            if (coordsA == adjacentCoords)
            {
                areAdjacent = true;
                break;
            }
        };

        return areAdjacent;
    }
}