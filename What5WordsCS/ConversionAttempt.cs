using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;

namespace What5WordsCS
{
    internal class ConversionAttempt
    {
        private const int threadCount = 1;


        private static List<uint> WordBits = new List<uint>();
        private static Dictionary<uint, string> AllWords = new Dictionary<uint, string>();
        private static List<uint>[] LetterIndex = new List<uint>[26];
        private static int[] LetterOrder = new int[26];
        private static Dictionary<uint, uint> BitsToIndex = new Dictionary<uint, uint>();

        private static Mutex queuemutex;
        private static Queue<State> Queue = new Queue<State>();

        private struct State
        {
            public uint totalbits;
            public int numwords;
            public uint[] words;
            public int maxletter;
            public bool skipped;
            public bool stop;
        }

        private struct Freq
        {
            public int f;
            public int l;
        }

        public static void Run()
        {
            ReadWords("words_alpha.txt", 5);

            var solutions = new List<uint[]>();

            var num = FindWords(solutions);

            foreach (uint[] words in solutions)
            {
                foreach (uint word in words)
                {
                    Console.Write(AllWords.ElementAt((int)BitsToIndex[word]) + "\t");
                }
            }
        }

        private static int FindWords(List<uint[]> solutions)
        {
            var tasks = new List<Task>();
            //for (var i = 0; i < threadCount; i++)
            //{
            //    tasks.Add(Task.Run(() => { FindThread(solutions); }));
            //}
            uint[] words = new uint[5];
            FindWords(solutions, 0, 0, words, 0, false);
            for (uint i = 0; i < threadCount; i++)
            {
                Queue.Enqueue(new State { stop = true });
            }

            FindThread(solutions);
            //await Task.WhenAll(tasks);

            return solutions.Count();
        }

        private static void FindWords(List<uint[]> solutions, uint totalbits, int numwords, uint[] words, int maxLetter, bool skipped, bool force = false)
        {
            if (numwords == 5)
            {
                solutions.Add(words);
                return;
            }

            if (!force && numwords == 1)
            {
                //Lock?
                Queue.Enqueue(new State
                {
                    totalbits = totalbits,
                    numwords = numwords,
                    words = words,
                    maxletter = maxLetter,
                    skipped = skipped,
                    stop = false
                });
                //Unlock?

                //queueCondition.notify_onw
                return;
            }

            var max = WordBits.Count();

            //Walk over all letters in order until we find an unused one
            for (var i = maxLetter; i < 26; i++)
            {
                int letter = LetterOrder[i];
                uint m = (uint)(1 << (int)letter);
                if ((totalbits & m) > 0)
                {
                    //Letter used?
                    continue;
                }

                // take all words from the index of this letter and add each word to the solution if all letters of the word aren't used before.
                foreach (var w in LetterIndex[i] ?? new List<uint>())
                {

                    if ((totalbits & w) > 0)
                    {
                        //Letter used?
                        continue;
                    }

                    words[numwords] = w;
                    // Recusive call with the next letter (or word?)
                    FindWords(solutions, totalbits | w, numwords + 1, words, i + 1, skipped);
                }

                if (skipped)
                {
                    break;
                }
                skipped = true;
            }
        }

        private static void FindThread(List<uint[]> solutions)
        {
            List<uint[]> mySolutions = new List<uint[]>();

            //lock(queuemutex)
            //{
            while (true)
            {
                if (!Queue.Any())
                {
                    //Wait until queue is not empty
                    Thread.Sleep(10);
                }

                var state = Queue.Dequeue();

                if (state.stop)
                {
                    break;
                }
                FindWords(mySolutions, state.totalbits, state.numwords, state.words, state.maxletter, state.skipped, true);
            }
            //}

            solutions.AddRange(mySolutions);
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

                    if (!BitsToIndex.ContainsKey(bits))
                    {
                        BitsToIndex.Add(bits, (uint)WordBits.Count);
                        WordBits.Add(bits);
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
            foreach (uint w in WordBits)
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
                var charLetter = (char)(letter + 'a');
                var charMinLetter = (char)(min + 'a');


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
