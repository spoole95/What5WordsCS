using NUnit.Framework;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Runtime.Intrinsics.X86;

namespace What5WordsTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TestCase("a", 1 << 0)]
        [TestCase("b", 1 << 1)]
        [TestCase("c", 1 << 2)]
        [TestCase("d", 1 << 3)]
        [TestCase("e", 1 << 4)]
        [TestCase("f", 1 << 5)]
        [TestCase("g", 1 << 6)]
        [TestCase("z", 1 << 25)]
        [TestCase("hello", 1 << 7 | 1 << 4 | 1 << 11 | 1 << 11 | 1 << 14)]
        [TestCase("japyx", 25199105)]
        public void Test1(string word, int expectedResult)
        {
            var result = GetBits(word);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [TestCase("hello", true)]
        [TestCase("house", false)]
        public void Test2(string word, bool expectedResult)
        {
            var bits = GetBits(word);
            var result = Popcnt.PopCount(bits) != word.Length;

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [TestCase((uint)20491, (uint)7)]// "abdom"//countr_zero = 0
        [TestCase((uint)4212739, (uint)6)]
        [TestCase((uint)2113555, (uint)4)]
        [TestCase((uint)16777245, (uint)13)]
        [TestCase((uint)157, (uint)12)]
        public void Test3(uint wordBits, uint expectedMin)
        {
            var reverseLetterOrder = new int[] { 25, 7, 14, 13, 24, 5, 9, 12, 22, 2, 8, 17, 11, 18, 21, 10, 0, 20, 24, 16, 19, 4, 6, 1, 15, 3 };

            // build index based on least used letter
            uint m = wordBits;
            int letter = BitOperations.TrailingZeroCount(m);
            int min = reverseLetterOrder[letter];

            m &= m - 1; //Drop lowest set bit
            while (m > 0)
            {
                letter = BitOperations.TrailingZeroCount(m);
                min = Math.Min(min, reverseLetterOrder[letter]);
                m &= m - 1;
            }

            Assert.That(expectedMin, Is.EqualTo(min));
        }


        /// <summary>
        /// Returns the bit representation of the word with each bit representing a letter
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private static uint GetBits(string word)
        {
            uint r = 0;
            foreach (var c in word)
            {
                r |= (uint)1 << (c - 'a');
            }
            return r;
        }
    }
}