using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RuralCafe
{
    // JJJ: Disabled
    // Class with all the functionality for loading N-Grams from input files,
    // and suggesting N-Grams for given search terms
    class NGrams
    {
        // input files
        static string _1gramFileName = "1gram_gt_500K.txt";
        static string _2gramFileName = "2gram_gt_200K.txt";
        static string _3gramFileName = "3gram_gt_100K.txt";
        static string _4gramFileName = "4gram_gt_50K.txt";

        // loaded N-Gram data
        static Dictionary<string, long> _LDCData1Gram;
        static Dictionary<string, Dictionary<string, long>> _LDCData2Gram;
        static Dictionary<string, Dictionary<string, Dictionary<string, long>>> _LDCData3Gram;
        static Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, long>>>> _LDCData4Gram;

        // methods for loading N-Grams
        public static void Load1Grams()
        {
            Console.Write("Loading LDC Data 1 Grams... ");
            // 1-GRAM
            // initialize data structures
            _LDCData1Gram = new Dictionary<string, long>();

            // open the file
            // Read the file and display it line by line
            int counter = 0;
            string line;
            string[] entry;
            long score;

            System.IO.StreamReader file =
               new System.IO.StreamReader(_1gramFileName);
            while ((line = file.ReadLine()) != null)
            {
                entry = ConvertNGramLineToEntry(line);
                if (entry.Length != 2)
                {
                    // error
                    Console.WriteLine("Error, bad entry: " + entry.Length + " terms");
                    break; //return;
                }
                try
                {
                    score = Int64.Parse(entry[1]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error, bad entry score: \"" + entry[1] + ", \"" + e.Message);
                    break; //return;
                }

                //Console.WriteLine(entry[0] + ": " + score);
                // add the entry
                _LDCData1Gram.Add(entry[0], score);

                counter++;
            }

            file.Close();

            Console.WriteLine("OK");
        }
        public static void Load2Grams()
        {
            Console.Write("Loading LDC Data 2 Grams... ");

            // 2-GRAM
            // initialize data structures
            _LDCData2Gram = new Dictionary<string, Dictionary<string, long>>();

            // open the file
            // Read the file and display it line by line
            int counter = 0;
            string line;
            string[] entry;
            long score;

            System.IO.StreamReader file =
               new System.IO.StreamReader(_2gramFileName);
            while ((line = file.ReadLine()) != null)
            {
                entry = ConvertNGramLineToEntry(line);
                if (entry.Length != 3)
                {
                    // error
                    Console.WriteLine("Error, bad entry: " + entry.Length + " terms");
                    break; //return;
                }
                try
                {
                    score = Int64.Parse(entry[2]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error, bad entry score: \"" + entry[2] + "\"" + e.Message);
                    break; //return;
                }

                //Console.WriteLine(entry[0] + " " + entry[1] + ": " + score);

                // check if the 1 term exists in the dictionary
                if (!_LDCData2Gram.ContainsKey(entry[0]))
                {
                    // add the 1st term
                    Dictionary<string, long> temp = new Dictionary<string, long>();
                    _LDCData2Gram.Add(entry[0], temp);
                }

                Dictionary<string, long> firstPart = _LDCData2Gram[entry[0]];

                // add the entry
                firstPart.Add(entry[1], score);

                counter++;
            }

            file.Close();

            Console.WriteLine("OK");
        }
        public static void Load3Grams()
        {
            Console.Write("Loading LDC Data 3 Grams... ");
            // 3-GRAM
            // initialize data structures
            _LDCData3Gram = new Dictionary<string, Dictionary<string, Dictionary<string, long>>>();

            // open the file
            // Read the file and display it line by line
            int counter = 0;
            string line;
            string[] entry;
            long score;

            System.IO.StreamReader file =
               new System.IO.StreamReader(_3gramFileName);
            while ((line = file.ReadLine()) != null)
            {
                entry = ConvertNGramLineToEntry(line);
                if (entry.Length != 4)
                {
                    // error
                    Console.WriteLine("Error, bad entry: " + entry.Length + " terms");
                    break; //return;
                }
                try
                {
                    score = Int64.Parse(entry[3]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error, bad entry score: \"" + entry[3] + "\"" + e.Message);
                    break; //return;
                }

                //Console.WriteLine(entry[0] + " " + entry[1] + " " + entry[2] + ": " + score);

                // check if the 1 term exists in the dictionary
                if (!_LDCData3Gram.ContainsKey(entry[0]))
                {
                    // add the 1st term
                    Dictionary<string, Dictionary<string, long>> temp = new Dictionary<string, Dictionary<string, long>>();
                    _LDCData3Gram.Add(entry[0], temp);
                }

                Dictionary<string, Dictionary<string, long>> firstPart = _LDCData3Gram[entry[0]];

                // check if the 2 term exists in the dictionary
                if (!firstPart.ContainsKey(entry[1]))
                {
                    // add the 1st term
                    Dictionary<string, long> temp = new Dictionary<string, long>();
                    firstPart.Add(entry[1], temp);
                }

                Dictionary<string, long> secondPart = _LDCData3Gram[entry[0]][entry[1]];

                // add the entry
                secondPart.Add(entry[2], score);

                counter++;
            }

            file.Close();

            Console.WriteLine("OK");
        }
        public static void Load4Grams()
        {
            Console.Write("Loading LDC Data 4 Grams... ");
            // 4-GRAM
            // initialize data structures
            _LDCData4Gram = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, long>>>>();

            // open the file
            // Read the file and display it line by line
            int counter = 0;
            string line;
            string[] entry;
            long score;

            System.IO.StreamReader file =
               new System.IO.StreamReader(_4gramFileName);
            while ((line = file.ReadLine()) != null)
            {
                entry = ConvertNGramLineToEntry(line);
                if (entry.Length != 5)
                {
                    // error
                    Console.WriteLine("Error, bad entry: " + entry.Length + " terms");
                    break; //return;
                }
                try
                {
                    score = Int64.Parse(entry[4]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error, bad entry score: \"" + entry[4] + "\"" + e.Message);
                    break; //return;
                }

                //Console.WriteLine(entry[0] + " " + entry[1] + " " + entry[2] + " " + entry[3] + ": " + score);

                // check if the 1 term exists in the dictionary
                if (!_LDCData4Gram.ContainsKey(entry[0]))
                {
                    // add the 1st term
                    Dictionary<string, Dictionary<string, Dictionary<string, long>>> temp = new Dictionary<string, Dictionary<string, Dictionary<string, long>>>();
                    _LDCData4Gram.Add(entry[0], temp);
                }

                Dictionary<string, Dictionary<string, Dictionary<string, long>>> firstPart = _LDCData4Gram[entry[0]];

                // check if the 2 term exists in the dictionary
                if (!firstPart.ContainsKey(entry[1]))
                {
                    // add the 1st term
                    Dictionary<string, Dictionary<string, long>> temp = new Dictionary<string, Dictionary<string, long>>();
                    firstPart.Add(entry[1], temp);
                }

                Dictionary<string, Dictionary<string, long>> secondPart = _LDCData4Gram[entry[0]][entry[1]];

                // check if the 3 term exists in the dictionary
                if (!secondPart.ContainsKey(entry[2]))
                {
                    // add the 1st term
                    Dictionary<string, long> temp = new Dictionary<string, long>();
                    secondPart.Add(entry[2], temp);
                }

                Dictionary<string, long> thirdPart = _LDCData4Gram[entry[0]][entry[1]][entry[2]];

                // add the entry
                thirdPart.Add(entry[3], score);

                counter++;
            }

            file.Close();
            Console.WriteLine("OK");
        }

        // helper function to convert N-Grams from text input to parseable format
        static string[] ConvertNGramLineToEntry(string line)
        {
            string[] entry = line.Split(new char[] { '\t', ' ' });

            return entry;
        }

        // iteratively go through terms and NGrams till you get to a leaf
        static Dictionary<string, long> TryGetSubGramN(string[] inputTerms, int N)
        {
            // leaf
            if (N == 2)
            {
                if (_LDCData2Gram.ContainsKey(inputTerms[0]))
                {
                    return _LDCData2Gram[inputTerms[0]];
                }
            }
            else if (N == 3)
            {
                if (_LDCData3Gram.ContainsKey(inputTerms[0]))
                {
                    if (_LDCData3Gram[inputTerms[0]].ContainsKey(inputTerms[1]))
                    {
                        return _LDCData3Gram[inputTerms[0]][inputTerms[1]];
                    }
                }
            }
                /*
            else if (N == 4)
            {
                if (_LDCData4Gram.ContainsKey(inputTerms[0]))
                {
                    if (_LDCData4Gram[inputTerms[0]].ContainsKey(inputTerms[1]))
                    {
                        if (_LDCData4Gram[inputTerms[0]][inputTerms[1]].ContainsKey(inputTerms[2]))
                        {
                            return _LDCData4Gram[inputTerms[0]][inputTerms[1]][inputTerms[2]];
                        }
                    }
                }
            }*/
            else
            {
                Console.WriteLine("ERROR, N > 4!");
            }

            return null;
        }

        public static string GetRelatedQueriesLinks(string searchString)
        {
            string htmlString = "";

            SortedDictionary<long, string[]> suggestedQueries = SuggestNGrams(searchString.Split(' '));
            List<string[]> suggestedQueriesList = suggestedQueries.Values.ToList();
            suggestedQueriesList.Reverse();

            //http://www.ruralcafe.net/localsearch?textfield=BLAH+BLAH&button=RuralCafe+Search&specificity=normal&richness=medium

            foreach (string[] suggestedQuery in suggestedQueriesList)
            {
                string linkString = "http://www.ruralcafe.net/localsearch?textfield=";
                string textString = "\"";
                if (suggestedQuery.Length > 1)
                {
                    // get the terms in the suggested query
                    for (int i = 0; i < suggestedQuery.Length - 1; i++)
                    {
                        linkString += suggestedQuery[i] + "+";
                        textString += suggestedQuery[i] + " ";
                    }

                    // don't echo original searchstring
                    if(textString.Trim().Equals("\"" + searchString)) 
                    {
                        continue;
                    }

                    linkString = linkString.Substring(0, linkString.Length - 1);
                    textString = textString.Substring(0, textString.Length - 1);

                    // XXX: default values for now we're not keeping track of the original search settings
                    linkString += "&button=Queue+Search+Query&specificity=normal&richness=low&depth=normal";

                    textString += "\" - " + suggestedQuery[suggestedQuery.Length - 1] + " occurences";
                    htmlString += "<a href=\"" + linkString + "\" target=\"content_frame\">" + textString + "</a><br>";
                }
            }

            return htmlString;
        }

        // suggest N-Grams for a given array of input terms
        private static SortedDictionary<long, string[]> SuggestNGrams(string[] inputTerms)
        {
            int N = inputTerms.Length;

            SortedDictionary<long, string[]> suggestedNGrams = new SortedDictionary<long, string[]>();

            // Try N = N grams

            // only can suggest for N = 2+
            if (N > 1)
            {
                // try to get the subGram
                Dictionary<string, long> subGram = TryGetSubGramN(inputTerms, N);
                if (subGram != null)
                {
                    // suggest highest N-gram with
                    // first N-1 terms the same as the the first N-1 terms in the N-gram

                    // get top 5 scores
                    // XXX: might want to change to over some threshold on top of maximum of 5
                    // XXX: also should use edit distance in the formulation of weighting

                    long originalNGramScore = 0;
                    if (subGram.ContainsKey(inputTerms[N - 1]))
                    {
                        originalNGramScore = subGram[inputTerms[N - 1]];
                    }

                    long currentNGramScore = 0;
                    string suggestedLastTerm = "";
                    foreach (KeyValuePair<string, long> kvp in subGram)
                    {
                        currentNGramScore = kvp.Value;
                        suggestedLastTerm = kvp.Key;
                        // get the lowest score or 0 if nothing in the suggestions
                        long lowestScore = 0;
                        if (suggestedNGrams.Count > 0)
                        {
                            lowestScore = suggestedNGrams.ElementAt(0).Key;
                        }

                        // add if there aren't enough suggestions
                        if (suggestedNGrams.Count < 5 &&
                            currentNGramScore > originalNGramScore)
                        {
                            // add to the set
                            string[] suggestedTerms = new string[N + 1];
                            int i = 0;
                            for (; i < N - 1; i++)
                            {
                                suggestedTerms[i] = inputTerms[i];
                            }
                            suggestedTerms[N - 1] = suggestedLastTerm;
                            suggestedTerms[N] = currentNGramScore.ToString();

                            suggestedNGrams.Add(currentNGramScore, suggestedTerms);
                        }
                        // add the score if its high enough
                        else if (kvp.Value > lowestScore)
                        {
                            // remove the lowest scoring N Gram (top 5 for now)
                            if (suggestedNGrams.Count >= 5)
                            {
                                suggestedNGrams.Remove(lowestScore);
                            }

                            // add to the set
                            string[] suggestedTerms = new string[N + 1];
                            int i = 0;
                            for (; i < N - 1; i++)
                            {
                                suggestedTerms[i] = inputTerms[i];
                            }
                            suggestedTerms[N - 1] = suggestedLastTerm;
                            suggestedTerms[N] = currentNGramScore.ToString();

                            suggestedNGrams.Add(currentNGramScore, suggestedTerms);
                        }
                    }
                }
            }

            // Try to suggest N+1 grams
            if (N < 4)
            {
                // try to get the subGram
                Dictionary<string, long> subGram = TryGetSubGramN(inputTerms, N + 1);
                if (subGram != null)
                {
                    // suggest highest N+1-gram with
                    // first N terms the same as the the first N term in the N-gram

                    // get top 5 scores
                    // XXX: might want to change to over some threshold on top of maximum of 5
                    // XXX: also should use edit distance in the formulation of weighting

                    long currentNGramScore = 0;
                    string suggestedLastTerm = "";
                    foreach (KeyValuePair<string, long> kvp in subGram)
                    {
                        currentNGramScore = kvp.Value;
                        suggestedLastTerm = kvp.Key;
                        // get the lowest score or 0 if nothing in the suggestions
                        long lowestScore = 0;
                        if (suggestedNGrams.Count > 0)
                        {
                            lowestScore = suggestedNGrams.ElementAt(0).Key;
                        }

                        // add the score if its high enough
                        if (kvp.Value > lowestScore)
                        {
                            // remove the lowest scoring N Gram (top 5 for now)
                            if (suggestedNGrams.Count >= 5)
                            {
                                suggestedNGrams.Remove(lowestScore);
                            }

                            // add to the set
                            string[] suggestedTerms = new string[N + 2];
                            int i = 0;
                            for (; i < N; i++)
                            {
                                suggestedTerms[i] = inputTerms[i];
                            }
                            suggestedTerms[N] = suggestedLastTerm;
                            suggestedTerms[N + 1] = currentNGramScore.ToString();

                            suggestedNGrams.Add(currentNGramScore, suggestedTerms);
                        }
                    }
                }
            }

            /*
            // Try to suggest N+2 grams
            if (N < 3)
            {
                // try to get the subGram
                Dictionary<string, long> subGram = TryGetSubGramN(inputTerms, N + 2);
                if (subGram != null)
                {
                    // suggest highest N+2-gram with
                    // first N terms the same as the the first N term in the N-gram

                    // get top 5 scores
                    // XXX: might want to change to over some threshold on top of maximum of 5
                    // XXX: also should use edit distance in the formulation of weighting

                    int currentNGramScore = 0;
                    string suggestedLastTerm = "";
                    foreach (KeyValuePair<string, long> kvp in subGram)
                    {
                        currentNGramScore = kvp.Value;
                        suggestedLastTerm = kvp.Key;
                        // get the lowest score or 0 if nothing in the suggestions
                        int lowestScore = 0;
                        if (suggestedNGrams.Count > 0)
                        {
                            lowestScore = suggestedNGrams.ElementAt(0).Key;
                        }

                        // add the score if its high enough
                        if (kvp.Value > lowestScore)
                        {
                            // remove the lowest scoring N Gram (top 5 for now)
                            if (suggestedNGrams.Count >= 5)
                            {
                                suggestedNGrams.Remove(lowestScore);
                            }

                            // add to the set
                            string[] suggestedTerms = new string[N + 2];
                            int i = 0;
                            for (; i < N; i++)
                            {
                                suggestedTerms[i] = inputTerms[i];
                            }
                            suggestedTerms[N] = suggestedLastTerm;
                            suggestedTerms[N + 1] = currentNGramScore.ToString();

                            suggestedNGrams.Add(currentNGramScore, suggestedTerms);
                        }
                    }
                }
            }
            */

            return suggestedNGrams;
        }
    }
}
