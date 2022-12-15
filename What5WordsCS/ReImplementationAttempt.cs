using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace What5WordsCS
{
    internal class ReImplementationAttempt
    {
        private static Dictionary<uint, string> AllWords = new Dictionary<uint, string>();
        private static int[] LetterOrder = new int[26];
        private static List<uint>[] LetterIndex = new List<uint>[26];

        private static List<State> Queue = new List<State>();

        private struct Freq
        {
            public int f;
            public int l;
        }

        private struct State
        {
            public uint Total;
            public uint[] Words;
            public uint MaxLetter;
            public bool Skipped;
            public bool End;
        }
        //13174

        public static void Run()
        {
            var start = DateTime.Now;
            ReadWords("words_alpha.txt", 5);

            var startAlgo = DateTime.Now;
            var solutions = new List<uint[]>();

            var serial = false;
            FindWords(solutions, 0, new uint[5], 0, false, !serial);

            if (!serial)
            {
                Parallel.ForEach(Queue, new ParallelOptions { },
                    (state) => FindWords(solutions, state.Total, state.Words, state.MaxLetter, state.Skipped, true));
            }

            var startOut = DateTime.Now;

            const bool output = true;
            if (output)
            {
                foreach (var solution in solutions)
                {
                    Console.WriteLine($"{AllWords[solution[0]]}, {AllWords[solution[1]]}, {AllWords[solution[2]]}, {AllWords[solution[3]]}, {AllWords[solution[4]]}");
                }
            }

            var end = DateTime.Now;
            Console.WriteLine(solutions.Count + " solutions found"); // 538
            Console.WriteLine($"Total time: {(end - start).TotalMilliseconds}ms ({(end - start).TotalSeconds}s)"); //cpp 33.2548ms (0.332548s), c# standard 1.99s
            Console.WriteLine($"Read: {(startAlgo - start).TotalMilliseconds}ms"); // 5.6522ms, c# standard 84.03ms
            Console.WriteLine($"Process: {(startOut - startAlgo).TotalMilliseconds}ms"); // 4.1249ms, c# standard 678.4, c# paraell 260.84ms
            Console.WriteLine($"Write: {(end - startOut).TotalMilliseconds}ms"); // 23.4777ms, c# standard 1229.4ms
        }

        private static void FindWords(List<uint[]> solutions, uint total, uint[] currentWords, uint maxLetter, bool skipped, bool force = false)
        {
            var words = currentWords.Count(x => x != 0);
            if (words == 5)
            {
                solutions.Add(currentWords);
                return;
            }

            if (!force && words == 1)
            {
                Queue.Add(new State
                {
                    Total = total,
                    Words = currentWords,
                    MaxLetter = maxLetter,
                    Skipped = skipped
                });
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
                {
                    if ((total & w) > 0)
                    {
                        //Letter of word w is used already
                        continue;
                    }

                    //Add this attempt to the list
                    currentWords[words] = w;

                    var word = AllWords[w];

                    //Recusive to find word that now satisfies all words in currentWords, if none found then this exits
                    FindWords(solutions, total | w, (uint[])currentWords.Clone(), i + 1, skipped);
                }


                if (skipped) break;
                skipped = true; //Skip all future if not found a result on the first pass. Limit
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
            var reverseLetterFrequencyOrder = Freq.OrderBy(x => x.f).ToArray();
            var reverseLetterOrder = new int[26];
            for (var i = 0; i < reverseLetterFrequencyOrder.Count(); i++)
            {
                LetterOrder[i] = reverseLetterFrequencyOrder[i].l;
                reverseLetterOrder[reverseLetterFrequencyOrder[i].l] = i;
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
