using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="BitConverterHelper"/>.
/// </summary>
[TestClass]
public class BitConverterHelperTests
{
    [TestMethod]
    public void GetElements_ValidOffsetAndCount_ReturnsExactSlice()
    {
        byte[] arr = [1, 2, 3, 4, 5];

        byte[] result = BitConverterHelper.GetElements(arr, 1, 3);

        CollectionAssert.AreEqual(new byte[] { 2, 3, 4 }, result);
    }

    [TestMethod]
    public void GetElements_NullArray_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitConverterHelper.GetElements<byte>(null!, 0, 1));
    }

    [TestMethod]
    public void GetElements_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.GetElements(arr, -1, 1));
    }

    [TestMethod]
    public void GetElements_OffsetEqualsLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.GetElements(arr, 3, 1));
    }

    [TestMethod]
    public void GetElements_CountNotPositive_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.GetElements(arr, 0, 0));
    }

    [TestMethod]
    public void GetElements_OffsetPlusCountExceedsLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.GetElements(arr, 2, 2));
    }

    [TestMethod]
    public void IsElementsEqual_EqualRanges_ReturnsTrue()
    {
        byte[] arr = [1, 2, 1, 2, 9];

        bool result = BitConverterHelper.IsElementsEqual(arr, 0, 2, 2);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsElementsEqual_DifferentRanges_ReturnsFalse()
    {
        byte[] arr = [1, 2, 3, 4];

        bool result = BitConverterHelper.IsElementsEqual(arr, 0, 2, 2);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsElementsEqual_SameOffsets_ReturnsTrue()
    {
        byte[] arr = [1, 2, 3];

        bool result = BitConverterHelper.IsElementsEqual(arr, 1, 1, 1);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsElementsEqual_NullArray_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitConverterHelper.IsElementsEqual<byte>(null!, 0, 0, 1));
    }

    [TestMethod]
    public void IsElementsEqual_BadOffset1_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.IsElementsEqual(arr, -1, 0, 1));
    }

    [TestMethod]
    public void IsElementsEqual_BadOffset2_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.IsElementsEqual(arr, 0, 5, 1));
    }

    /// <summary>
    /// Validation must run before the offset1 == offset2 short-circuit, so an invalid
    /// <paramref name="count"/> must still throw even when both offsets are identical.
    /// </summary>
    [TestMethod]
    public void IsElementsEqual_SameOffsetsButBadCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] arr = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitConverterHelper.IsElementsEqual(arr, 1, 1, 10));
    }

    [TestMethod]
    public void IsListsEqual_EqualLists_ReturnsTrue()
    {
        List<int> list1 = [1, 2, 3];
        List<int> list2 = [1, 2, 3];

        Assert.IsTrue(BitConverterHelper.IsListsEqual(list1, list2));
    }

    [TestMethod]
    public void IsListsEqual_BothNullAtSameIndex_ReturnsTrue()
    {
        List<string?> list1 = [null];
        List<string?> list2 = [null];

        Assert.IsTrue(BitConverterHelper.IsListsEqual(list1, list2));
    }

    [TestMethod]
    public void IsListsEqual_OneNullElement_ReturnsFalse()
    {
        List<string?> list1 = [null];
        List<string?> list2 = ["a"];

        Assert.IsFalse(BitConverterHelper.IsListsEqual(list1, list2));
    }

    [TestMethod]
    public void IsListsEqual_DifferentLengths_ReturnsFalse()
    {
        List<int> list1 = [1, 2];
        List<int> list2 = [1, 2, 3];

        Assert.IsFalse(BitConverterHelper.IsListsEqual(list1, list2));
    }

    [TestMethod]
    public void IsListsEqual_BothNullLists_ReturnsTrue()
    {
        List<int>? list1 = null;
        List<int>? list2 = null;

        Assert.IsTrue(BitConverterHelper.IsListsEqual(list1!, list2!));
    }

    [TestMethod]
    public void IsListsEqual_OneNullList_ReturnsFalse()
    {
        List<int>? list1 = null;
        List<int> list2 = [1];

        Assert.IsFalse(BitConverterHelper.IsListsEqual(list1!, list2));
    }

    [TestMethod]
    public void IsArraysEqual_EqualArrays_ReturnsTrue()
    {
        int[] array1 = [1, 2, 3];
        int[] array2 = [1, 2, 3];

        Assert.IsTrue(BitConverterHelper.IsArraysEqual(array1, array2));
    }

    [TestMethod]
    public void IsArraysEqual_DifferentArrays_ReturnsFalse()
    {
        int[] array1 = [1, 2, 3];
        int[] array2 = [1, 2, 4];

        Assert.IsFalse(BitConverterHelper.IsArraysEqual(array1, array2));
    }

    [TestMethod]
    public void IsArraysEqual_NullArrays_ReturnsTrue()
    {
        Assert.IsTrue(BitConverterHelper.IsArraysEqual<int>(null!, null!));
    }

    [TestMethod]
    public void IsArraysEqual_OneNullArray_ReturnsFalse()
    {
        int[] array2 = [1];

        Assert.IsFalse(BitConverterHelper.IsArraysEqual<int>(null!, array2));
    }
}
