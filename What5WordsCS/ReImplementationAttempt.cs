using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace What5WordsCS
{
    internal class ReImplementationAttempt
    {
        private static Dictionary<uint, string> AllWords = new Dictionary<uint, string>();
        private static int[] LetterOrder = new int[26];
        private static List<uint>[] LetterIndex = new List<uint>[26];

        private struct Freq
        {
            public int f;
            public int l;
        }

        public static void Run()
        {
            ReadWords("words_alpha.txt", 5);

            var solutions = new List<uint[]>();
            FindWords(solutions, 0, new uint[5], 0, false);

            Console.WriteLine(solutions.Count + " solutions found");
        }

        private static void FindWords(List<uint[]> solutions, uint total, uint[] currentWords, uint maxLetter, bool skipped)
        {
            var words = currentWords.Count(x => x != 0);
            if (words == 5)
            {
                Console.WriteLine($"Found solution: {AllWords[currentWords[0]]}, {AllWords[currentWords[1]]}, {AllWords[currentWords[2]]}, {AllWords[currentWords[3]]}, {AllWords[currentWords[4]]}");
                solutions.Add(currentWords);
                return;
            }

            //walk over all letters in reverse-used order until we find an unused one
            for (uint i = maxLetter; i < 26; i++)
            {
                int letter = LetterOrder[i];
                uint m = (uint)(1 << letter);
                if ((total & m) > 0)
                {
                    //Letter already used in the total
                    continue;
                }

                if (LetterIndex[i] == null)
                {
                    //No words starting with this letter, skip
                    continue;
                }

                foreach (uint w in LetterIndex[i])
                //Parallel.ForEach(LetterIndex[i], new ParallelOptions { MaxDegreeOfParallelism = 5 }, (w) =>
                {
                    if ((total & w) > 0)
                    {
                        //Letter of word w is used already
                        continue;
                        //return; // Parallel only
                    }

                    //Add this attempt to the list
                    currentWords[words] = w;

                    var word = AllWords[w];

                    //Recusive to find word that now satisfies all words in currentWords, if none found then this exits
                    FindWords(solutions, total | w, (uint[])currentWords.Clone(), i + 1, skipped);
                }
                //);
            }
        }

        private static void ReadWords(string file, int wordLength)
        {
            Freq[] Freq = new Freq[26];
            for (int i = 0; i < Freq.Length; i++)
            {
                Freq[i].l = i;
            }

            using (var reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var word = reader.ReadLine();
                    if (word.Length != wordLength)
                    {
                        continue;
                    }

                    var bits = GetBits(word);
                    //If more than one of the same letter in the word, reject
                    if (Popcnt.PopCount(bits) != wordLength)
                    {
                        continue;
                    }

                    if (!AllWords.ContainsKey(bits))
                    {
                        AllWords.Add(bits, word);

                        // count letter frequency
                        foreach (char c in word)
                        {
                            Freq[c - 'a'].f++;
                        }
                    }
                }
            }

            // rearrange letter order based on lettter frequency (least used letter gets lowest index)
            var reverseLetterFrequencyOrder = Freq.OrderByDescending(x => x.f).ToArray();
            var reverseLetterOrder = new int[26];
            for (var i = 0; i < reverseLetterFrequencyOrder.Count(); i++)
            {
                LetterOrder[i] = reverseLetterFrequencyOrder[i].l;
                reverseLetterOrder[Freq[i].l] = i;
            }

            // build index based on least used letter
            foreach (uint w in AllWords.Keys)
            {
                uint m = w;
                int letter = BitOperations.TrailingZeroCount(m);
                int min = reverseLetterOrder[letter];

                m &= m - 1; //Drop lowest set bit
                while (m > 0)
                {
                    letter = BitOperations.TrailingZeroCount(m);
                    min = Math.Min(min, reverseLetterOrder[letter]);
                    m &= m - 1;
                }

                if (LetterIndex[min] == null)
                {
                    LetterIndex[min] = new List<uint>();
                }
                LetterIndex[min].Add(w);
            }
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
