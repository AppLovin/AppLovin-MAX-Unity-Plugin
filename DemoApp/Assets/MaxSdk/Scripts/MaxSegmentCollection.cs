using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains a collection of <see cref="MaxSegment"/> objects.
/// </summary>
[Serializable]
public class MaxSegmentCollection
{
    [SerializeField] private List<MaxSegment> segments;

    private MaxSegmentCollection(MaxSegmentCollectionBuilder maxSegmentCollectionBuilder)
    {
        segments = maxSegmentCollectionBuilder.segments;
    }

    /// <returns>The list of <see cref="MaxSegment"/> in the <see cref="MaxSegmentCollection"/></returns>
    public List<MaxSegment> GetSegments()
    {
        return segments;
    }

    public static MaxSegmentCollectionBuilder Builder()
    {
        return new MaxSegmentCollectionBuilder();
    }

    /// <summary>
    /// Builder class for MaxSegmentCollection.
    /// </summary>
    public class MaxSegmentCollectionBuilder
    {
        internal readonly List<MaxSegment> segments = new List<MaxSegment>();

        internal MaxSegmentCollectionBuilder() { }

        /// <summary>
        /// Adds a MaxSegment to the collection.
        /// </summary>
        /// <param name="segment">The MaxSegment to add.</param>
        /// <returns>The MaxSegmentCollectionBuilder instance for chaining.</returns>
        public MaxSegmentCollectionBuilder AddSegment(MaxSegment segment)
        {
            segments.Add(segment);
            return this;
        }

        /// <summary>
        /// Builds and returns the MaxSegmentCollection.
        /// </summary>
        /// <returns>The constructed MaxSegmentCollection.</returns>
        public MaxSegmentCollection Build()
        {
            return new MaxSegmentCollection(this);
        }
    }
}

/// <summary>
/// This class encapsulates a key-value pair, where the key is an int and the value is a List&lt;int&gt;.
/// </summary>
[Serializable]
public class MaxSegment
{
    [SerializeField] private int key;
    [SerializeField] private List<int> values;

    /// <summary>
    /// Initializes a new <see cref="MaxSegment"/> with the specified key and value(s).
    /// </summary>
    /// <param name="key">The key of the segment. Must be a non-negative number in the range of [0, 32000].</param>
    /// <param name="values">The values(s) associated with the key. Each value must be a non-negative number in the range of [0, 32000].</param>
    public MaxSegment(int key, List<int> values)
    {
        this.key = key;
        this.values = values;
    }

    /// <returns>The key of the segment. Must be a non-negative number in the range of [0, 32000].</returns>
    public int GetKey()
    {
        return key;
    }

    /// <returns>The value(s) associated with the key. Each value must be a non-negative number in the range of [0, 32000].</returns>
    public List<int> GetValues()
    {
        return values;
    }
}
