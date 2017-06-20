﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.IO;


namespace Text_Prediction
{
    /// <summary>
    /// This class contains all the methods which are needed to implement Text Prediction
    /// </summary>
    public class Prediction
    {

        #region OBJECTS AND VARIABLES

        String input;
        int limit;
        String[] splits, data;

        StreamReader reader = null;
        TernarySearchTree tst = new TernarySearchTree();
        TernarySearchTree stst = new TernarySearchTree();
        SuggestDictionary sd = new SuggestDictionary();
        BKTree bkt = new BKTree();

        #endregion        

        /// <summary>
        /// Prediction Class Constructor
        /// </summary>
        /// <param name="source">Absolute path of dictionary file</param>
        public Prediction(String source)
        {
            createInitialStructures(source);
        }

        #region CREATE DATA STRUCTURES
        
        /// <summary>
        /// Create all structures
        /// </summary>
        /// <param name="source">Absolute path of dictionary file</param>
        private void createInitialStructures(String source)
        {
            createAutoCompleteTree(source);
            createSuggestDictionary(source);
            createBKTree(source);
        }

       /// <summary>
       /// Create autocomplete tree
       /// </summary>
       /// <param name="source">Absolute path of dictionary</param>
        private void createAutoCompleteTree(String source)
        {
            reader = new StreamReader(source);
            List<String> list = new List<String>();
            while ((input = reader.ReadLine()) != null)
            {
                splits = input.Split('&');
                list.Add(splits[0]);
            }
            MyExtensions.Shuffle(list);
            foreach (String i in list)
            {
                data = i.Split(';');
                tst.insert(data[0], int.Parse(data[1]));
            }
            reader.Close();
        }

        /// <summary>
        /// Create autosuggest Hash
        /// </summary>
        /// <param name="source">bsolute path of dictionary file<</param>
        private void createSuggestDictionary(String source)
        {
            int count=0;
            List<String> list;
            reader = new StreamReader(source);
            while ((input = reader.ReadLine()) != null)
            {
                list = null;
                splits = input.Split('&');
                data = splits[0].Split(';');
                list = new List<String>(splits);
                count++;
                list = list.GetRange(1, list.Count - 1);
                sd.ht.Add(data[0], input.Substring(splits[0].Length + 1));
            }
            reader.Close();
        }

        /// <summary>
        /// generate the BKTree
        /// </summary>
        /// <param name="source">Absolute path of dictionary file</param>
        private void createBKTree(String source)
        {

            reader = new StreamReader(source);
            while ((input = reader.ReadLine()) != null)
            {
                splits = input.Split('&');
                data = splits[0].Split(';');
                bkt.Add(data[0]);
            }
            reader.Close();
        }

        #endregion---------

        #region Autocomplete

        /// <summary>
        /// Suggest the probable word based on the dictionary in descending order
        /// </summary>
        /// <param name="input">an incomplete word</param>
        /// <returns>A list of probable words generated by prefix matching</returns>
        public List<String> getAutoCompleteSuggestions(String input)
        {
            List<String> list = new List<String>();
            list = tst.getAutoCompleteSuggestions(input);
            return list;
        }

        #endregion

        #region AutoSuggest

        /// <summary>
        /// Similar to Autocomplete but with a reduced no of words as given by the getNextWordSuggestions.Use after next word suggestions
        /// as an alternative for autocomplete so that the unrelated words are ignored
        /// </summary>
        /// <param name="prefix">An incomplete word entry</param>
        /// <returns>a list of possible words based on prefix matching to prefix</returns>
        public List<String> getAutoSuggestWordSuggestions(String prefix)
        {
            return sd.getAutoSuggestSuggestions(prefix);
        }
        
        /// <summary>
        /// Predict the next word after current input
        /// </summary>
        /// <param name="input">A word whose next probable word needs to be predicted</param>
        /// <returns>A list of strings which contains probable words in descending order</returns>
        public List<String> getNextWordSuggestions(String input)
        {
            return sd.getHashSuggestions(input);
        }

        #endregion

        #region AutoCorrect

        /// <summary>
        /// Gives the autocorrect alternatives
        /// </summary>
        /// <param name="input">the word which is suspected to be incorrect </param>
        /// <returns>A list of suggestions which can serve as an alternative to the input</returns>
        public List<String> getAutoCorrectSuggestions(String input)
        {
            return bkt.Search(input, 2);
        }

        #endregion

        #region Learning

        /// <summary>
        /// Function Updates the dictionary to include new words in the RAM
        /// </summary>
        /// <param name="inputText">Pass the data to be learned here</param>
        /// <param name="addNewWordsCond">This boolean value specifies whether or not new words should be learned.true if yes. False if new 
        /// words are to be ignored</param>
        public void learnData(String inputText, bool addNewWordsCond)
        {
            if (inputText == "")
                return;

            String[] terms = inputText.Split(' ');
            List<String> newterms = new List<String>();
            List<String> oldterms = new List<String>();
            Dictionary<String, String> h = new Dictionary<String, String>();

            
            // identify new terms
            foreach (String term in terms)
            {
                TSTNode node = tst.traverse(term);
                if(node==null)
                {
                    newterms.Add(term);
                }
                else
                {
                    oldterms.Add(term);
                    node.wordEnd = true;
                }
            }

            // generate bigrams
            if(terms.Length>1)
            {
               
                for (int i = 0; i < terms.Length - 1; i++)
                {
                    if (h.ContainsKey(terms[0]))
                    {
                        h[terms[i]] = h[terms[i]] + terms[i + 1];
                    }
                    else
                    {
                        h.Add(terms[i], terms[i + 1]);
                    }
                    
                }
            }
            if(terms.Length==1)
            {
                if (h.ContainsKey(terms[0]))
                {
                }
                else
                {
                    h.Add(terms[0], "");
                }
            }
            
            //autocorrect and autocomplete
            if (addNewWordsCond==true)
            {
                if (newterms.Count > 0)
                {
                    foreach (String newTerm in newterms)
                    {
                        tst.insert(newTerm, 1);
                        bkt.Add(newTerm);
                    }
                }
                if (oldterms.Count > 0)
                {
                    foreach (String oldTerm in oldterms)
                    {
                        TSTNode node = tst.traverse(oldTerm);
                        node.frequency += 1;
                    }
                }
                
                //add bigrams
                foreach (KeyValuePair<String, String> pair in h)
                {
                    if (sd.ht.ContainsKey(pair.Key))
                    {
                        String[] wordsWithFreq = pair.Value.Split('&');
                        int count = 0;
                        foreach(String wordWithFreq in wordsWithFreq)
                        {
                            String[] wordAndFreq = wordWithFreq.Split(';');
                            if(wordAndFreq[0]==pair.Value)
                            {
							    count++;
							    try 
                                {
                                    sd.ht[pair.Key] = sd.ht[pair.Key].Replace(wordWithFreq, wordAndFreq[0] + ";" + int.Parse(wordAndFreq[1] + 1));
                                    //sd.ht.Add(pair.Key,sd.ht[pair.Key].Replace(wordWithFreq,wordAndFreq[0]+";"+int.Parse(wordAndFreq[1] +1)));		
								} 
                                catch (IndexOutOfRangeException e) 
                                {
                                    sd.ht[pair.Key] = sd.ht[pair.Key].Replace(wordWithFreq, wordAndFreq[0] + ";1");
                                    //sd.ht.Add(pair.Key, sd.ht[pair.Key].Replace(wordWithFreq, wordAndFreq[0] + ";1"));
							    }
                                break;
                            }
                        }
                        if (count == 0) 
                        {
                            sd.ht[pair.Key] = sd.ht[pair.Key] + (pair.Value) + ";1&";
                            //sd.ht.Add(pair.Key , sd.ht[pair.Key]+(pair.Value)+ ";1&");
					    }
                    }
                    else
                    {
                        sd.ht.Add(pair.Key, pair.Value + ";1&");
                    }
                }
            }

            else
            {
                if (oldterms.Count > 0)
                {
                    foreach (String oldTerm in oldterms)
                    {
                        TSTNode node = tst.traverse(oldTerm);
                        node.frequency += 1;
                    }
                }
                foreach (KeyValuePair<String, String> pair in h)
                {
                    if (newterms.Contains(pair.Key) ||newterms.Contains(pair.Value))
                    {
                        //skip if new terms are found
                    }
                    else
                    {
                        if (sd.ht.ContainsKey(pair.Key))
                        {
                            String[] wordsWithFreq = pair.Value.Split('&');
                            int count = 0;
                            foreach(String wordWithFreq in wordsWithFreq)
                            {
                                String[] wordAndFreq = wordWithFreq.Split(';');
                                if(wordAndFreq[0]==pair.Value)
                                {
    							    count++;
  							        try 
                                    {
                                        sd.ht[pair.Key] = sd.ht[pair.Key].Replace(wordWithFreq, wordAndFreq[0] + ";" + int.Parse(wordAndFreq[1] + 1));   
                                        //sd.ht.Add(pair.Key,sd.ht[pair.Key].Replace(i,wordAndFreq[0]+";"+int.Parse(wordAndFreq[1] +1)));		
								    } 
                                    catch (IndexOutOfRangeException e) 
                                    {
                                        sd.ht[pair.Key] = sd.ht[pair.Key].Replace(wordWithFreq, wordAndFreq[0] + ";1");
                                        //sd.ht.Add(pair.Key, sd.ht[pair.Key].Replace(i, wordAndFreq[0] + ";1"));
							        }
                                    break;
                                }
                            }
                            if (count == 0) 
                            {
                                sd.ht[pair.Key] = sd.ht[pair.Key] + (pair.Value) + ";1&";
                                //sd.ht.Add(pair.Key , sd.ht[pair.Key]+(pair.Value)+ ";1&");
					        }
                        }
                        else
                        {
                            sd.ht.Add(pair.Key, pair.Value + ";1&");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// commit the changes made by learnData to the disk
        /// </summary>
        /// <param name="destination">Name of the target file</param>
        public void commit(String destination)
        {
            StreamWriter fw = new StreamWriter(destination);
            foreach(KeyValuePair<String,String> pair in sd.ht)
            {
                TSTNode node = tst.traverse(pair.Key);
                if (node == null)
                    continue;
                if(pair.Value=="")
                {
                    fw.Write(pair.Key + ";" + node.frequency + "&" + Environment.NewLine);
                    continue;
                } 
                fw.Write(pair.Key +";" + node.frequency+ "&" + pair.Value + Environment.NewLine);
            }
            fw.Close();
        }
        #endregion

        #region Combined Predictions

        /// <summary>
        /// A text changed event handler
        /// </summary>
        /// <param name="input">The input from a text field on which we want to apply text prediction</param>
        /// <param name="limit">The maximum no. of suggestions returned by this method</param>
        /// <returns>A list of strings which will be the most probable based on previous input</returns>
        public List<String> getAllPredictions(String input, int limit)
        {
            String line = input;
            List<String> ret = new List<string>();
            if (line == "")
            {
                return ret;
            }
            else
            {
                if (line[line.Length - 1] == ' ')
                {
                    String[] str = line.Split(' ');
                    ret = getNextWordSuggestions(str[str.Length - 2]);
                    if (ret.Count > limit)
                        ret = ret.GetRange(0, limit);
                    return ret;
                }
                else
                {
                    if (line.Split(' ').Length == 1)
                    {
                        ret = getAutoCompleteSuggestions(line);
                        if (ret.Count >= limit)
                            return ret.GetRange(0, limit);
                        else
                        {
                            foreach (String i in getAutoCorrectSuggestions(line))
                            {
                                ret.Add(i);
                            }
                            if (ret.Count > limit)
                                return ret.GetRange(0, limit);
                            return ret;
                        }
                    }
                    else
                    {
                        String[] str = line.Split(' ');

                        if (str[str.Length - 2].Contains('.') || str[str.Length - 2].Contains(','))
                        {
                            List<String> re = getAutoCompleteSuggestions(str[str.Length - 1]);
                            if (re.Count == limit)
                                return re;
                            else
                            {
                                foreach (String i in getAutoCorrectSuggestions(str[str.Length - 1]))
                                {
                                    re.Add(i);
                                }
                                try
                                {
                                    return re.GetRange(0, limit - 1);
                                }
                                catch (ArgumentOutOfRangeException e)
                                {

                                    return re;
                                }
                            }
                        }
                        ret=getAutoSuggestWordSuggestions(str[str.Length - 1]);
                        if(ret.Count>limit)
                            ret=ret.GetRange(0,limit);
                        return ret;
                    }
                }
            }
        }


        #endregion
    }

    #region "OTHER CLASSES"
    /// <summary>
    /// Safe threading for Shuffling
    /// </summary>
    static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    /// <summary>
    /// This class is used to shuffle the dictionary entries.
    /// </summary>
    static class MyExtensions
    {
        /// <summary>
        /// Shuffle a List
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="list">An Ilist object</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
    #endregion
}
